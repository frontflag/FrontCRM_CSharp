using System.Globalization;

namespace CRM.Core.Utilities;

/// <summary>列表看板趋势时间窗：日 30 点 / 周 12 点 / 月 12 点；可与入库日起止相交裁剪。</summary>
public static class InventoryAnalyticsTrendWindow
{
    public const int DefaultDayPoints = 30;
    public const int DefaultWeekPoints = 12;
    public const int DefaultMonthPoints = 12;

    public static string NormalizeGroupBy(string? groupBy) =>
        (groupBy ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "day" => "day",
            "week" => "week",
            _ => "month"
        };

    public static DateTime SnapToPeriodStart(DateTime date, string groupBy)
    {
        date = date.Date;
        return groupBy switch
        {
            "week" => ISOWeek.ToDateTime(ISOWeek.GetYear(date), ISOWeek.GetWeekOfYear(date), DayOfWeek.Monday),
            "month" => new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => date
        };
    }

    public static DateTime DefaultFrom(DateTime toInclusive, string groupBy)
    {
        toInclusive = toInclusive.Date;
        return groupBy switch
        {
            "day" => toInclusive.AddDays(-(DefaultDayPoints - 1)),
            "week" => SnapToPeriodStart(toInclusive, "week").AddDays(-7 * (DefaultWeekPoints - 1)),
            _ => new DateTime(toInclusive.Year, toInclusive.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMonths(-(DefaultMonthPoints - 1))
        };
    }

    /// <summary>
    /// 默认窗相对截止日；若填了入库日起止则与该区间相交。
    /// 无交集时返回 false。
    /// </summary>
    public static bool TryResolveRange(
        DateTime today,
        string groupBy,
        DateTime? inboundFrom,
        DateTime? inboundTo,
        out DateTime from,
        out DateTime to)
    {
        today = today.Date;
        to = today;
        if (inboundTo.HasValue && inboundTo.Value.Date < to)
            to = inboundTo.Value.Date;
        from = DefaultFrom(to, groupBy);
        if (inboundFrom.HasValue && inboundFrom.Value.Date > from)
            from = inboundFrom.Value.Date;
        from = SnapToPeriodStart(from, groupBy);
        if (from > to)
        {
            from = default;
            to = default;
            return false;
        }

        return true;
    }

    public static List<string> BuildPeriodKeys(DateTime from, DateTime to, string groupBy)
    {
        var keys = new List<string>();
        var cursor = from.Date;
        var end = to.Date;
        while (cursor <= end)
        {
            keys.Add(FormatPeriodKey(cursor, groupBy));
            cursor = groupBy switch
            {
                "day" => cursor.AddDays(1),
                "week" => cursor.AddDays(7),
                _ => cursor.AddMonths(1)
            };
        }

        return keys.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static string FormatPeriodKey(DateTime date, string groupBy) => groupBy switch
    {
        "day" => date.ToString("yyyy-MM-dd"),
        "week" => $"{date:yyyy}-W{ISOWeek.GetWeekOfYear(date):D2}",
        _ => date.ToString("yyyy-MM")
    };

    public static (DateTime Start, DateTime EndExclusive) ParsePeriodRange(string period, string groupBy)
    {
        if (groupBy == "day" && DateTime.TryParse(period, out var day))
            return (day.Date, day.Date.AddDays(1));

        if (groupBy == "month" && DateTime.TryParse(period + "-01", out var month))
            return (month.Date, month.AddMonths(1));

        if (groupBy == "week" && period.Contains("-W", StringComparison.Ordinal))
        {
            var parts = period.Split("-W", StringSplitOptions.None);
            if (parts.Length == 2
                && int.TryParse(parts[0], out var year)
                && int.TryParse(parts[1], out var week))
            {
                var start = ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
                return (start, start.AddDays(7));
            }
        }

        return (DateTime.MinValue, DateTime.MinValue);
    }
}
