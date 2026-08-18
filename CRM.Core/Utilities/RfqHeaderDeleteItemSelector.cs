using CRM.Core.Models.RFQ;

namespace CRM.Core.Utilities;

/// <summary>
/// 整单删除时仍有效的明细：优先认删除日志 ExtraInfo 中的行 ID；否则用与主单修改时间对齐的窗口。
/// </summary>
public static class RfqHeaderDeleteItemSelector
{
    public static readonly TimeSpan ModifyTimeWindow = TimeSpan.FromSeconds(3);

    public static HashSet<string>? ParseLoggedItemIds(string? extraInfo)
    {
        if (string.IsNullOrWhiteSpace(extraInfo))
            return null;

        var ids = extraInfo
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return ids.Count == 0 ? null : ids;
    }

    public static IReadOnlyList<RFQItem> Select(
        IEnumerable<RFQItem> items,
        DateTime? headerModifyTime,
        IReadOnlySet<string>? loggedItemIds)
    {
        var deleted = items.Where(i => i.IsDeleted).ToList();
        if (loggedItemIds is { Count: > 0 })
            return deleted
                .Where(i => loggedItemIds.Contains(i.Id))
                .OrderBy(i => i.LineNo)
                .ToList();

        if (!headerModifyTime.HasValue)
            return Array.Empty<RFQItem>();

        var start = headerModifyTime.Value - ModifyTimeWindow;
        var end = headerModifyTime.Value + TimeSpan.FromSeconds(1);
        return deleted
            .Where(i => i.ModifyTime.HasValue && i.ModifyTime.Value >= start && i.ModifyTime.Value <= end)
            .OrderBy(i => i.LineNo)
            .ToList();
    }
}
