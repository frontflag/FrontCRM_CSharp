namespace CRM.Core.Utilities;

/// <summary>收款核销延期：计算日 − 核销日须严格大于延期天数。</summary>
public static class CommissionDelayRules
{
    public static bool IsEligible(DateOnly calcDate, DateOnly receiptDate, int delayDays) =>
        calcDate.DayNumber - receiptDate.DayNumber > delayDays;

    /// <summary>首次满足延期的计算日 = 核销日 + 延期天数 + 1。</summary>
    public static DateOnly FirstEligibleDate(DateOnly receiptDate, int delayDays) =>
        receiptDate.AddDays(delayDays + 1);
}
