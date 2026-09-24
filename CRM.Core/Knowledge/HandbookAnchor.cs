using System.Security.Cryptography;
using System.Text;

namespace CRM.Core.Knowledge;

/// <summary>
/// 章、节锚点。问答链接和浏览页用同一规则，刷新后仍能对上。
/// </summary>
public static class HandbookAnchor
{
    public static string Chapter(string? chapterNo, string? heading)
    {
        if (IsAppendix(heading))
            return string.IsNullOrEmpty(chapterNo) ? "c-ax" : "c-a-" + chapterNo.Trim();
        return string.IsNullOrEmpty(chapterNo) ? "c-front" : "c-" + chapterNo.Trim();
    }

    public static string Section(string? chapterNo, string? heading, string? sectionNo, string? sectionTitle)
    {
        var chapter = Chapter(chapterNo, heading);
        if (!string.IsNullOrEmpty(sectionNo))
            return chapter + "-s-" + sectionNo.Trim();
        if (!string.IsNullOrEmpty(sectionTitle))
            return chapter + "-s-t-" + ShortHash(sectionTitle);
        return chapter + "-s-intro";
    }

    public static bool IsAppendix(string? heading) =>
        !string.IsNullOrEmpty(heading) && heading.StartsWith("附录", StringComparison.Ordinal);

    private static string ShortHash(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text.Trim()));
        return Convert.ToHexString(hash)[..12].ToLowerInvariant();
    }
}
