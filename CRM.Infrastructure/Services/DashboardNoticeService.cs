using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Bbs;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Services;

public class DashboardNoticeService : IDashboardNoticeService
{
    private readonly ApplicationDbContext _db;

    public DashboardNoticeService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<DashboardNoticeItemDto>> ListAsync(string userId, CancellationToken ct = default)
    {
        var announcements = await _db.SysAnnouncements.AsNoTracking()
            .Where(x => x.Status == SysAnnouncementStatuses.Published)
            .Where(x => x.Delivery == SysAnnouncementDeliveries.Desktop)
            .Where(x => !_db.SysAnnouncementReads.Any(r => r.AnnouncementId == x.Id && r.UserId == userId))
            .Select(x => new DashboardNoticeItemDto
            {
                Kind = DashboardNoticeKinds.Announcement,
                Id = x.Id,
                Title = x.Title,
                At = x.PublishedAt ?? x.CreateTime
            })
            .ToListAsync(ct);

        var cutoff = DashboardNoticeRules.BbsSystemUpdateNotBeforeUtc;
        var posts = await _db.BbsSubjects.AsNoTracking()
            .Where(x => x.Type == BbsSubjectTypes.SystemUpdate && !x.IsDeleted)
            .Where(x => x.CreateTime >= cutoff)
            .Where(x => !_db.BbsDashboardNoticeDismisses.Any(d => d.SubjectId == x.Id && d.UserId == userId))
            .Select(x => new DashboardNoticeItemDto
            {
                Kind = DashboardNoticeKinds.Bbs,
                Id = x.Id,
                Title = x.Title,
                At = x.CreateTime
            })
            .ToListAsync(ct);

        return announcements
            .Concat(posts)
            .OrderByDescending(x => x.At)
            .ThenByDescending(x => x.Id)
            .ToList();
    }

    public async Task DismissBbsAsync(string subjectId, string userId, CancellationToken ct = default)
    {
        var cutoff = DashboardNoticeRules.BbsSystemUpdateNotBeforeUtc;
        var subject = await _db.BbsSubjects.AsNoTracking()
            .Where(x => x.Id == subjectId && x.Type == BbsSubjectTypes.SystemUpdate && !x.IsDeleted)
            .Select(x => new { x.Id, x.CreateTime })
            .FirstOrDefaultAsync(ct);
        if (subject == null || subject.CreateTime < cutoff)
            throw new InvalidOperationException("系统更新不存在");

        var already = await _db.BbsDashboardNoticeDismisses.AsNoTracking()
            .AnyAsync(x => x.SubjectId == subjectId && x.UserId == userId, ct);
        if (already) return;

        _db.BbsDashboardNoticeDismisses.Add(new BbsDashboardNoticeDismiss
        {
            SubjectId = subjectId,
            UserId = userId,
            DismissedAt = DateTime.UtcNow
        });
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // 并发下唯一约束冲突视为已消失
        }
    }
}
