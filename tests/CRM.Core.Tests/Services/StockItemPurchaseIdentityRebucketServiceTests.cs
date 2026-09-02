using System.Linq.Expressions;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CRM.Core.Tests.Services;

public class StockItemPurchaseIdentityRebucketServiceTests
{
    private readonly List<StockItem> _layers = new();
    private readonly List<StockInfo> _buckets = new();
    private readonly IRepository<StockItem> _layerRepo = Substitute.For<IRepository<StockItem>>();
    private readonly IRepository<StockInfo> _bucketRepo = Substitute.For<IRepository<StockInfo>>();
    private readonly ISerialNumberService _serial = Substitute.For<ISerialNumberService>();
    private readonly ISellOrderItemPurchasedStockAvailableSyncService _available =
        Substitute.For<ISellOrderItemPurchasedStockAvailableSyncService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly StockItemPurchaseIdentityRebucketService _service;

    public StockItemPurchaseIdentityRebucketServiceTests()
    {
        Bind(_layerRepo, _layers);
        Bind(_bucketRepo, _buckets);
        _serial.GenerateNextAsync(Arg.Any<string>()).Returns("STKTEST1");
        _uow.SaveChangesAsync().Returns(1);
        _uow.ExecuteNonQueryAsync(Arg.Any<string>()).Returns(1);
        _service = new StockItemPurchaseIdentityRebucketService(
            _layerRepo,
            _bucketRepo,
            NullLogger<StockItemPurchaseIdentityRebucketService>.Instance,
            _serial,
            _available,
            _uow);
    }

    [Fact]
    public async Task EnsureAggregatesAsync_SameBucket_AlignsDisplayText_DoesNotMove()
    {
        var bucket = NewBucket("AGG-1", "PN-OLD", "BRAND-OLD", qty: 10);
        var layer = NewLayer("STK-1", "AGG-1", "PN-OLD", "BRAND-OLD", inbound: 10);
        layer.PurchasePn = "pn-old";
        layer.PurchaseBrand = "brand-old";
        _buckets.Add(bucket);
        _layers.Add(layer);

        var result = await _service.EnsureAggregatesAsync(new[] { layer });

        Assert.Equal(0, result.StockItemsMoved);
        Assert.Equal(0, result.StockAggregatesCreated);
        Assert.Equal(0, result.StockAggregatesRemoved);
        Assert.Equal("AGG-1", layer.StockAggregateId);
        Assert.Equal("STK-1-CODE", layer.StockItemCode);
        Assert.Equal("pn-old", bucket.PurchasePn);
        Assert.Equal("brand-old", bucket.PurchaseBrand);
        await _available.Received().RecalculateByPurchasePnAndBrandAsync("pn-old", "brand-old", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureAggregatesAsync_ExistingTargetBucket_MovesLayer_RecalculatesBoth()
    {
        var oldBucket = NewBucket("AGG-OLD", "PN-A", "BRAND-OLD", qty: 10);
        var newBucket = NewBucket("AGG-NEW", "PN-A", "BRAND-NEW", qty: 3);
        var moving = NewLayer("STK-1", "AGG-OLD", "PN-A", "BRAND-NEW", inbound: 10);
        var staying = NewLayer("STK-2", "AGG-NEW", "PN-A", "BRAND-NEW", inbound: 3);
        _buckets.Add(oldBucket);
        _buckets.Add(newBucket);
        _layers.Add(moving);
        _layers.Add(staying);

        var result = await _service.EnsureAggregatesAsync(new[] { moving });

        Assert.Equal(1, result.StockItemsMoved);
        Assert.Equal(0, result.StockAggregatesCreated);
        Assert.Equal(1, result.StockAggregatesRemoved);
        Assert.Equal("AGG-NEW", moving.StockAggregateId);
        Assert.Equal("STK-1-CODE", moving.StockItemCode);
        Assert.Equal(0, oldBucket.Qty);
        Assert.Equal(0, oldBucket.QtyRepertory);
        Assert.True(oldBucket.IsDeleted);
        Assert.False(newBucket.IsDeleted);
        Assert.Equal(13, newBucket.Qty);
        Assert.Equal(13, newBucket.QtyRepertory);
        await _available.Received().RecalculateByPurchasePnAndBrandAsync("PN-A", "BRAND-OLD", Arg.Any<CancellationToken>());
        await _available.Received().RecalculateByPurchasePnAndBrandAsync("PN-A", "BRAND-NEW", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureAggregatesAsync_NoTargetBucket_CreatesOne_KeepsStockItemCode()
    {
        var oldBucket = NewBucket("AGG-OLD", "PN-OLD", "BRAND-A", qty: 8);
        var layer = NewLayer("STK-1", "AGG-OLD", "PN-NEW", "BRAND-A", inbound: 8);
        layer.QtyStockOut = 2;
        layer.QtyOccupy = 1;
        layer.QtySales = 0;
        layer.QtyRepertory = 6;
        layer.QtyRepertoryAvailable = 5;
        oldBucket.QtyStockOut = 2;
        oldBucket.QtyOccupy = 1;
        oldBucket.QtyRepertory = 6;
        oldBucket.QtyRepertoryAvailable = 5;
        _buckets.Add(oldBucket);
        _layers.Add(layer);

        var result = await _service.EnsureAggregatesAsync(new[] { layer });

        Assert.Equal(1, result.StockItemsMoved);
        Assert.Equal(1, result.StockAggregatesCreated);
        Assert.Equal(1, result.StockAggregatesRemoved);
        Assert.Equal(2, _buckets.Count);
        var created = _buckets.Single(s => s.Id != "AGG-OLD");
        Assert.Equal("STKTEST1", created.StockCode);
        Assert.Equal("PN-NEW", created.PurchasePn);
        Assert.Equal("BRAND-A", created.PurchaseBrand);
        Assert.Equal(created.Id, layer.StockAggregateId);
        Assert.Equal("STK-1-CODE", layer.StockItemCode);
        Assert.Equal(0, oldBucket.Qty);
        Assert.Equal(0, oldBucket.QtyStockOut);
        Assert.True(oldBucket.IsDeleted);
        Assert.False(created.IsDeleted);
        Assert.Equal(8, created.Qty);
        Assert.Equal(2, created.QtyStockOut);
        Assert.Equal(1, created.QtyOccupy);
        Assert.Equal(6, created.QtyRepertory);
        Assert.Equal(5, created.QtyRepertoryAvailable);
        await _uow.Received(1).SaveChangesAsync();
        await _serial.Received(1).GenerateNextAsync("Stock");
    }

    [Fact]
    public async Task EnsureAggregatesAsync_TwoLayersSameNewKey_CreatesOnlyOneBucket()
    {
        var old1 = NewBucket("AGG-1", "PN-OLD", "BRAND-A", qty: 4);
        var old2 = NewBucket("AGG-2", "PN-OLD", "BRAND-B", qty: 6);
        var layer1 = NewLayer("STK-1", "AGG-1", "PN-NEW", "BRAND-A", inbound: 4);
        var layer2 = NewLayer("STK-2", "AGG-2", "PN-NEW", "BRAND-A", inbound: 6);
        layer2.PurchaseBrand = "BRAND-A";
        _buckets.Add(old1);
        _buckets.Add(old2);
        _layers.Add(layer1);
        _layers.Add(layer2);

        var result = await _service.EnsureAggregatesAsync(new[] { layer1, layer2 });

        Assert.Equal(2, result.StockItemsMoved);
        Assert.Equal(1, result.StockAggregatesCreated);
        Assert.Equal(2, result.StockAggregatesRemoved);
        Assert.Equal(3, _buckets.Count);
        Assert.Equal(layer1.StockAggregateId, layer2.StockAggregateId);
        var created = _buckets.Single(s => s.Id != "AGG-1" && s.Id != "AGG-2");
        Assert.Equal(10, created.Qty);
        Assert.True(old1.IsDeleted);
        Assert.True(old2.IsDeleted);
        Assert.False(created.IsDeleted);
    }

    [Fact]
    public async Task EnsureAggregatesAsync_OldBucketStillHasLayer_DoesNotRemove()
    {
        var oldBucket = NewBucket("AGG-OLD", "PN-A", "BRAND-OLD", qty: 15);
        var newBucket = NewBucket("AGG-NEW", "PN-A", "BRAND-NEW", qty: 0);
        var staying = NewLayer("STK-STAY", "AGG-OLD", "PN-A", "BRAND-OLD", inbound: 5);
        var moving = NewLayer("STK-1", "AGG-OLD", "PN-A", "BRAND-NEW", inbound: 10);
        _buckets.Add(oldBucket);
        _buckets.Add(newBucket);
        _layers.Add(staying);
        _layers.Add(moving);

        var result = await _service.EnsureAggregatesAsync(new[] { moving });

        Assert.Equal(1, result.StockItemsMoved);
        Assert.Equal(0, result.StockAggregatesRemoved);
        Assert.False(oldBucket.IsDeleted);
        Assert.Equal(5, oldBucket.Qty);
        Assert.Equal("AGG-NEW", moving.StockAggregateId);
        Assert.Equal("AGG-OLD", staying.StockAggregateId);
    }

    private static StockInfo NewBucket(string id, string pn, string brand, int qty) => new()
    {
        Id = id,
        StockCode = id,
        MaterialId = "M-1",
        WarehouseId = "WH-1",
        StockType = 1,
        RegionType = 10,
        PurchasePn = pn,
        PurchaseBrand = brand,
        Qty = qty,
        QtyRepertory = qty,
        QtyRepertoryAvailable = qty,
        Status = 1
    };

    private static StockItem NewLayer(string id, string aggId, string pn, string brand, int inbound) => new()
    {
        Id = id,
        StockItemCode = id + "-CODE",
        StockInItemId = id + "-SII",
        StockInId = "SI-1",
        StockAggregateId = aggId,
        MaterialId = "M-1",
        WarehouseId = "WH-1",
        StockType = 1,
        RegionType = 10,
        PurchasePn = pn,
        PurchaseBrand = brand,
        PurchaseOrderItemId = "POI-1",
        QtyInbound = inbound,
        QtyRepertory = inbound,
        QtyRepertoryAvailable = inbound
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
                if (found is CRM.Core.Interfaces.ISoftDeletable soft)
                    soft.IsDeleted = true;
                return Task.CompletedTask;
            });
    }
}
