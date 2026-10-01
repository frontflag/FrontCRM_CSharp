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
    public int LikeCount { get; set; }
    public int DislikeCount { get; set; }
    /// <summary>当前用户对该主题的态度：1赞 -1踩 0无。</summary>
    public int MyReaction { get; set; }
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

public class BbsReactionRequest
{
    /// <summary>1=赞 -1=踩 0=取消</summary>
    public int Value { get; set; }
}

public class BbsReactionResultDto
{
    public int LikeCount { get; set; }
    public int DislikeCount { get; set; }
    public int MyReaction { get; set; }
}

public class BbsReplyPagedDto
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public IReadOnlyList<BbsReplyDto> Items { get; set; } = Array.Empty<BbsReplyDto>();
}

/// <summary>侧栏版块统计：帖子数、浏览合计。</summary>
public class BbsBoardStatItemDto
{
    /// <summary>all / top / 类型数字字符串。</summary>
    public string Key { get; set; } = string.Empty;
    public int? Type { get; set; }
    public int SubjectCount { get; set; }
    public int ViewCount { get; set; }
}

public class BbsBoardStatsDto
{
    public BbsBoardStatItemDto All { get; set; } = new() { Key = "all" };
    public BbsBoardStatItemDto Top { get; set; } = new() { Key = "top" };
    public IReadOnlyList<BbsBoardStatItemDto> ByType { get; set; } = Array.Empty<BbsBoardStatItemDto>();
}

public class BbsMediaItemDto
{
    public string Id { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public string? FileExtension { get; set; }
    public long FileSize { get; set; }
    /// <summary>image | video</summary>
    public string Kind { get; set; } = "image";
    public string PreviewPath { get; set; } = string.Empty;
}

public class BbsMediaUploadFile
{
    public Stream Stream { get; set; } = Stream.Null;
    public string FileName { get; set; } = "file";
    public string? ContentType { get; set; }
    public long Length { get; set; }
}

/// <summary>板块设置（版主 + 自定义名称）。</summary>
public class BbsBoardModeratorDto
{
    public int Type { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? RealName { get; set; }
    /// <summary>自定义名称；空表示使用默认。</summary>
    public string? DisplayName { get; set; }
    /// <summary>默认名称（系统文案）。</summary>
    public string DefaultName { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    /// <summary>该板块未删除主帖数量。</summary>
    public int SubjectCount { get; set; }
    /// <summary>显示顺序（越小越靠前）；仅可配置板块有效。</summary>
    public int SortOrder { get; set; }
}

public class BbsBoardModeratorSetRequest
{
    /// <summary>版主用户 Id；null 或空串表示清空版主。</summary>
    public string? UserId { get; set; }

    /// <summary>板块显示名；空串表示恢复默认名称。</summary>
    public string? DisplayName { get; set; }

    /// <summary>显示顺序（1 起）；越小越靠前。</summary>
    public int? SortOrder { get; set; }
}

public class BbsBoardCreateRequest
{
    /// <summary>新建板块名称（必填）。</summary>
    public string? DisplayName { get; set; }
}

public class BbsBoardReorderRequest
{
    /// <summary>可拖放板块按目标显示顺序排列的 type 列表。</summary>
    public List<int> OrderedTypes { get; set; } = [];
}
