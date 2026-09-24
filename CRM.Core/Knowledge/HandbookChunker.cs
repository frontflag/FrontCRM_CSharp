using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CRM.Core.Knowledge;

/// <summary>
/// 按章、节把教材段落切成块。不访问数据库，也不读 docx。
/// </summary>
public static class HandbookChunker
{
    public const int TargetLength = 900;
    public const int HardMax = 1400;
    public const int Overlap = 150;
    public const int MinMerge = 80;

    private static readonly Regex ChapterRegex = new(@"^第(?<no>\d+)章\s*(?<title>.*)$", RegexOptions.Compiled);
    private static readonly Regex SectionRegex = new(@"^(?<no>\d+\.\d+)(?!\.)\s*(?<title>.*)$", RegexOptions.Compiled);
    private static readonly Regex AppendixRegex = new(@"^附录(?:\s+(?<no>[A-Za-z0-9]+))?\s*(?<title>.*)$", RegexOptions.Compiled);

    public static IReadOnlyList<HandbookChunk> Chunk(IEnumerable<string> paragraphs)
    {
        var lines = paragraphs
            .Select(Normalize)
            .Where(x => x.Length > 0)
            .ToList();
        if (lines.Count == 0)
            return Array.Empty<HandbookChunk>();

        var body = lines.Skip(FindBodyStart(lines)).ToList();
        var sections = GroupSections(body);
        var raw = new List<RawChunk>();
        foreach (var section in sections)
            raw.AddRange(SplitSection(section));

        MergeShortChunks(raw);

        var result = new List<HandbookChunk>(raw.Count);
        for (var i = 0; i < raw.Count; i++)
        {
            var item = raw[i];
            result.Add(new HandbookChunk
            {
                ChapterNo = item.ChapterNo,
                ChapterTitle = item.ChapterTitle,
                SectionNo = item.SectionNo,
                SectionTitle = item.SectionTitle,
                Heading = item.Heading,
                Content = item.Content,
                ChunkIndex = i,
                ContentSha256 = Sha256Hex(item.Content)
            });
        }

        return result;
    }

    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";
        var sb = new StringBuilder(text.Length);
        var pendingSpace = false;
        foreach (var ch in text.Trim())
        {
            if (char.IsWhiteSpace(ch))
            {
                pendingSpace = sb.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                sb.Append(' ');
                pendingSpace = false;
            }

            sb.Append(ch);
        }

        return sb.ToString();
    }

    private static int FindBodyStart(IReadOnlyList<string> lines)
    {
        var toc = -1;
        for (var i = 0; i < lines.Count; i++)
        {
            if (IsTocMarker(lines[i]))
            {
                toc = i;
                break;
            }
        }

        if (toc < 0)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                if (Classify(lines[i]) != LineKind.Body)
                    return i;
            }

            return 0;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = toc + 1; i < lines.Count; i++)
        {
            if (!seen.Add(lines[i]))
                return i;
        }

        for (var i = toc + 1; i < lines.Count; i++)
        {
            if (Classify(lines[i]) != LineKind.Body)
                return i;
        }

        return Math.Min(toc + 1, lines.Count);
    }

    private static bool IsTocMarker(string line) => line is "目录" or "目 录";

    private static List<SectionDraft> GroupSections(IReadOnlyList<string> body)
    {
        var sections = new List<SectionDraft>();
        SectionDraft? current = null;
        string? chapterNo = null;
        string? chapterTitle = null;
        var appendix = false;

        void Flush()
        {
            if (current != null)
                sections.Add(current);
            current = null;
        }

        foreach (var line in body)
        {
            var kind = Classify(line);
            if (kind == LineKind.Chapter)
            {
                Flush();
                var match = ChapterRegex.Match(line);
                appendix = false;
                chapterNo = match.Groups["no"].Value;
                chapterTitle = EmptyToNull(match.Groups["title"].Value);
                current = NewSection(chapterNo, chapterTitle, appendix, null, null, line);
                continue;
            }

            if (kind == LineKind.Appendix)
            {
                Flush();
                var match = AppendixRegex.Match(line);
                appendix = true;
                chapterNo = EmptyToNull(match.Groups["no"].Value);
                chapterTitle = EmptyToNull(match.Groups["title"].Value);
                current = NewSection(chapterNo, chapterTitle, appendix, null, null, line);
                continue;
            }

            if (kind == LineKind.Section)
            {
                Flush();
                var match = SectionRegex.Match(line);
                current = NewSection(
                    chapterNo,
                    chapterTitle,
                    appendix,
                    match.Groups["no"].Value,
                    EmptyToNull(match.Groups["title"].Value),
                    line);
                continue;
            }

            if (kind == LineKind.Case)
            {
                Flush();
                current = NewSection(chapterNo, chapterTitle, appendix, null, line, line);
                continue;
            }

            current ??= NewSection(chapterNo, chapterTitle, appendix, null, null, null);
            current.Body.Add(line);
        }

        Flush();
        return sections;
    }

    private static SectionDraft NewSection(
        string? chapterNo,
        string? chapterTitle,
        bool appendix,
        string? sectionNo,
        string? sectionTitle,
        string? headingSource)
    {
        return new SectionDraft
        {
            ChapterNo = chapterNo,
            ChapterTitle = chapterTitle,
            Appendix = appendix,
            SectionNo = sectionNo,
            SectionTitle = sectionTitle,
            Heading = BuildHeading(chapterNo, chapterTitle, appendix, sectionNo, sectionTitle, headingSource)
        };
    }

    private static string BuildHeading(
        string? chapterNo,
        string? chapterTitle,
        bool appendix,
        string? sectionNo,
        string? sectionTitle,
        string? headingSource)
    {
        if (appendix)
        {
            var head = string.IsNullOrEmpty(chapterNo) ? "附录" : $"附录 {chapterNo}";
            if (sectionNo == null && !string.IsNullOrEmpty(chapterTitle))
                head += " " + chapterTitle;
            if (!string.IsNullOrEmpty(sectionNo))
                head += " " + sectionNo;
            if (!string.IsNullOrEmpty(sectionTitle) && sectionTitle != headingSource)
                head += " " + sectionTitle;
            else if (sectionNo == null && sectionTitle == headingSource && !string.IsNullOrEmpty(sectionTitle))
                head += " " + sectionTitle;
            return head.Trim();
        }

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(chapterNo))
            parts.Add($"第{chapterNo}章");
        if (!string.IsNullOrEmpty(sectionNo))
            parts.Add(sectionNo);

        var title = !string.IsNullOrEmpty(sectionTitle)
            ? sectionTitle
            : sectionNo == null ? chapterTitle : null;
        if (!string.IsNullOrEmpty(title))
            parts.Add(title);
        var heading = string.Join(' ', parts);
        return heading.Length > 0 ? heading : headingSource ?? "";
    }

    private static IEnumerable<RawChunk> SplitSection(SectionDraft section)
    {
        var body = string.Join('\n', section.Body);
        var headingPrefix = section.Heading.Length == 0 ? "" : section.Heading + "\n";
        if (body.Length == 0)
        {
            if (headingPrefix.Length == 0)
                yield break;
            yield return section.ToChunk(section.Heading);
            yield break;
        }

        var budget = Math.Max(1, HardMax - headingPrefix.Length);
        var target = Math.Max(1, Math.Min(TargetLength - headingPrefix.Length, budget));
        if (headingPrefix.Length + body.Length <= HardMax)
        {
            yield return section.ToChunk(headingPrefix + body);
            yield break;
        }

        var start = 0;
        var first = true;
        while (start < body.Length)
        {
            var overlap = 0;
            if (!first)
                overlap = Math.Min(Overlap, start);
            var room = Math.Max(1, budget - overlap);
            var preferred = Math.Max(1, Math.Min(target - overlap, room));
            var cut = NextCut(body, start, preferred, room);
            var sliceStart = start - overlap;
            yield return section.ToChunk(headingPrefix + body[sliceStart..cut]);
            if (cut <= start)
                cut = Math.Min(body.Length, start + room);
            start = cut;
            first = false;
        }
    }

    private static int NextCut(string body, int start, int preferredLen, int hardLen)
    {
        var remaining = body.Length - start;
        if (remaining <= hardLen)
            return body.Length;

        var preferred = Math.Min(start + preferredLen, body.Length);
        var limit = Math.Min(start + hardLen, body.Length);
        var cut = LastBreak(body, start + 1, preferred);
        if (cut < 0)
            cut = LastBreak(body, preferred, limit);
        if (cut < 0)
            cut = limit;
        return cut;
    }

    private static int LastBreak(string text, int from, int to)
    {
        if (to <= from)
            return -1;
        for (var i = to - 1; i >= from; i--)
        {
            var ch = text[i];
            if (ch is '。' or '；' or '\n')
                return i + 1;
        }

        return -1;
    }

    private static void MergeShortChunks(List<RawChunk> chunks)
    {
        for (var i = 0; i < chunks.Count - 1;)
        {
            if (chunks[i].Content.Length >= MinMerge)
            {
                i++;
                continue;
            }

            var next = chunks[i + 1];
            next.Content = chunks[i].Content + "\n" + next.Content;
            chunks.RemoveAt(i);
        }
    }

    private static LineKind Classify(string line)
    {
        if (ChapterRegex.IsMatch(line))
            return LineKind.Chapter;
        if (AppendixRegex.IsMatch(line))
            return LineKind.Appendix;
        if (SectionRegex.IsMatch(line))
            return LineKind.Section;
        if (line.StartsWith("【案例", StringComparison.Ordinal))
            return LineKind.Case;
        return LineKind.Body;
    }

    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Sha256Hex(string content)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private enum LineKind
    {
        Body,
        Chapter,
        Section,
        Appendix,
        Case
    }

    private sealed class SectionDraft
    {
        public string? ChapterNo { get; init; }
        public string? ChapterTitle { get; init; }
        public bool Appendix { get; init; }
        public string? SectionNo { get; init; }
        public string? SectionTitle { get; init; }
        public string Heading { get; init; } = "";
        public List<string> Body { get; } = new();

        public RawChunk ToChunk(string content) => new()
        {
            ChapterNo = ChapterNo,
            ChapterTitle = ChapterTitle,
            SectionNo = SectionNo,
            SectionTitle = SectionTitle,
            Heading = Heading,
            Content = content
        };
    }

    private sealed class RawChunk
    {
        public string? ChapterNo { get; init; }
        public string? ChapterTitle { get; init; }
        public string? SectionNo { get; init; }
        public string? SectionTitle { get; init; }
        public string Heading { get; init; } = "";
        public string Content { get; set; } = "";
    }
}
