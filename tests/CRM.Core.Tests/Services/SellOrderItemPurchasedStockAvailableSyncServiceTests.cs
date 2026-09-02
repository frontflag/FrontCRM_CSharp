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
}
