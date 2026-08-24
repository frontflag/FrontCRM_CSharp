using CRM.Core.Document;

namespace CRM.Core.Interfaces;

public interface ISysUserNoticeService
{
    Task<IReadOnlyList<SysUserNoticeRecipientDto>> ListRecipientsAsync(CancellationToken ct = default);

    Task<SysUserNoticeAdminPagedDto> AdminListAsync(SysUserNoticeAdminQuery query, CancellationToken ct = default);

    Task<SysUserNoticeDetailDto?> AdminGetAsync(string id, CancellationToken ct = default);

    Task<SysUserNoticeDetailDto> AdminSendAsync(
        SysUserNoticeSendRequest request,
        string senderUserId,
        IReadOnlyList<DocumentUploadFile>? images,
        CancellationToken ct = default);

    Task<IReadOnlyList<SysUserNoticeMeListItemDto>> ListMineAsync(string userId, CancellationToken ct = default);

    Task<SysUserNoticeUnreadSummaryDto> GetUnreadSummaryAsync(string userId, CancellationToken ct = default);

    Task<SysUserNoticeDetailDto?> GetMineAsync(string id, string userId, CancellationToken ct = default);

    Task MarkReadAsync(string id, string userId, CancellationToken ct = default);
    Task MarkAllReadAsync(string userId, CancellationToken ct = default);

    /// <summary>系统通知附图：仅 SuperAdmin、接收人或发送人可预览/下载。</summary>
    Task<bool> CanAccessNoticeAttachmentAsync(string documentId, string userId, bool isSysAdmin, CancellationToken ct = default);

    /// <summary>按通知 Id 判断是否可列出该通知附图。</summary>
    Task<bool> CanAccessNoticeBizAsync(string noticeId, string userId, bool isSysAdmin, CancellationToken ct = default);
}

public class SysUserNoticeAdminQuery
{
    public bool? IsUrgent { get; set; }
    public bool? IsRead { get; set; }
    public string? RecipientUserId { get; set; }
    public string? Keyword { get; set; }
    public DateTime? SendFrom { get; set; }
    public DateTime? SendTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class SysUserNoticeSendRequest
{
    public string RecipientUserId { get; set; } = string.Empty;
    public bool IsUrgent { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class SysUserNoticeRecipientDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? RealName { get; set; }
}

public class SysUserNoticeAdminListItemDto
{
    public string Id { get; set; } = string.Empty;
    public bool IsUrgent { get; set; }
    public bool IsRead { get; set; }
    public string RecipientUserId { get; set; } = string.Empty;
    public string RecipientLabel { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string BodyPreview { get; set; } = string.Empty;
    public int ImageCount { get; set; }
    public DateTime CreateTime { get; set; }
}

public class SysUserNoticeAdminPagedDto
{
    public IReadOnlyList<SysUserNoticeAdminListItemDto> Items { get; set; } = Array.Empty<SysUserNoticeAdminListItemDto>();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class SysUserNoticeDetailDto
{
    public string Id { get; set; } = string.Empty;
    public bool IsUrgent { get; set; }
    public bool IsRead { get; set; }
    public string RecipientUserId { get; set; } = string.Empty;
    public string RecipientLabel { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public IReadOnlyList<SysUserNoticeImageDto> Images { get; set; } = Array.Empty<SysUserNoticeImageDto>();
    public DateTime CreateTime { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class SysUserNoticeImageDto
{
    public string DocumentId { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
}

public class SysUserNoticeMeListItemDto
{
    public string Id { get; set; } = string.Empty;
    public bool IsUrgent { get; set; }
    public bool IsRead { get; set; }
    public string Title { get; set; } = string.Empty;
    public string BodyPreview { get; set; } = string.Empty;
    public int ImageCount { get; set; }
    public DateTime CreateTime { get; set; }
}

public class SysUserNoticeUnreadSummaryDto
{
    public int UnreadCount { get; set; }
    public bool HasUnreadUrgent { get; set; }
}
