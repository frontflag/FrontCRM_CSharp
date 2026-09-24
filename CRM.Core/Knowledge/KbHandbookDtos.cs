namespace CRM.Core.Knowledge;

public sealed class KbImportResultDto
{
    public string DocumentId { get; set; } = "";
    public string VersionId { get; set; } = "";
    public int VersionNo { get; set; }
    public short Status { get; set; }
}

public sealed class KbDocumentVersionDto
{
    public string DocumentId { get; set; } = "";
    public string DocumentCode { get; set; } = "";
    public string Title { get; set; } = "";
    public string VersionId { get; set; } = "";
    public int VersionNo { get; set; }
    public short Status { get; set; }
    public bool IsActive { get; set; }
    public int ChunkCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string SourceFileName { get; set; } = "";
}

public sealed class KbChunkListItemDto
{
    public string Id { get; set; } = "";
    public int ChunkIndex { get; set; }
    public string Heading { get; set; } = "";
    public string? ChapterNo { get; set; }
    public string? SectionNo { get; set; }
    public int ContentChars { get; set; }
    public string Excerpt { get; set; } = "";
}

public sealed class KbCitationDto
{
    public string ChunkId { get; set; } = "";
    public string Heading { get; set; } = "";
    public string? ChapterNo { get; set; }
    public string? SectionNo { get; set; }
    public string Anchor { get; set; } = "";
    public string Excerpt { get; set; } = "";
    public double Distance { get; set; }
}

public sealed class KbAskResultDto
{
    public bool Covered { get; set; }
    public string Answer { get; set; } = "";
    public string? DocumentTitle { get; set; }
    public string? VersionId { get; set; }
    public int? VersionNo { get; set; }
    public bool FromCache { get; set; }
    public List<KbCitationDto> Citations { get; set; } = new();
}

public sealed class KbHandbookReaderDto
{
    public string VersionId { get; set; } = "";
    public int VersionNo { get; set; }
    public string Title { get; set; } = "";
    public List<KbHandbookChapterDto> Chapters { get; set; } = new();
}

public sealed class KbHandbookChapterDto
{
    public string Anchor { get; set; } = "";
    public string Title { get; set; } = "";
    public List<KbHandbookSectionDto> Sections { get; set; } = new();
}

public sealed class KbHandbookSectionDto
{
    public string Anchor { get; set; } = "";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
}
