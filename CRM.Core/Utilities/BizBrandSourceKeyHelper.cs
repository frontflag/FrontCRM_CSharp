using System.Text;

namespace CRM.Core.Utilities;

/// <summary>导入品牌原文归一化键（全公司学习映射查重，英文大小写不敏感）。</summary>
public static class BizBrandSourceKeyHelper
{
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var s = raw.Trim();
        s = s.Replace('（', '(').Replace('）', ')');
        s = ToHalfWidth(s);
        s = CollapseWhitespace(s);
        return s.ToLowerInvariant();
    }

    private static string CollapseWhitespace(string s)
    {
        var sb = new StringBuilder(s.Length);
        var prevSpace = false;
        foreach (var ch in s)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!prevSpace)
                {
                    sb.Append(' ');
                    prevSpace = true;
                }
            }
            else
            {
                sb.Append(ch);
                prevSpace = false;
            }
        }

        return sb.ToString().Trim();
    }

    private static string ToHalfWidth(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (ch >= 0xFF01 && ch <= 0xFF5E)
                sb.Append((char)(ch - 0xFEE0));
            else if (ch == 0x3000)
                sb.Append(' ');
            else
                sb.Append(ch);
        }

        return sb.ToString();
    }
}
