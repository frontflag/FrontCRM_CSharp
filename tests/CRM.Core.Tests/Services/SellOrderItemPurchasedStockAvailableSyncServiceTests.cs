using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CRM.Core.Tests.Services;

public class SellOrderItemPurchasedStockAvailableSyncServiceTests
{
    private readonly IRepository<StockItem> _stockItemRepo = Substitute.For<IRepository<StockItem>>();
    private readonly IRepository<SellOrderItem> _soItemRepo = Substitute.For<IRepository<SellOrderItem>>();
    private readonly IRepository<SellOrderItemExtend> _extendRepo = Substitute.For<IRepository<SellOrderItemExtend>>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly SellOrderItemPurchasedStockAvailableSyncService _service;

    public SellOrderItemPurchasedStockAvailableSyncServiceTests()
    {
        _uow.SaveChangesAsync().Returns(1);
        _service = new SellOrderItemPurchasedStockAvailableSyncService(
            Substitute.For<IRepository<StockInfo>>(),
            _stockItemRepo,
            _soItemRepo,
            _extendRepo,
            Substitute.For<IRepository<PurchaseOrderItem>>(),
            Substitute.For<IRepository<PurchaseOrder>>(),
            Substitute.For<IRepository<StockInItem>>(),
            Substitute.For<IRepository<StockInItemExtend>>(),
            _uow,
            NullLogger<SellOrderItemPurchasedStockAvailableSyncService>.Instance);
    }

    [Fact]
    public async Task RecalculateByPurchasePnAndBrandAsync_WritesAvailableSumAndSaves()
    {
        var lineId = "soi-1";
        var ext = new SellOrderItemExtend { Id = lineId, PurchasedStock_AvailableQty = 0 };
        _stockItemRepo.GetAllAsync().Returns(new List<StockItem>
        {
            new()
            {
                StockType = StockInventoryTypeCodes.Stocking,
                PurchasePn = "ISO6762FQDwRQ1",
                PurchaseBrand = "TI/德州仪器",
                QtyRepertoryAvailable = 2000
            }
        });
        _soItemRepo.GetAllAsync().Returns(new List<SellOrderItem>
        {
            new() { Id = lineId, PN = "ISO6762FQDwRQ1", Brand = "TI/德州仪器", Qty = 2500, Status = 0 }
        });
        _extendRepo.GetByIdAsync(lineId).Returns(ext);

        await _service.RecalculateByPurchasePnAndBrandAsync("ISO6762FQDwRQ1", "TI/德州仪器");

        Assert.Equal(2000, ext.PurchasedStock_AvailableQty);
        await _extendRepo.Received(1).UpdateAsync(ext);
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task RecalculateByPurchasePnAndBrandAsync_UnchangedQty_DoesNotSave()
    {
        var lineId = "soi-1";
        var ext = new SellOrderItemExtend { Id = lineId, PurchasedStock_AvailableQty = 2000 };
        _stockItemRepo.GetAllAsync().Returns(new List<StockItem>
        {
            new()
            {
                StockType = StockInventoryTypeCodes.Stocking,
                PurchasePn = "PN",
                PurchaseBrand = "BR",
                QtyRepertoryAvailable = 2000
            }
        });
        _soItemRepo.GetAllAsync().Returns(new List<SellOrderItem>
        {
            new() { Id = lineId, PN = "PN", Brand = "BR", Qty = 1, Status = 0 }
        });
        _extendRepo.GetByIdAsync(lineId).Returns(ext);

        await _service.RecalculateByPurchasePnAndBrandAsync("PN", "BR");

        await _extendRepo.DidNotReceive().UpdateAsync(Arg.Any<SellOrderItemExtend>());
        await _uow.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task RecalculateAllUnfinishedSellLinesAsync_WritesPoolQtyForUnfinishedLine()
    {
        var lineId = "soi-open";
        var ext = new SellOrderItemExtend
        {
            Id = lineId,
            PurchasedStock_AvailableQty = 0,
            StockOutProgressStatus = 0
        };
        _stockItemRepo.GetAllAsync().Returns(new List<StockItem>
        {
            new()
            {
                StockType = StockInventoryTypeCodes.Stocking,
                PurchasePn = "NTECL216M/32AM-H1",
                PurchaseBrand = "NANYA(南亚)",
                QtyRepertoryAvailable = 500
            }
        });
        _soItemRepo.GetAllAsync().Returns(new List<SellOrderItem>
        {
            new()
            {
                Id = lineId,
                SellOrderItemCode = "SO0023P-1",
                PN = "NTECL216M/32AM-H1",
                Brand = "NANYA(南亚)",
                Qty = 1000,
                Status = 0
            }
        });
        _extendRepo.GetAllAsync().Returns(new List<SellOrderItemExtend> { ext });

        var result = await _service.RecalculateAllUnfinishedSellLinesAsync();

        Assert.Equal(500, ext.PurchasedStock_AvailableQty);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(1, result.IncreasedCount);
        Assert.Equal(0, result.DecreasedCount);
        Assert.Contains("SO0023P-1", result.ChangedLineCodes);
        await _extendRepo.Received(1).UpdateAsync(ext);
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task RecalculateAllUnfinishedSellLinesAsync_SkipsOutboundComplete()
    {
        var lineId = "soi-done";
        var ext = new SellOrderItemExtend
        {
            Id = lineId,
            PurchasedStock_AvailableQty = 0,
            StockOutProgressStatus = 2
        };
        _stockItemRepo.GetAllAsync().Returns(new List<StockItem>
        {
            new()
            {
                StockType = StockInventoryTypeCodes.Stocking,
                PurchasePn = "PN",
                PurchaseBrand = "BR",
                QtyRepertoryAvailable = 500
            }
        });
        _soItemRepo.GetAllAsync().Returns(new List<SellOrderItem>
        {
            new() { Id = lineId, PN = "PN", Brand = "BR", Qty = 10, Status = 0 }
        });
        _extendRepo.GetAllAsync().Returns(new List<SellOrderItemExtend> { ext });

        var result = await _service.RecalculateAllUnfinishedSellLinesAsync();

        Assert.Equal(0, ext.PurchasedStock_AvailableQty);
        Assert.Equal(1, result.SkippedOutboundComplete);
        Assert.Equal(0, result.UpdatedCount);
        await _extendRepo.DidNotReceive().UpdateAsync(Arg.Any<SellOrderItemExtend>());
        await _uow.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task RecalculateAllUnfinishedSellLinesAsync_UnchangedQty_DoesNotSave()
    {
        var lineId = "soi-same";
        var ext = new SellOrderItemExtend
        {
            Id = lineId,
            PurchasedStock_AvailableQty = 2000,
            StockOutProgressStatus = 0
        };
        _stockItemRepo.GetAllAsync().Returns(new List<StockItem>
        {
            new()
            {
                StockType = StockInventoryTypeCodes.Stocking,
                PurchasePn = "PN",
                PurchaseBrand = "BR",
                QtyRepertoryAvailable = 2000
            }
        });
        _soItemRepo.GetAllAsync().Returns(new List<SellOrderItem>
        {
            new() { Id = lineId, PN = "PN", Brand = "BR", Qty = 1, Status = 0 }
        });
        _extendRepo.GetAllAsync().Returns(new List<SellOrderItemExtend> { ext });

        var result = await _service.RecalculateAllUnfinishedSellLinesAsync();

        Assert.Equal(2000, ext.PurchasedStock_AvailableQty);
        Assert.Equal(1, result.UnchangedCount);
        Assert.Equal(0, result.UpdatedCount);
        await _extendRepo.DidNotReceive().UpdateAsync(Arg.Any<SellOrderItemExtend>());
        await _uow.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task RecalculateAllUnfinishedSellLinesAsync_ExcludesManualTransferSourceFromPool()
    {
        var lineId = "soi-1";
        var ext = new SellOrderItemExtend { Id = lineId, PurchasedStock_AvailableQty = 0 };
        _stockItemRepo.GetAllAsync().Returns(new List<StockItem>
        {
            new()
            {
                StockType = StockInventoryTypeCodes.Stocking,
                TransferType = StockItemTransferTypeCodes.ManualTransferSource,
                PurchasePn = "PN",
                PurchaseBrand = "BR",
                QtyRepertoryAvailable = 100
            },
            new()
            {
                StockType = StockInventoryTypeCodes.Stocking,
                PurchasePn = "PN",
                PurchaseBrand = "BR",
                QtyRepertoryAvailable = 50
            }
        });
        _soItemRepo.GetAllAsync().Returns(new List<SellOrderItem>
        {
            new() { Id = lineId, PN = "pn", Brand = "br", Qty = 1, Status = 0 }
        });
        _extendRepo.GetAllAsync().Returns(new List<SellOrderItemExtend> { ext });

        await _service.RecalculateAllUnfinishedSellLinesAsync();

        Assert.Equal(50, ext.PurchasedStock_AvailableQty);
    }
}
