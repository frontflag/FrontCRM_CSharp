namespace CRM.Core.Utilities;

public readonly record struct CommissionBimonthWindow(DateOnly From, DateOnly To)
{
    public string Term => $"{To.Year:D4}{To.Month:D2}";
}

/// <summary>双月窗口与锁定日（上海日历）。</summary>
public static class CommissionTerm
{
    public static bool IsEvenMonth(int month) => month is 2 or 4 or 6 or 8 or 10 or 12;

    public static bool IsLockDate(DateOnly shanghaiDate) =>
        shanghaiDate.Day == 2 && IsEvenMonth(shanghaiDate.Month);

    /// <summary>把任意上海日收成最近一次应执行的锁定日（该偶数月 2 日，尚未到则回退上一锁定日）。</summary>
    public static DateOnly ResolveLockDate(DateOnly shanghaiDate)
    {
        if (IsEvenMonth(shanghaiDate.Month))
        {
            var day2 = new DateOnly(shanghaiDate.Year, shanghaiDate.Month, 2);
            return shanghaiDate >= day2 ? day2 : AddEvenMonths(day2, -1);
        }

        var prev = shanghaiDate.AddMonths(-1);
        return new DateOnly(prev.Year, prev.Month, 2);
    }

    /// <summary>锁定日对应的上一完整双月（入池日闭区间）。<paramref name="lockDate"/> 应为偶数月 2 日。</summary>
    public static CommissionBimonthWindow GetWindow(DateOnly lockDate)
    {
        var end = lockDate.AddMonths(-1);
        var start = lockDate.AddMonths(-2);
        var from = new DateOnly(start.Year, start.Month, 1);
        var to = new DateOnly(end.Year, end.Month, DateTime.DaysInMonth(end.Year, end.Month));
        return new CommissionBimonthWindow(from, to);
    }

    /// <summary>下一锁定日：当天已是偶数月 2 日则取当天，否则取即将到来的偶数月 2 日。</summary>
    public static DateOnly GetNextLockDate(DateOnly today)
    {
        if (IsEvenMonth(today.Month))
        {
            var day2 = new DateOnly(today.Year, today.Month, 2);
            return today <= day2 ? day2 : AddEvenMonths(day2, 1);
        }

        var nextEven = today.AddMonths(1);
        return new DateOnly(nextEven.Year, nextEven.Month, 2);
    }

    /// <summary>Other 核销窗起点：上一锁定日（偶数月 2 日）。</summary>
    public static DateOnly GetPreviousLockDate(DateOnly lockDate) =>
        AddEvenMonths(lockDate, -1);

    public static string FormatMonth(DateOnly day) =>
        $"{day.Year:D4}-{day.Month:D2}";

    /// <summary>今天尚未锁或已锁之后，入池日应落入的开放双月。</summary>
    public static CommissionBimonthWindow GetOpenWindow(DateOnly today)
    {
        DateOnly nextLock;
        if (IsEvenMonth(today.Month))
        {
            var thisLock = new DateOnly(today.Year, today.Month, 2);
            nextLock = today < thisLock ? thisLock : AddEvenMonths(thisLock, 1);
        }
        else
        {
            var nextEven = today.AddMonths(1);
            nextLock = new DateOnly(nextEven.Year, nextEven.Month, 2);
        }

        return GetWindow(nextLock);
    }

    public static string FormatTerm(DateOnly endMonth) =>
        $"{endMonth.Year:D4}{endMonth.Month:D2}";

    public static string FormatTermLabel(string term)
    {
        if (!TryParseTerm(term, out var year, out var month))
            return term;
        var end = new DateOnly(year, month, 1);
        var start = end.AddMonths(-1);
        return $"{year:D4}-{month:D2}（{start.Month}–{month}月）";
    }

    public static bool TryParseTerm(string? term, out int year, out int month)
    {
        year = 0;
        month = 0;
        if (string.IsNullOrWhiteSpace(term) || term.Length != 6)
            return false;
        return int.TryParse(term.AsSpan(0, 4), out year)
            && int.TryParse(term.AsSpan(4, 2), out month)
            && month is >= 1 and <= 12;
    }

    static DateOnly AddEvenMonths(DateOnly evenMonthDay2, int steps) =>
        evenMonthDay2.AddMonths(steps * 2);
}
