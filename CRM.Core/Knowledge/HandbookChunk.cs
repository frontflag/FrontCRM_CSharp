namespace CRM.Core.Knowledge;

public sealed class HandbookChunk
{
    public string? ChapterNo { get; init; }
    public string? ChapterTitle { get; init; }
    public string? SectionNo { get; init; }
    public string? SectionTitle { get; init; }
    public string Heading { get; init; } = "";
    public string Content { get; init; } = "";
    public int ChunkIndex { get; init; }
    public string ContentSha256 { get; init; } = "";
    public int ContentChars => Content.Length;
}
