namespace CRM.Core.Utilities;

public enum CommissionEntryKind : short
{
    None = 0,
    Base = 1,
    Other = 2
}

/// <summary>Base / Other 入期与计算月（上海日历）。</summary>
public static class CommissionBaseOtherRules
{
    public static CommissionEntryKind Classify(
        DateOnly? stockOutDate,
        DateOnly? receiptDate,
        DateOnly lockDate)
    {
        if (!stockOutDate.HasValue || !receiptDate.HasValue)
            return CommissionEntryKind.None;

        var window = CommissionTerm.GetWindow(lockDate);
        var otherFrom = CommissionTerm.GetPreviousLockDate(lockDate);
        var outDay = stockOutDate.Value;
        var payDay = receiptDate.Value;

        if (outDay >= window.From && outDay <= window.To && payDay < lockDate)
            return CommissionEntryKind.Base;
        if (outDay < window.From && payDay >= otherFrom && payDay < lockDate)
            return CommissionEntryKind.Other;
        return CommissionEntryKind.None;
    }

    public static string? CalcMonth(
        CommissionEntryKind kind,
        DateOnly? stockOutDate,
        DateOnly? receiptDate)
    {
        var day = kind switch
        {
            CommissionEntryKind.Base => stockOutDate,
            CommissionEntryKind.Other => receiptDate,
            _ => null
        };
        return day.HasValue ? CommissionTerm.FormatMonth(day.Value) : null;
    }

    /// <summary>已有计算月优先；否则按 Base/Other 或出库/核销日回填。</summary>
    public static string ResolveCalcMonth(
        string? stored,
        CommissionEntryKind kind,
        DateOnly? stockOutDate,
        DateOnly? receiptDate,
        DateOnly? fallbackDate = null)
    {
        var trimmed = stored?.Trim();
        if (CalcMonthStart(trimmed) != null)
            return trimmed!;

        var fromKind = CalcMonth(kind, stockOutDate, receiptDate);
        if (fromKind != null)
            return fromKind;

        var day = kind == CommissionEntryKind.Other
            ? receiptDate ?? stockOutDate ?? fallbackDate
            : stockOutDate ?? receiptDate ?? fallbackDate;
        return day.HasValue ? CommissionTerm.FormatMonth(day.Value) : string.Empty;
    }

    public static DateOnly? CalcMonthStart(string? calcMonth)
    {
        if (string.IsNullOrWhiteSpace(calcMonth) || calcMonth.Length != 7 || calcMonth[4] != '-')
            return null;
        if (!int.TryParse(calcMonth.AsSpan(0, 4), out var y) || !int.TryParse(calcMonth.AsSpan(5, 2), out var m))
            return null;
        if (m is < 1 or > 12) return null;
        return new DateOnly(y, m, 1);
    }
}
