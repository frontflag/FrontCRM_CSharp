using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Services;

public class SysUserNoticeService : ISysUserNoticeService
{
    private readonly ApplicationDbContext _db;

    public SysUserNoticeService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SysUserNoticeRecipientDto>> ListRecipientsAsync(CancellationToken ct = default)
    {
        return await _db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.Status == UserAccountStatus.Active)
            .OrderBy(u => u.UserName)
            .Select(u => new SysUserNoticeRecipientDto
            {
                Id = u.Id,
                UserName = u.UserName,
                RealName = u.RealName
            })
            .ToListAsync(ct);
    }

    public async Task<SysUserNoticeAdminPagedDto> AdminListAsync(SysUserNoticeAdminQuery query, CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100);

        var q = _db.SysUserNotices.AsNoTracking().AsQueryable();
        if (query.IsUrgent.HasValue)
            q = q.Where(x => x.IsUrgent == query.IsUrgent.Value);
        if (query.IsRead.HasValue)
            q = query.IsRead.Value
                ? q.Where(x => x.ReadAt != null)
                : q.Where(x => x.ReadAt == null);
        if (!string.IsNullOrWhiteSpace(query.RecipientUserId))
        {
            var rid = query.RecipientUserId.Trim();
            q = q.Where(x => x.RecipientUserId == rid);
        }
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = "%" + query.Keyword.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            q = q.Where(x => EF.Functions.ILike(x.Title, kw) || EF.Functions.ILike(x.Body, kw));
        }
        if (query.SendFrom.HasValue)
        {
            var from = ToUtcInclusiveStart(query.SendFrom.Value);
            q = q.Where(x => x.CreateTime >= from);
        }
        if (query.SendTo.HasValue)
        {
            var to = ToUtcExclusiveEnd(query.SendTo.Value);
            q = q.Where(x => x.CreateTime < to);
        }

        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderByDescending(x => x.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var recipientIds = rows.Select(x => x.RecipientUserId).Distinct().ToList();
        var names = await LoadUserLabelsAsync(recipientIds, ct);

        return new SysUserNoticeAdminPagedDto
        {
            Items = rows.Select(x => new SysUserNoticeAdminListItemDto
            {
                Id = x.Id,
                IsUrgent = x.IsUrgent,
                IsRead = x.ReadAt != null,
                RecipientUserId = x.RecipientUserId,
                RecipientLabel = names.GetValueOrDefault(x.RecipientUserId, x.RecipientUserId),
                Title = x.Title,
                BodyPreview = MakeBodyPreview(x.Body),
                CreateTime = x.CreateTime
            }).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<SysUserNoticeDetailDto?> AdminGetAsync(string id, CancellationToken ct = default)
    {
        var row = await _db.SysUserNotices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (row == null) return null;
        return await ToDetailAsync(row, ct);
    }

    public async Task<SysUserNoticeDetailDto> AdminSendAsync(
        SysUserNoticeSendRequest request,
        string senderUserId,
        CancellationToken ct = default)
    {
        var recipientId = (request.RecipientUserId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(recipientId))
            throw new InvalidOperationException("请选择接收人");

        var recipientOk = await _db.Users.AsNoTracking()
            .AnyAsync(u => u.Id == recipientId && u.IsActive && u.Status == UserAccountStatus.Active, ct);
        if (!recipientOk)
            throw new InvalidOperationException("接收人不是启用中的员工");

        var title = (request.Title ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidOperationException("请填写标题");
        if (title.Length > SysUserNoticeLimits.TitleMaxLength)
            throw new InvalidOperationException($"标题最长 {SysUserNoticeLimits.TitleMaxLength} 字符");

        var body = (request.Body ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("请填写正文");
        if (body.Length > SysUserNoticeLimits.BodyMaxLength)
            throw new InvalidOperationException($"正文最长 {SysUserNoticeLimits.BodyMaxLength} 字符");

        var entity = new SysUserNotice
        {
            RecipientUserId = recipientId,
            IsUrgent = request.IsUrgent,
            Title = title,
            Body = body,
            SenderUserId = senderUserId,
            CreateTime = DateTime.UtcNow
        };
        _db.SysUserNotices.Add(entity);
        await _db.SaveChangesAsync(ct);
        return await ToDetailAsync(entity, ct);
    }

    public async Task<IReadOnlyList<SysUserNoticeMeListItemDto>> ListMineAsync(string userId, CancellationToken ct = default)
    {
        var rows = await _db.SysUserNotices.AsNoTracking()
            .Where(x => x.RecipientUserId == userId)
            .OrderByDescending(x => x.CreateTime)
            .ToListAsync(ct);
        return rows.Select(x => new SysUserNoticeMeListItemDto
        {
            Id = x.Id,
            IsUrgent = x.IsUrgent,
            IsRead = x.ReadAt != null,
            Title = x.Title,
            BodyPreview = MakeBodyPreview(x.Body),
            CreateTime = x.CreateTime
        }).ToList();
    }

    public async Task<SysUserNoticeUnreadSummaryDto> GetUnreadSummaryAsync(string userId, CancellationToken ct = default)
    {
        var unread = await _db.SysUserNotices.AsNoTracking()
            .Where(x => x.RecipientUserId == userId && x.ReadAt == null)
            .Select(x => x.IsUrgent)
            .ToListAsync(ct);
        return new SysUserNoticeUnreadSummaryDto
        {
            UnreadCount = unread.Count,
            HasUnreadUrgent = unread.Any(u => u)
        };
    }

    public async Task<SysUserNoticeDetailDto?> GetMineAsync(string id, string userId, CancellationToken ct = default)
    {
        var row = await _db.SysUserNotices.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.RecipientUserId == userId, ct);
        if (row == null) return null;
        return await ToDetailAsync(row, ct);
    }

    public async Task MarkReadAsync(string id, string userId, CancellationToken ct = default)
    {
        var entity = await _db.SysUserNotices
            .FirstOrDefaultAsync(x => x.Id == id && x.RecipientUserId == userId, ct)
            ?? throw new InvalidOperationException("通知不存在");
        if (entity.ReadAt != null) return;
        entity.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(string userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await _db.SysUserNotices
            .Where(x => x.RecipientUserId == userId && x.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReadAt, now), ct);
    }

    private async Task<SysUserNoticeDetailDto> ToDetailAsync(SysUserNotice x, CancellationToken ct)
    {
        var names = await LoadUserLabelsAsync(new[] { x.RecipientUserId }, ct);
        return new SysUserNoticeDetailDto
        {
            Id = x.Id,
            IsUrgent = x.IsUrgent,
            IsRead = x.ReadAt != null,
            RecipientUserId = x.RecipientUserId,
            RecipientLabel = names.GetValueOrDefault(x.RecipientUserId, x.RecipientUserId),
            Title = x.Title,
            Body = x.Body,
            CreateTime = x.CreateTime,
            ReadAt = x.ReadAt
        };
    }

    private async Task<Dictionary<string, string>> LoadUserLabelsAsync(IEnumerable<string> userIds, CancellationToken ct)
    {
        var ids = userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<string, string>();

        var users = await _db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.UserName, u.RealName })
            .ToListAsync(ct);

        return users.ToDictionary(u => u.Id, u => FormatUserLabel(u.UserName, u.RealName));
    }

    private const int BodyPreviewMaxLength = 80;

    private static string MakeBodyPreview(string? body)
    {
        var text = string.Join(" ", (body ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (text.Length <= BodyPreviewMaxLength) return text;
        return text[..BodyPreviewMaxLength] + "…";
    }

    private static string FormatUserLabel(string userName, string? realName)
    {
        var name = (realName ?? string.Empty).Trim();
        return string.IsNullOrEmpty(name) ? userName : $"{userName} / {name}";
    }

    private static DateTime ToUtcInclusiveStart(DateTime value)
    {
        var d = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value.Date, DateTimeKind.Utc)
            : value.ToUniversalTime().Date;
        return DateTime.SpecifyKind(d, DateTimeKind.Utc);
    }

    private static DateTime ToUtcExclusiveEnd(DateTime value)
    {
        return ToUtcInclusiveStart(value).AddDays(1);
    }
}
