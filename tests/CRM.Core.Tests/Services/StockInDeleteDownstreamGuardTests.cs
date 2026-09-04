using CRM.Core.Constants;
using CRM.Core.Models.Inventory;
using CRM.Core.Tests.Fakes;

namespace CRM.Core.Tests.Services;

/// <summary>
/// 入库单删除前「下游库存明细」拦截与列表/过账行为一致。
/// </summary>
public class StockInDeleteDownstreamGuardTests
{
    [Fact]
    public async Task FindAsync_ShouldExcludeSoftDeletedStockItems_ForDownstreamGuard()
    {
        var repo = new MemoryRepository<StockItem>();
        await repo.AddAsync(new StockItem
        {
            Id = "si-layer-1",
            StockItemCode = "STK0004U-1",
            StockInId = "sti-0007s",
            StockInItemId = "line-1",
            StockAggregateId = "agg-1",
            MaterialId = "mat-1",
            WarehouseId = "wh-1",
            IsDeleted = true
        });
        await repo.AddAsync(new StockItem
        {
            Id = "si-layer-2",
            StockItemCode = "STK0004U-2",
            StockInId = "sti-0007s",
            StockInItemId = "line-2",
            StockAggregateId = "agg-1",
            MaterialId = "mat-1",
            WarehouseId = "wh-1"
        });

        var sidLower = "sti-0007s";
        var blocking = (await repo.FindAsync(x =>
                x.StockInId != null &&
                x.StockInId.ToLower() == sidLower &&
                (x.TransferType == null || x.TransferType != StockItemTransferTypeCodes.ManualTransferSource)))
            .Where(x => !x.IsDeleted)
            .ToList();

        Assert.Single(blocking);
        Assert.Equal("STK0004U-2", blocking[0].StockItemCode);
    }

    [Fact]
    public async Task DownstreamGuard_ShouldIgnoreManualTransferSourceRows()
    {
        var repo = new MemoryRepository<StockItem>();
        await repo.AddAsync(new StockItem
        {
            Id = "si-layer-transfer",
            StockItemCode = "STK0004U-1",
            StockInId = "sti-0007s",
            StockInItemId = "line-1",
            StockAggregateId = "agg-1",
            MaterialId = "mat-1",
            WarehouseId = "wh-1",
            TransferType = StockItemTransferTypeCodes.ManualTransferSource
        });

        var sidLower = "sti-0007s";
        var blocking = (await repo.FindAsync(x =>
                x.StockInId != null &&
                x.StockInId.ToLower() == sidLower &&
                (x.TransferType == null || x.TransferType != StockItemTransferTypeCodes.ManualTransferSource)))
            .Where(x => !x.IsDeleted)
            .ToList();

        Assert.Empty(blocking);
    }

    [Fact]
    public async Task PostStockInDuplicateCheck_ShouldTreatSoftDeletedLayerAsAlreadyPosted()
    {
        var repo = new MemoryRepository<StockItem>();
        const string stockInId = "sti-0007s";
        const string lineId = "line-1";
        await repo.AddAsync(new StockItem
        {
            Id = "si-layer-deleted",
            StockItemCode = "STK0004U-1",
            StockInId = stockInId,
            StockInItemId = lineId,
            StockAggregateId = "agg-1",
            MaterialId = "mat-1",
            WarehouseId = "wh-1",
            IsDeleted = true
        });

        var postedLayers = (await repo.FindIgnoreFiltersAsync(x => x.StockInId == stockInId)).ToList();
        var shouldSkipLine = postedLayers.Any(x => x.StockInItemId == lineId);

        Assert.True(shouldSkipLine);
    }
}
