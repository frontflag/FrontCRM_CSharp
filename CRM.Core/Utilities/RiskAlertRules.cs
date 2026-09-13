using CRM.Core.Constants;

namespace CRM.Core.Utilities;

public static class RiskAlertRules
{
    public static bool IsEnabled(decimal threshold) => threshold > 0m;

    public static bool IsEnabled(int threshold) => threshold > 0;

    /// <summary>超标：实际严格大于门槛；相等不预警。</summary>
    public static bool ExceedsAmount(decimal actualUsd, decimal thresholdUsd) =>
        IsEnabled(thresholdUsd) && actualUsd > thresholdUsd;

    public static bool ExceedsDays(int ageDays, int thresholdDays) =>
        IsEnabled(thresholdDays) && ageDays > thresholdDays;

    /// <summary>无入库日返回 -1，调用方跳过。</summary>
    public static int AgeDays(DateOnly today, DateTime? date)
    {
        if (date is null || date.Value == default)
            return -1;
        var inbound = DateOnly.FromDateTime(date.Value.Date);
        if (inbound == DateOnly.MinValue)
            return -1;
        return Math.Max(0, today.DayNumber - inbound.DayNumber);
    }

    public static bool TryNormalizeUsd(decimal? raw, out decimal stored, out string? error)
    {
        stored = 0m;
        error = null;
        var v = raw ?? 0m;
        if (v < 0m)
        {
            error = "预警金额不能为负数";
            return false;
        }

        if (v > RiskAlertLimits.MaxUsd)
        {
            error = "预警金额超过上限";
            return false;
        }

        if (decimal.Round(v, RiskAlertLimits.Scale, MidpointRounding.AwayFromZero) != v)
        {
            error = "预警金额最多两位小数";
            return false;
        }

        stored = v;
        return true;
    }

    public static bool TryNormalizeDays(int? raw, out int stored, out string? error)
    {
        stored = 0;
        error = null;
        var v = raw ?? 0;
        if (v < 0)
        {
            error = "预警天数不能为负数";
            return false;
        }

        if (v > RiskAlertLimits.MaxDays)
        {
            error = "预警天数超过上限";
            return false;
        }

        stored = v;
        return true;
    }
}
