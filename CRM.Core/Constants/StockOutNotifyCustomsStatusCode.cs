namespace CRM.Core.Constants;

/// <summary>出库通知 <c>stockout_notify.CustomsStatus</c> 报关状态。</summary>
public static class StockOutNotifyCustomsStatusCode
{
    /// <summary>未知（新建出库通知）。</summary>
    public const short Unknown = 0;

    /// <summary>无需报关（生成销售装箱单且判定无需报关）。</summary>
    public const short NotRequired = 10;

    /// <summary>待报关（已生成待报关记录）。</summary>
    public const short PendingCustoms = 20;

    /// <summary>报关中（已生成报关出库通知）。</summary>
    public const short InCustoms = 30;

    /// <summary>报关完成（对应报关入库完成）。</summary>
    public const short Completed = 100;
}
