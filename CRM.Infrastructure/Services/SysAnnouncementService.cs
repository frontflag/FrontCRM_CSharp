using System.Text;
using CRM.Core.Interfaces;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Services;

public class SysAnnouncementService : ISysAnnouncementService
{
    private readonly ApplicationDbContext _db;

    public SysAnnouncementService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SysAnnouncementAdminListItemDto>> AdminListAsync(
        string? status,
        string? type = null,
        CancellationToken ct = default)
    {
        var q = _db.SysAnnouncements.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim().ToLowerInvariant();
            q = q.Where(x => x.Status == s);
        }
        if (!string.IsNullOrWhiteSpace(type))
        {
            var t = type.Trim().ToLowerInvariant();
            q = q.Where(x => x.Type == t);
        }

        return await q
            .OrderByDescending(x => x.PublishedAt ?? x.CreateTime)
            .ThenByDescending(x => x.CreateTime)
            .Select(x => new SysAnnouncementAdminListItemDto
            {
                Id = x.Id,
                Title = x.Title,
                Type = x.Type,
                Status = x.Status,
                CreateTime = x.CreateTime,
                PublishedAt = x.PublishedAt,
                ModifyTime = x.ModifyTime
            })
            .ToListAsync(ct);
    }

    public async Task<SysAnnouncementDetailDto?> AdminGetAsync(string id, CancellationToken ct = default)
    {
        var row = await _db.SysAnnouncements.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return row == null ? null : ToDetail(row);
    }

    public async Task<SysAnnouncementDetailDto> AdminCreateAsync(
        SysAnnouncementUpsertRequest request,
        string userId,
        CancellationToken ct = default)
    {
        ValidateUpsert(request);
        var now = DateTime.UtcNow;
        var entity = new SysAnnouncement
        {
            Title = request.Title.Trim(),
            Type = NormalizeType(request.Type),
            BodyMd = request.BodyMd ?? string.Empty,
            Status = SysAnnouncementStatuses.Draft,
            CreateTime = now,
            CreateBy = userId,
            ModifyTime = now,
            ModifyBy = userId
        };
        _db.SysAnnouncements.Add(entity);
        await _db.SaveChangesAsync(ct);
        return ToDetail(entity);
    }

    public async Task<SysAnnouncementDetailDto> AdminUpdateAsync(
        string id,
        SysAnnouncementUpsertRequest request,
        string userId,
        CancellationToken ct = default)
    {
        ValidateUpsert(request);
        var entity = await _db.SysAnnouncements.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("公告不存在");
        if (entity.Status != SysAnnouncementStatuses.Draft)
            throw new InvalidOperationException("已发布的公告不可编辑");

        entity.Title = request.Title.Trim();
        entity.Type = NormalizeType(request.Type);
        entity.BodyMd = request.BodyMd ?? string.Empty;
        entity.ModifyTime = DateTime.UtcNow;
        entity.ModifyBy = userId;
        await _db.SaveChangesAsync(ct);
        return ToDetail(entity);
    }

    public async Task AdminDeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.SysAnnouncements.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("公告不存在");
        if (entity.Status != SysAnnouncementStatuses.Draft)
            throw new InvalidOperationException("已发布的公告不可删除");

        _db.SysAnnouncements.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<SysAnnouncementDetailDto> AdminPublishAsync(
        string id,
        string userId,
        CancellationToken ct = default)
    {
        var entity = await _db.SysAnnouncements.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InvalidOperationException("公告不存在");
        if (entity.Status != SysAnnouncementStatuses.Draft)
            throw new InvalidOperationException("仅草稿可发布");
        if (string.IsNullOrWhiteSpace(entity.Title))
            throw new InvalidOperationException("标题不能为空");
        if (string.IsNullOrWhiteSpace(entity.BodyMd))
            throw new InvalidOperationException("正文不能为空");

        entity.Status = SysAnnouncementStatuses.Published;
        entity.PublishedAt = DateTime.UtcNow;
        entity.PublishedBy = userId;
        entity.ModifyTime = entity.PublishedAt;
        entity.ModifyBy = userId;
        await _db.SaveChangesAsync(ct);
        return ToDetail(entity);
    }

    public async Task<SysAnnouncementUnreadSummaryDto> GetUnreadSummaryAsync(
        string userId,
        CancellationToken ct = default)
    {
        var total = await CountUnreadAsync(userId, ct);
        return new SysAnnouncementUnreadSummaryDto { TotalUnread = total };
    }

    public async Task<SysAnnouncementUnreadPreviewDto> GetUnreadPreviewAsync(
        string userId,
        int limit = 5,
        CancellationToken ct = default)
    {
        if (limit < 1) limit = 5;
        if (limit > 5) limit = 5;

        var total = await CountUnreadAsync(userId, ct);
        var items = await UnreadQuery(userId)
            .OrderByDescending(x => x.PublishedAt)
            .Take(limit)
            .ToListAsync(ct);

        return new SysAnnouncementUnreadPreviewDto
        {
            TotalUnread = total,
            Items = items.Select(ToDetail).ToList()
        };
    }

    public async Task<IReadOnlyList<SysAnnouncementHistoryItemDto>> GetHistoryAsync(
        string userId,
        CancellationToken ct = default)
    {
        var readIds = await _db.SysAnnouncementReads.AsNoTracking()
            .Where(r => r.UserId == userId)
            .Select(r => r.AnnouncementId)
            .ToListAsync(ct);
        var readSet = readIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var rows = await _db.SysAnnouncements.AsNoTracking()
            .Where(x => x.Status == SysAnnouncementStatuses.Published)
            .OrderByDescending(x => x.PublishedAt)
            .Select(x => new { x.Id, x.Title, x.Type, x.PublishedAt })
            .ToListAsync(ct);

        return rows.Select(x => new SysAnnouncementHistoryItemDto
        {
            Id = x.Id,
            Title = x.Title,
            Type = x.Type,
            PublishedAt = x.PublishedAt,
            IsRead = readSet.Contains(x.Id)
        }).ToList();
    }

    public async Task<SysAnnouncementDetailDto?> GetPublishedAsync(string id, CancellationToken ct = default)
    {
        var row = await _db.SysAnnouncements.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.Status == SysAnnouncementStatuses.Published, ct);
        return row == null ? null : ToDetail(row);
    }

    public async Task MarkReadAsync(string id, string userId, CancellationToken ct = default)
    {
        var exists = await _db.SysAnnouncements.AsNoTracking()
            .AnyAsync(x => x.Id == id && x.Status == SysAnnouncementStatuses.Published, ct);
        if (!exists)
            throw new InvalidOperationException("公告不存在或未发布");

        var already = await _db.SysAnnouncementReads.AsNoTracking()
            .AnyAsync(r => r.AnnouncementId == id && r.UserId == userId, ct);
        if (already) return;

        _db.SysAnnouncementReads.Add(new SysAnnouncementRead
        {
            AnnouncementId = id,
            UserId = userId,
            ReadAt = DateTime.UtcNow
        });
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // 并发下唯一约束冲突视为已读成功
        }
    }

    private IQueryable<SysAnnouncement> UnreadQuery(string userId) =>
        _db.SysAnnouncements.AsNoTracking()
            .Where(x => x.Status == SysAnnouncementStatuses.Published)
            .Where(x => !_db.SysAnnouncementReads.Any(r =>
                r.AnnouncementId == x.Id && r.UserId == userId));

    private Task<int> CountUnreadAsync(string userId, CancellationToken ct) =>
        UnreadQuery(userId).CountAsync(ct);

    private static void ValidateUpsert(SysAnnouncementUpsertRequest request)
    {
        var title = (request.Title ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(title))
            throw new InvalidOperationException("标题不能为空");
        if (title.Length > SysAnnouncementLimits.TitleMaxLength)
            throw new InvalidOperationException($"标题最长 {SysAnnouncementLimits.TitleMaxLength} 字符");

        var body = request.BodyMd ?? string.Empty;
        var bytes = Encoding.UTF8.GetByteCount(body);
        if (bytes > SysAnnouncementLimits.BodyMaxBytes)
            throw new InvalidOperationException("正文超过 50KB 限制");

        if (!string.IsNullOrWhiteSpace(request.Type) && !SysAnnouncementTypes.IsValid(request.Type.Trim()))
            throw new InvalidOperationException("公告类型无效");
    }

    private static string NormalizeType(string? type)
    {
        var t = (type ?? string.Empty).Trim();
        return SysAnnouncementTypes.IsValid(t) ? t : SysAnnouncementTypes.PlatformNotice;
    }

    private static SysAnnouncementDetailDto ToDetail(SysAnnouncement x) => new()
    {
        Id = x.Id,
        Title = x.Title,
        Type = x.Type,
        BodyMd = x.BodyMd,
        Status = x.Status,
        CreateTime = x.CreateTime,
        PublishedAt = x.PublishedAt,
        PublishedBy = x.PublishedBy,
        ModifyTime = x.ModifyTime
    };
}
