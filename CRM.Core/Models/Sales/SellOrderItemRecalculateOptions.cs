namespace CRM.Core.Models.Sales;

/// <summary>
/// 销售明细扩展重算时，是否把出库通知计划量对齐到销售行。
/// 保存等业务事件默认全开；详情页「刷新状态」必须全关，避免副作用改通知数量。
/// </summary>
public sealed class SellOrderItemRecalculateOptions
{
    public static SellOrderItemRecalculateOptions Default { get; } = new();

    /// <summary>只重算进度与状态，不改出库通知计划量。</summary>
    public static SellOrderItemRecalculateOptions StatusOnly { get; } = new()
    {
        SyncStockOutNotifyQty = false,
        ValidateOutboundQtyAgainstSoLine = false
    };

    /// <summary>仅收缩超量单条未出库通知，不扩成整单。</summary>
    public static SellOrderItemRecalculateOptions QtyPlanOnly { get; } = new()
    {
        SyncStockOutNotifyQty = true,
        ValidateOutboundQtyAgainstSoLine = true
    };

    public bool SyncStockOutNotifyQty { get; init; } = true;
    public bool ValidateOutboundQtyAgainstSoLine { get; init; } = true;
}
