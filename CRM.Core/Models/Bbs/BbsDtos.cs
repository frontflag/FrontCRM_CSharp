using CRM.Core.Constants;

namespace CRM.Core.Models.Bbs;

public class BbsSubjectQuery
{
    public int? Type { get; set; }
    public int? Status { get; set; }
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class BbsSubjectCreateRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Type { get; set; } = BbsSubjectTypes.Share;
    public bool Anonymous { get; set; }
}

public class BbsSubjectUpdateRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Type { get; set; }
    public bool Anonymous { get; set; }
}

public class BbsReplyCreateRequest
{
    public string Content { get; set; } = string.Empty;
    public bool Anonymous { get; set; }
}

public class BbsSubjectListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Type { get; set; }
    public string TypeLabel { get; set; } = string.Empty;
    public int Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public bool IsTop { get; set; }
    public bool IsHot { get; set; }
    public bool Anonymous { get; set; }
    public int ViewCount { get; set; }
    public int ReplyCount { get; set; }
    public DateTime? LastReplyTime { get; set; }
    public DateTime CreateTime { get; set; }
    public string? CreateBy { get; set; }
    public string AuthorDisplay { get; set; } = string.Empty;
    public bool CanDelete { get; set; }
    public bool CanEdit { get; set; }
    public bool CanSetTop { get; set; }
    public bool CanModerate { get; set; }
}

public class BbsSubjectDetailDto : BbsSubjectListItemDto
{
    public string Content { get; set; } = string.Empty;
}

public class BbsSubjectPagedDto
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public IReadOnlyList<BbsSubjectListItemDto> Items { get; set; } = Array.Empty<BbsSubjectListItemDto>();
}

public class BbsReplyDto
{
    public string Id { get; set; } = string.Empty;
    public string SubjectId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool Anonymous { get; set; }
    public DateTime CreateTime { get; set; }
    public string? CreateBy { get; set; }
    public string AuthorDisplay { get; set; } = string.Empty;
    public bool CanDelete { get; set; }
}

public class BbsReplyPagedDto
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public IReadOnlyList<BbsReplyDto> Items { get; set; } = Array.Empty<BbsReplyDto>();
}
