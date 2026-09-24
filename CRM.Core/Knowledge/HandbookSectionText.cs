namespace CRM.Core.Knowledge;

/// <summary>
/// 把同一节的多块拼成连续正文，去掉后一块开头与前一块结尾的重叠。
/// </summary>
public static class HandbookSectionText
{
    public static string Merge(IReadOnlyList<string> parts)
    {
        if (parts.Count == 0)
            return "";
        var merged = parts[0] ?? "";
        for (var i = 1; i < parts.Count; i++)
        {
            var piece = StripRepeatedHeading(merged, parts[i] ?? "");
            var overlap = SharedEdge(merged, piece, 500);
            if (overlap >= 20)
                piece = piece[overlap..];
            if (piece.Length == 0)
                continue;
            if (merged.Length > 0 && piece[0] != '\n' && merged[^1] != '\n')
                merged += "\n";
            merged += piece;
        }

        return merged;
    }

    private static string StripRepeatedHeading(string merged, string piece)
    {
        var nl = piece.IndexOf('\n');
        if (nl <= 0)
            return piece;
        var line = piece[..nl];
        if (line.Length >= 2 && merged.Contains(line, StringComparison.Ordinal))
            return piece[(nl + 1)..];
        return piece;
    }

    private static int SharedEdge(string merged, string piece, int max)
    {
        var limit = Math.Min(max, Math.Min(merged.Length, piece.Length));
        for (var len = limit; len >= 20; len--)
        {
            if (merged.AsSpan(merged.Length - len).SequenceEqual(piece.AsSpan(0, len)))
                return len;
        }

        return 0;
    }
}
