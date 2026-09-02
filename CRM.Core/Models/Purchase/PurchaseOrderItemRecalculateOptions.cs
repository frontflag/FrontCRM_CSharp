namespace CRM.Core.Models.Purchase;

/// <summary>
/// 采购明细扩展重算时，是否把到货通知上的计划量 / 单价 / 品牌快照对齐到采购行。
/// 保存等业务事件默认全开；详情页「刷新状态」必须全关，避免副作用改快照。
/// </summary>
public sealed class PurchaseOrderItemRecalculateOptions
{
    public static PurchaseOrderItemRecalculateOptions Default { get; } = new();

    /// <summary>只重算进度与状态，不改到货通知数量、单价、品牌。</summary>
    public static PurchaseOrderItemRecalculateOptions StatusOnly { get; } = new()
    {
        SyncArrivalNoticeQty = false,
        SyncArrivalNoticeCost = false,
        SyncArrivalNoticeBrand = false,
        ValidateArrivalQtyAgainstPoLine = false
    };

    /// <summary>仅收缩超量单批次预计数量，不改单价与品牌。</summary>
    public static PurchaseOrderItemRecalculateOptions QtyPlanOnly { get; } = new()
    {
        SyncArrivalNoticeQty = true,
        SyncArrivalNoticeCost = false,
        SyncArrivalNoticeBrand = false,
        ValidateArrivalQtyAgainstPoLine = true
    };

    public bool SyncArrivalNoticeQty { get; init; } = true;
    public bool SyncArrivalNoticeCost { get; init; } = true;
    public bool SyncArrivalNoticeBrand { get; init; } = true;
    public bool ValidateArrivalQtyAgainstPoLine { get; init; } = true;
}
