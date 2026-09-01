namespace CRM.Core.Utilities;

/// <summary>
/// 供应商交易次数：采购货款付款单上、按付款单供应商现读。
/// 一次交易 = 该供应商有效付款单下一条不重复的采购明细。
/// </summary>
public static class VendorTradeCountRules
{
    public const short PaymentAuditFailed = -1;
    public const short PaymentCancelled = -2;

    /// <summary>有效付款单：未取消、未审核失败（软删由查询过滤器排除）。</summary>
    public static bool IsValidPaymentStatus(short status) =>
        status != PaymentAuditFailed && status != PaymentCancelled;
}
