using CRM.Core.Models.Purchase;

namespace CRM.Core.Interfaces;

/// <summary>
/// 以采购行当前 PN / 品牌为准，覆盖下游展示快照，并重对齐库存分桶键。
/// </summary>
public interface IPurchaseOrderIdentityDownstreamSyncService
{
    Task<PurchaseOrderIdentityDownstreamSyncResult> ApplyAsync(
        IReadOnlyList<PurchaseOrderItem> items,
        PurchaseOrderIdentitySnapshotField field,
        CancellationToken cancellationToken = default);
}

public enum PurchaseOrderIdentitySnapshotField
{
    Pn = 0,
    Brand = 1
}

public class PurchaseOrderIdentityDownstreamSyncResult
{
    public int ArrivalNoticesUpdated { get; set; }
    public int StockInItemsUpdated { get; set; }
    public int PackingItemsUpdated { get; set; }
    public int CustomsDeclarationItemsUpdated { get; set; }
    public int StockItemsUpdated { get; set; }
    public int StockItemsMoved { get; set; }
    public int StockAggregatesCreated { get; set; }
    public int StockAggregatesRemoved { get; set; }
    public List<PurchaseOrderIdentitySnapshotChangeDto> Changes { get; set; } = new();

    public bool HasUpdates =>
        ArrivalNoticesUpdated > 0
        || StockInItemsUpdated > 0
        || PackingItemsUpdated > 0
        || CustomsDeclarationItemsUpdated > 0
        || StockItemsUpdated > 0
        || StockItemsMoved > 0
        || StockAggregatesCreated > 0
        || StockAggregatesRemoved > 0;
}

public class PurchaseOrderIdentitySnapshotChangeDto
{
    public string NodeType { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string? NodeCode { get; set; }
    public string? Before { get; set; }
    public string? After { get; set; }
}
