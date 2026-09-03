namespace CRM.Core.Utilities;

/// <summary>
/// 销售明细收款/开票进度：有金额走「已收 ≥ 应收」；单价为 0 时须出库完成才视为完成。
/// </summary>
public static class SellOrderLineMoneyProgress
{
    public const short Pending = 0;
    public const short Partial = 1;
    public const short Complete = 2;

    /// <summary>
    /// 计算收款或开票进度（0=待 1=部分 2=完成）。
    /// </summary>
    public static short Compute(
        decimal unitPrice,
        decimal qty,
        decimal amountDue,
        decimal amountFinish,
        bool outboundComplete)
    {
        if (IsZeroPriceFullyShipped(unitPrice, qty, amountDue, outboundComplete))
            return Complete;
        if (amountFinish <= 0m)
            return Pending;
        if (amountDue > 0m && amountFinish + 0.0001m >= amountDue)
            return Complete;
        return Partial;
    }

    public static bool IsZeroPriceFullyShipped(
        decimal unitPrice,
        decimal qty,
        decimal amountDue,
        bool outboundComplete) =>
        unitPrice == 0m && amountDue == 0m && qty > 0m && outboundComplete;
}
