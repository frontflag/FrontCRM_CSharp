namespace CRM.Core.Utilities;

/// <summary>个人提成详情行：收款进度与是否入池。</summary>
public static class CommissionPersonLineRules
{
    public const short ReceiptPending = 0;
    public const short ReceiptPartial = 1;
    public const short ReceiptComplete = 2;

    public static bool IsReceiptWriteOffDone(short receiptProgressStatus) =>
        receiptProgressStatus == ReceiptComplete;

    public static bool InCommission(
        short receiptProgressStatus,
        DateOnly? receiptDate,
        DateOnly today,
        int delayDays) =>
        IsReceiptWriteOffDone(receiptProgressStatus)
        && receiptDate.HasValue
        && CommissionDelayRules.IsEligible(today, receiptDate.Value, delayDays);

    public static int? DaysSinceReceipt(DateOnly? receiptDate, DateOnly today) =>
        receiptDate.HasValue ? today.DayNumber - receiptDate.Value.DayNumber : null;
}
