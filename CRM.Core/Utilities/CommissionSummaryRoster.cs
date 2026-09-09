using CRM.Core.Interfaces;

namespace CRM.Core.Utilities;

public readonly record struct CommissionRosterPerson(string UserId, string UserName, short UserLevel);

/// <summary>预计提成按人汇总：在职全员（含 0）+ 离职仅提成大于 0。</summary>
public static class CommissionSummaryRoster
{
    public static List<CommissionSummaryDto> Merge(
        IReadOnlyList<CommissionSummaryDto> fromLines,
        IReadOnlyList<CommissionRosterPerson> activeRoster,
        string? keyword)
    {
        var lines = fromLines ?? [];
        var roster = activeRoster ?? [];
        var byId = lines
            .GroupBy(x => x.UserId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var rosterIds = roster
            .Select(x => x.UserId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var kw = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();

        var result = new List<CommissionSummaryDto>();
        foreach (var person in roster)
        {
            if (byId.TryGetValue(person.UserId, out var row))
            {
                result.Add(new CommissionSummaryDto
                {
                    UserId = row.UserId,
                    UserName = string.IsNullOrWhiteSpace(person.UserName) ? row.UserName : person.UserName,
                    UserLevel = row.UserLevel,
                    LineCount = row.LineCount,
                    MonthCount = row.MonthCount,
                    PeriodGpUsd = row.PeriodGpUsd,
                    CommissionUsd = row.CommissionUsd
                });
                continue;
            }

            if (kw != null && !NameMatches(person.UserName, kw))
                continue;

            result.Add(new CommissionSummaryDto
            {
                UserId = person.UserId,
                UserName = person.UserName,
                UserLevel = person.UserLevel,
                LineCount = 0,
                MonthCount = 0,
                PeriodGpUsd = 0m,
                CommissionUsd = 0m
            });
        }

        foreach (var row in lines)
        {
            if (rosterIds.Contains(row.UserId))
                continue;
            if (row.CommissionUsd <= 0m)
                continue;
            result.Add(row);
        }

        return result
            .OrderByDescending(x => x.CommissionUsd)
            .ThenBy(x => x.UserName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    static bool NameMatches(string? name, string keyword) =>
        !string.IsNullOrWhiteSpace(name)
        && name.Contains(keyword, StringComparison.OrdinalIgnoreCase);
}
