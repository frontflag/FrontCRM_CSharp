namespace CRM.Core.Constants;

/// <summary>出库通知 <c>stockout_notify.Status</c> 业务状态。</summary>
public static class StockOutRequestStatusCode
{
    /// <summary>待报关（境外库存满足整单数量时；不可销售装箱）。</summary>
    public const short PendingCustoms = 5;

    /// <summary>待装箱（可加入篮子生成装箱单）。</summary>
    public const short PendingPacking = 10;

    /// <summary>已装箱。</summary>
    public const short Packed = 20;

    /// <summary>已出库。</summary>
    public const short StockedOut = 100;

    /// <summary>已取消（不可再生成装箱单）。</summary>
    public const short Cancelled = -1;

    public static bool IsCancelled(short status) => status == Cancelled;

    public static bool IsActiveForQuantitySum(short status) => status != Cancelled;

    /// <summary>
    /// 是否计入销售明细「已通知出库数量」。
    /// 报关出库通知（<see cref="StockOutTypeCode.Customs"/>）与销售出库通知一一对应，不重复占用销售数量。
    /// </summary>
    public static bool IsCountedForSalesLineNotifyQuantity(short status, short stockOutType) =>
        IsActiveForQuantitySum(status)
        && StockOutTypeCode.NormalizeForNotify(stockOutType) != StockOutTypeCode.Customs;
}
