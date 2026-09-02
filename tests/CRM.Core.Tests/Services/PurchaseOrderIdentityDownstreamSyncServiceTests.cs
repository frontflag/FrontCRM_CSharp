using System.Linq.Expressions;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customs;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CRM.Core.Tests.Services;

public class PurchaseOrderIdentityDownstreamSyncServiceTests
{
    private readonly List<StockInNotify> _notices = new();
    private readonly List<StockInItem> _stockInItems = new();
    private readonly List<StockInItemExtend> _stockInExtends = new();
    private readonly List<StockItem> _stockItems = new();
    private readonly List<StockInfo> _stocks = new();
    private readonly List<PackingItem> _packingItems = new();
    private readonly List<CustomsDeclarationItem> _customsItems = new();
    private readonly ISellOrderItemPurchasedStockAvailableSyncService _available =
        Substitute.For<ISellOrderItemPurchasedStockAvailableSyncService>();
    private readonly PurchaseOrderIdentityDownstreamSyncService _service;

    public PurchaseOrderIdentityDownstreamSyncServiceTests()
    {
        var notifyRepo = Substitute.For<IRepository<StockInNotify>>();
        var stockInItemRepo = Substitute.For<IRepository<StockInItem>>();
        var stockInExtendRepo = Substitute.For<IRepository<StockInItemExtend>>();
        var stockItemRepo = Substitute.For<IRepository<StockItem>>();
        var stockRepo = Substitute.For<IRepository<StockInfo>>();
        var packingRepo = Substitute.For<IRepository<PackingItem>>();
        var customsRepo = Substitute.For<IRepository<CustomsDeclarationItem>>();

        Bind(notifyRepo, _notices);
        Bind(stockInItemRepo, _stockInItems);
        Bind(stockInExtendRepo, _stockInExtends);
        Bind(stockItemRepo, _stockItems);
        Bind(stockRepo, _stocks);
        Bind(packingRepo, _packingItems);
        Bind(customsRepo, _customsItems);

        var rebucket = new StockItemPurchaseIdentityRebucketService(
            stockItemRepo,
            stockRepo,
            NullLogger<StockItemPurchaseIdentityRebucketService>.Instance,
            serialNumbers: null,
            purchasedStockAvailable: _available,
            unitOfWork: null);

        _service = new PurchaseOrderIdentityDownstreamSyncService(
            notifyRepo,
            stockInItemRepo,
            stockInExtendRepo,
            stockItemRepo,
            packingRepo,
            customsRepo,
            NullLogger<PurchaseOrderIdentityDownstreamSyncService>.Instance,
            rebucket);
    }

    [Fact]
    public async Task ApplyAsync_Brand_UpdatesSnapshots_AndRebucketsStockItem()
    {
        var item = NewPoItem(pn: "PN-OLD", brand: "BRAND-NEW");
        _notices.Add(new StockInNotify
        {
            Id = "N-1",
            NoticeCode = "AN001",
            PurchaseOrderItemId = item.Id,
            Brand = "OLD-BRAND",
            Pn = "PN-OLD"
        });
        _stockInItems.Add(new StockInItem
        {
            Id = "SII-1",
            StockInItemCode = "STI-1",
            PurchaseBrand = "OLD-BRAND",
            PurchasePn = "PN-OLD"
        });
        _stockInExtends.Add(new StockInItemExtend
        {
            Id = "SII-1",
            PurchaseOrderItemId = item.Id
        });
        _stocks.Add(new StockInfo
        {
            Id = "AGG-1",
            StockCode = "STK00001",
            MaterialId = "M-1",
            WarehouseId = "WH-1",
            StockType = 1,
            RegionType = 10,
            PurchaseBrand = "OLD-BRAND",
            PurchasePn = "PN-OLD",
            Qty = 5,
            QtyRepertory = 5,
            QtyRepertoryAvailable = 5
        });
        _stockItems.Add(new StockItem
        {
            Id = "STK-1",
            StockItemCode = "STK00001-1",
            StockInItemId = "SII-1",
            StockInId = "SI-1",
            StockAggregateId = "AGG-1",
            MaterialId = "M-1",
            WarehouseId = "WH-1",
            StockType = 1,
            RegionType = 10,
            PurchaseOrderItemId = item.Id,
            PurchaseBrand = "OLD-BRAND",
            PurchasePn = "PN-OLD",
            QtyInbound = 5,
            QtyRepertory = 5,
            QtyRepertoryAvailable = 5
        });
        _packingItems.Add(new PackingItem
        {
            Id = "PK-1",
            PackingId = "P-1",
            ItemCode = "PAK-1",
            StockItemId = "STK-1",
            Brand = "OLD-BRAND",
            Pn = "PN-OLD"
        });
        _customsItems.Add(new CustomsDeclarationItem
        {
            Id = "CDI-1",
            DeclarationId = "CD-1",
            StockOutRequestId = "SOR-1",
            MaterialId = "M-1",
            SourceStockItemId = "STK-1",
            PurchaseBrand = "OLD-BRAND",
            PurchasePn = "PN-OLD"
        });

        var result = await _service.ApplyAsync(new[] { item }, PurchaseOrderIdentitySnapshotField.Brand);

        Assert.True(result.HasUpdates);
        Assert.Equal(1, result.ArrivalNoticesUpdated);
        Assert.Equal(1, result.StockInItemsUpdated);
        Assert.Equal(1, result.PackingItemsUpdated);
        Assert.Equal(1, result.CustomsDeclarationItemsUpdated);
        Assert.Equal(1, result.StockItemsUpdated);
        Assert.Equal(1, result.StockItemsMoved);
        Assert.Equal(1, result.StockAggregatesCreated);
        Assert.Equal(1, result.StockAggregatesRemoved);
        Assert.True(_stocks[0].IsDeleted);
        Assert.Equal("BRAND-NEW", _notices[0].Brand);
        Assert.Equal("PN-OLD", _notices[0].Pn);
        Assert.Equal("BRAND-NEW", _stockInItems[0].PurchaseBrand);
        Assert.Equal("PN-OLD", _stockInItems[0].PurchasePn);
        Assert.Equal("BRAND-NEW", _packingItems[0].Brand);
        Assert.Equal("BRAND-NEW", _customsItems[0].PurchaseBrand);
        Assert.Equal("BRAND-NEW", _stockItems[0].PurchaseBrand);
        Assert.Equal("PN-OLD", _stockItems[0].PurchasePn);
        Assert.Equal("STK00001-1", _stockItems[0].StockItemCode);
        Assert.NotEqual("AGG-1", _stockItems[0].StockAggregateId);
        Assert.Equal(0, _stocks[0].Qty);
        var created = _stocks.Single(s => s.Id != "AGG-1");
        Assert.Equal("BRAND-NEW", created.PurchaseBrand);
        Assert.Equal("PN-OLD", created.PurchasePn);
        Assert.Equal(5, created.Qty);
        await _available.Received().RecalculateByPurchasePnAndBrandAsync(
            "PN-OLD", "OLD-BRAND", Arg.Any<CancellationToken>());
        await _available.Received().RecalculateByPurchasePnAndBrandAsync(
            "PN-OLD", "BRAND-NEW", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_Pn_UpdatesDisplaySnapshots_LeavesBrand()
    {
        var item = NewPoItem(pn: "PN-NEW", brand: "BRAND-NEW");
        _notices.Add(new StockInNotify
        {
            Id = "N-1",
            PurchaseOrderItemId = item.Id,
            Brand = "OLD-BRAND",
            Pn = "PN-OLD"
        });
        _stocks.Add(new StockInfo
        {
            Id = "AGG-1",
            MaterialId = "M-1",
            WarehouseId = "WH-1",
            StockType = 1,
            RegionType = 10,
            PurchasePn = "PN-OLD",
            PurchaseBrand = "OLD-BRAND",
            Qty = 1,
            QtyRepertory = 1
        });
        _stockItems.Add(new StockItem
        {
            Id = "STK-1",
            StockItemCode = "STK-1-CODE",
            StockInItemId = "SII-1",
            StockInId = "SI-1",
            StockAggregateId = "AGG-1",
            MaterialId = "M-1",
            WarehouseId = "WH-1",
            StockType = 1,
            RegionType = 10,
            PurchaseOrderItemId = item.Id,
            PurchasePn = "PN-OLD",
            PurchaseBrand = "OLD-BRAND",
            QtyInbound = 1,
            QtyRepertory = 1
        });

        var result = await _service.ApplyAsync(new[] { item }, PurchaseOrderIdentitySnapshotField.Pn);

        Assert.Equal(1, result.ArrivalNoticesUpdated);
        Assert.Equal(1, result.StockItemsUpdated);
        Assert.Equal("PN-NEW", _notices[0].Pn);
        Assert.Equal("OLD-BRAND", _notices[0].Brand);
        Assert.Equal("PN-NEW", _stockItems[0].PurchasePn);
        Assert.Equal("OLD-BRAND", _stockItems[0].PurchaseBrand);
        Assert.Equal(1, result.StockItemsMoved);
        Assert.Equal(1, result.StockAggregatesRemoved);
        Assert.True(_stocks[0].IsDeleted);
        Assert.NotEqual("AGG-1", _stockItems[0].StockAggregateId);
    }

    private static PurchaseOrderItem NewPoItem(string pn, string brand) => new()
    {
        Id = "POI-LINE-1",
        PurchaseOrderItemCode = "PO001-1",
        PurchaseOrderId = "PO-1",
        PN = pn,
        Brand = brand,
        Qty = 1m,
        Cost = 1m
    };

    private static void Bind<T>(IRepository<T> repo, List<T> store) where T : CRM.Core.Models.BaseGuidEntity
    {
        repo.FindAsync(Arg.Any<Expression<Func<T, bool>>>())
            .Returns(call =>
            {
                var pred = call.Arg<Expression<Func<T, bool>>>().Compile();
                return store.Where(pred).ToList();
            });
        repo.GetByIdAsync(Arg.Any<string>())
            .Returns(call =>
            {
                var id = call.Arg<string>();
                return store.FirstOrDefault(x =>
                    string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
            });
        repo.UpdateAsync(Arg.Any<T>()).Returns(Task.CompletedTask);
        repo.AddAsync(Arg.Any<T>())
            .Returns(call =>
            {
                store.Add(call.Arg<T>());
                return Task.CompletedTask;
            });
        repo.DeleteAsync(Arg.Any<string>())
            .Returns(call =>
            {
                var id = call.Arg<string>();
                var found = store.FirstOrDefault(x =>
                    string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
                if (found is ISoftDeletable soft)
                    soft.IsDeleted = true;
                return Task.CompletedTask;
            });
    }
}
