namespace CRM.Infrastructure.Common;

/// <summary>进度筛选项规范化：仅保留 0/1/2，去重排序。</summary>
internal static class ProgressStatusFilterHelper
{
    public static List<short> Normalize(IEnumerable<short>? values)
    {
        if (values == null) return new List<short>();
        return values
            .Where(v => v is >= 0 and <= 2)
            .Distinct()
            .OrderBy(v => v)
            .ToList();
    }
}
