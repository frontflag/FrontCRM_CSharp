using System.Text;
using System.Text.RegularExpressions;

namespace CRM.Infrastructure.Knowledge;

/// <summary>把操作手册 Markdown 收成纯文本段落，供 HandbookChunker 按「第N章 / N.N」切块。</summary>
public static class HandbookMarkdownReader
{
    private static readonly Regex ListItem = new(@"^(?:[-*]|\d+\.)\s+", RegexOptions.Compiled);

    public static IReadOnlyList<string> ReadParagraphs(string markdown)
    {
        var paragraphs = new List<string>();
        var buffer = new StringBuilder();
        void Flush()
        {
            var text = buffer.ToString().Trim();
            if (text.Length > 0)
                paragraphs.Add(text);
            buffer.Clear();
        }

        foreach (var raw in markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line is "---" or "***")
            {
                Flush();
                continue;
            }

            if (line.StartsWith('#'))
            {
                Flush();
                var heading = line.TrimStart('#').Trim();
                if (heading.Length > 0)
                    paragraphs.Add(heading);
                continue;
            }

            if (ListItem.IsMatch(line))
            {
                Flush();
                paragraphs.Add(ListItem.Replace(line, "", 1).Trim());
                continue;
            }

            if (buffer.Length > 0)
                buffer.Append(' ');
            buffer.Append(line);
        }

        Flush();
        return paragraphs;
    }
}
