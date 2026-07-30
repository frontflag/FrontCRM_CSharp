namespace CRM.Core.Interfaces;

/// <summary>
/// 按有效出库事实对账装箱单主状态（可上/下行；供详情刷新与出库强制删除共用）。
/// </summary>
public interface IPackingStatusReconcileService
{
    /// <summary>
    /// 对账单个装箱单状态。
    /// <paramref name="excludingStockOutId"/> 用于强制删除场景：该出库单即将/已软删，一律不计入有效出库。
    /// <paramref name="saveChanges"/> 为 false 时仅 Update，由调用方统一 SaveChanges。
    /// </summary>
    Task<PackingStatusReconcileResult> ReconcileAsync(
        string packingId,
        string? actingUserId = null,
        string? excludingStockOutId = null,
        bool saveChanges = true,
        CancellationToken cancellationToken = default);

    /// <summary>批量对账（强制删除出库时可能关联多张装箱单）。</summary>
    Task<IReadOnlyList<PackingStatusReconcileResult>> ReconcileManyAsync(
        IReadOnlyCollection<string> packingIds,
        string? actingUserId = null,
        string? excludingStockOutId = null,
        bool saveChanges = true,
        CancellationToken cancellationToken = default);
}

public sealed class PackingStatusReconcileResult
{
    public string PackingId { get; set; } = string.Empty;
    public string? PackingCode { get; set; }
    public short PreviousStatus { get; set; }
    public short CurrentStatus { get; set; }
    public bool Changed => PreviousStatus != CurrentStatus;
    public bool HasLiveCompletedStockOut { get; set; }
}
