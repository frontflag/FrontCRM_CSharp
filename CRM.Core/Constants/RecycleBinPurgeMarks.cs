namespace CRM.Core.Constants;

/// <summary>回收站「删除」标记：仍保留软删行供单据引用，列表不再展示、不可恢复。</summary>
public static class RecycleBinPurgeMarks
{
    public const string Marker = "[RECYCLE_PURGED]";

    public static bool IsPurged(string? deleteReason) =>
        !string.IsNullOrEmpty(deleteReason)
        && deleteReason.StartsWith(Marker, StringComparison.Ordinal);

    public static string Mark(string? deleteReason, int maxLength)
    {
        var rest = deleteReason ?? string.Empty;
        if (rest.StartsWith(Marker, StringComparison.Ordinal))
            return rest.Length <= maxLength ? rest : rest[..maxLength];
        var marked = Marker + rest;
        return marked.Length <= maxLength ? marked : marked[..maxLength];
    }
}
