using CRM.Core.Constants;

namespace CRM.Core.Utilities;

/// <summary>库存滚存按月切分：业务自然月按显示时区（默认 Asia/Shanghai）。</summary>
public static class FinanceAccumulatedMonthBoundary
{
    public static string DefaultTimeZoneId => SysParamCodes.DefaultDisplayTimeZoneId;

    public static DateTime YearStartUtc(int year, string? timeZoneId = null) =>
        MonthStartUtc(year, 1, timeZoneId);

    public static DateTime YearEndExclusiveUtc(int year, string? timeZoneId = null) =>
        MonthStartUtc(year + 1, 1, timeZoneId);

    public static (DateTime StartUtc, DateTime EndExclusiveUtc) MonthRangeUtc(int year, int month, string? timeZoneId = null) =>
        (MonthStartUtc(year, month, timeZoneId), MonthEndExclusiveUtc(year, month, timeZoneId));

    /// <summary>当月结束边界（次月 1 日 00:00 显示时区，转 UTC）。</summary>
    public static DateTime MonthEndExclusiveUtc(int year, int month, string? timeZoneId = null)
    {
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month));

        return month == 12
            ? MonthStartUtc(year + 1, 1, timeZoneId)
            : MonthStartUtc(year, month + 1, timeZoneId);
    }

    public static (DateTime StartUtc, DateTime EndExclusiveUtc) ParseMonthRangeUtc(string month, string? timeZoneId = null)
    {
        if (!TryParseYearMonth(month, out var year, out var m))
            throw new ArgumentException("月份格式无效，应为 yyyy-MM。", nameof(month));

        return MonthRangeUtc(year, m, timeZoneId);
    }

    public static bool TryParseYearMonth(string? value, out int year, out int month)
    {
        year = 0;
        month = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Trim().Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return false;

        return int.TryParse(parts[0], out year)
               && int.TryParse(parts[1], out month)
               && year is >= 1900 and <= 9999
               && month is >= 1 and <= 12;
    }

    public static bool TryParseYear(string? value, out int year)
    {
        year = 0;
        return !string.IsNullOrWhiteSpace(value) && int.TryParse(value.Trim(), out year) && year is >= 1900 and <= 9999;
    }

    public static string ToYearMonthKey(DateTime utcTimestamp, string? timeZoneId = null)
    {
        var local = ToDisplayLocal(utcTimestamp, timeZoneId);
        return $"{local.Year:0000}-{local.Month:00}";
    }

    public static DateTime MonthStartUtc(int year, int month, string? timeZoneId = null)
    {
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month));

        var tz = ResolveTimeZone(timeZoneId);
        var local = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, tz);
    }

    private static DateTime ToDisplayLocal(DateTime utcTimestamp, string? timeZoneId)
    {
        var tz = ResolveTimeZone(timeZoneId);
        var utc = utcTimestamp.Kind == DateTimeKind.Utc
            ? utcTimestamp
            : DateTime.SpecifyKind(utcTimestamp, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        foreach (var id in BuildTimeZoneCandidates(timeZoneId))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        // Linux 容器等环境可能缺少 IANA 时区库，固定东八区偏移兜底。
        return TimeZoneInfo.CreateCustomTimeZone(
            "UTC+8",
            TimeSpan.FromHours(8),
            "UTC+8",
            "UTC+8");
    }

    private static IEnumerable<string> BuildTimeZoneCandidates(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId))
            yield return timeZoneId.Trim();

        yield return DefaultTimeZoneId;
        yield return "Asia/Shanghai";
        yield return "China Standard Time";
    }
}
