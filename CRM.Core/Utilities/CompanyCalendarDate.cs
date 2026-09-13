namespace CRM.Core.Utilities;

/// <summary>公司日历日（<c>Asia/Shanghai</c>），用于工作日程圆点日切。</summary>
public static class CompanyCalendarDate
{
    public static readonly TimeZoneInfo Zone = ResolveZone();

    private static TimeZoneInfo ResolveZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
        }
    }

    public static DateOnly ToCompanyDate(DateTime utc)
    {
        var u = utc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(utc, DateTimeKind.Utc)
            : utc.ToUniversalTime();
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(u, Zone));
    }

    public static (DateTime StartUtc, DateTime EndUtc) MonthUtcRange(int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        var next = start.AddMonths(1);
        return (ToUtc(start), ToUtc(next));
    }

    public static (DateTime StartUtc, DateTime EndUtc) DayUtcRange(DateOnly date)
    {
        return (ToUtc(date), ToUtc(date.AddDays(1)));
    }

    public static DateTime ToUtc(DateOnly date)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, Zone);
    }
}
