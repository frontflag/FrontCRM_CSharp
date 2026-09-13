using CRM.Core.Constants;

namespace CRM.Core.Utilities;

public static class IncentiveTargetRules
{
    public static short RoleTypeFromIdentity(short identityType) =>
        identityType is 2 or 3
            ? (short)CommissionRoleType.Purchase
            : (short)CommissionRoleType.Sales;

    /// <summary>null 或 0 = 清除；否则须为正数、不超过上限、最多两位小数。</summary>
    public static bool TryNormalizeTarget(decimal? raw, out decimal? stored, out string? error)
    {
        stored = null;
        error = null;
        if (raw is null || raw.Value == 0m)
            return true;
        var v = raw.Value;
        if (v < 0m)
        {
            error = "计划提成不能为负数";
            return false;
        }

        if (v > IncentiveTargetLimits.MaxUsd)
        {
            error = "计划提成超过上限";
            return false;
        }

        if (decimal.Round(v, IncentiveTargetLimits.Scale, MidpointRounding.AwayFromZero) != v)
        {
            error = "计划提成最多两位小数";
            return false;
        }

        stored = v;
        return true;
    }

    public static decimal? CompletionPct(decimal actual, decimal? target)
    {
        if (target is null or <= 0m)
            return null;
        return Math.Round(actual / target.Value * 100m, 1, MidpointRounding.AwayFromZero);
    }

    public static string MonthSpan(DateOnly from, DateOnly to) =>
        $"{from.Month}–{to.Month}月";
}
