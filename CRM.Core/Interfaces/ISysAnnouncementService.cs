using CRM.Core.Models.System;

namespace CRM.Core.Interfaces;

public interface ISysAnnouncementService
{
    Task<IReadOnlyList<SysAnnouncementAdminListItemDto>> AdminListAsync(
        string? status,
        string? type = null,
        CancellationToken ct = default);
    Task<SysAnnouncementDetailDto?> AdminGetAsync(string id, CancellationToken ct = default);
    Task<SysAnnouncementDetailDto> AdminCreateAsync(SysAnnouncementUpsertRequest request, string userId, CancellationToken ct = default);
    Task<SysAnnouncementDetailDto> AdminUpdateAsync(string id, SysAnnouncementUpsertRequest request, string userId, CancellationToken ct = default);
    Task AdminDeleteAsync(string id, CancellationToken ct = default);
    Task<SysAnnouncementDetailDto> AdminPublishAsync(string id, string userId, CancellationToken ct = default);

    Task<SysAnnouncementUnreadSummaryDto> GetUnreadSummaryAsync(string userId, CancellationToken ct = default);
    Task<SysAnnouncementUnreadPreviewDto> GetUnreadPreviewAsync(string userId, int limit = 5, CancellationToken ct = default);
    Task<IReadOnlyList<SysAnnouncementHistoryItemDto>> GetHistoryAsync(string userId, CancellationToken ct = default);
    Task<SysAnnouncementDetailDto?> GetPublishedAsync(string id, CancellationToken ct = default);
    Task MarkReadAsync(string id, string userId, CancellationToken ct = default);
}

public class SysAnnouncementUpsertRequest
{
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = SysAnnouncementTypes.PlatformNotice;
    public string BodyMd { get; set; } = string.Empty;
}

public class SysAnnouncementAdminListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ModifyTime { get; set; }
}

public class SysAnnouncementDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string BodyMd { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? PublishedBy { get; set; }
    public DateTime? ModifyTime { get; set; }
}

public class SysAnnouncementUnreadSummaryDto
{
    public int TotalUnread { get; set; }
}

public class SysAnnouncementUnreadPreviewDto
{
    public int TotalUnread { get; set; }
    public IReadOnlyList<SysAnnouncementDetailDto> Items { get; set; } = Array.Empty<SysAnnouncementDetailDto>();
}

public class SysAnnouncementHistoryItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    public bool IsRead { get; set; }
}
