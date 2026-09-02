using CRM.Core.Models.Sales;

namespace CRM.Core.Interfaces;

/// <summary>
/// 以销售行当前 PN / 品牌为准，覆盖下游销售身份展示快照（不换库存堆）。
/// </summary>
public interface ISalesOrderIdentityDownstreamSyncService
{
    Task<SalesOrderIdentityDownstreamSyncResult> ApplyAsync(
        IReadOnlyList<SellOrderItem> items,
        SalesOrderIdentitySnapshotField field,
        CancellationToken cancellationToken = default);
}

public enum SalesOrderIdentitySnapshotField
{
    Pn = 0,
    Brand = 1
}

public class SalesOrderIdentityDownstreamSyncResult
{
    public int StockOutNotifiesUpdated { get; set; }
    public int PackingItemsUpdated { get; set; }
    public int PackingItemExtendsUpdated { get; set; }
    public int ReceivablesUpdated { get; set; }
    public List<SalesOrderIdentitySnapshotChangeDto> Changes { get; set; } = new();

    public bool HasUpdates =>
        StockOutNotifiesUpdated > 0
        || PackingItemsUpdated > 0
        || PackingItemExtendsUpdated > 0
        || ReceivablesUpdated > 0;
}

public class SalesOrderIdentitySnapshotChangeDto
{
    public string NodeType { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string? NodeCode { get; set; }
    public string? Before { get; set; }
    public string? After { get; set; }
}
