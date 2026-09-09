using CRM.Core.Constants;

namespace CRM.Core.Utilities;

public readonly record struct UserLevelChangePoint(DateTime ChangeTimeUtc, short OldLevel, short NewLevel);

/// <summary>按入池日回放用户等级履历。</summary>
public static class UserLevelAsOf
{
    public static short Resolve(
        short currentLevel,
        IReadOnlyList<UserLevelChangePoint> history,
        DateOnly asOfShanghai)
    {
        var fallback = Normalize(currentLevel);
        if (history == null || history.Count == 0)
            return fallback;

        var ordered = history.OrderBy(x => x.ChangeTimeUtc).ThenBy(x => x.NewLevel).ToList();
        short? level = null;
        foreach (var row in ordered)
        {
            var changeDate = CommissionShanghai.ToDate(row.ChangeTimeUtc);
            if (changeDate <= asOfShanghai)
                level = Normalize(row.NewLevel);
            else
            {
                level ??= Normalize(row.OldLevel);
                break;
            }
        }

        return level ?? fallback;
    }

    static short Normalize(short level) =>
        UserLevelCode.IsValid(level) ? level : UserLevelCode.Default;
}
