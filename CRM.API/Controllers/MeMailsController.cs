using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.API.Services.Interfaces;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/me/mails")]
[Authorize]
public sealed class MeMailsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IUserMailSyncService _sync;

    public MeMailsController(ApplicationDbContext db, IUserMailSyncService sync)
    {
        _db = db;
        _sync = sync;
    }

    private string? CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<MyMailSummaryDto>>> Summary(CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(ApiResponse<MyMailSummaryDto>.Fail("未登录", 401));

        var verifiedCount = await _db.UserMailboxes.AsNoTracking()
            .CountAsync(x => x.UserId == userId && !x.IsDeleted && x.VerifyStatus == UserMailboxVerifyStatus.Ok, ct);

        var q = _db.UserMailMessages.AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsDeleted);
        var total = await q.CountAsync(ct);
        var unread = await q.CountAsync(x => x.IsUnread, ct);

        return Ok(ApiResponse<MyMailSummaryDto>.Ok(new MyMailSummaryDto
        {
            HasVerifiedMailbox = verifiedCount > 0,
            VerifiedMailboxCount = verifiedCount,
            TotalCount = total,
            UnreadCount = unread
        }));
    }

    [HttpGet("mailboxes")]
    public async Task<ActionResult<ApiResponse<List<MyMailMailboxOptionDto>>>> Mailboxes(CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(ApiResponse<List<MyMailMailboxOptionDto>>.Fail("未登录", 401));

        var rows = await _db.UserMailboxes.AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsDeleted && x.VerifyStatus == UserMailboxVerifyStatus.Ok)
            .OrderBy(x => x.Address)
            .Select(x => new MyMailMailboxOptionDto
            {
                Id = x.Id,
                Address = x.Address,
                Kind = x.Kind == UserMailboxKind.Personal ? "personal" : "platform",
                DisplayName = x.DisplayName
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<List<MyMailMailboxOptionDto>>.Ok(rows));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResultDto<MyMailListItemDto>>>> List(
        [FromQuery] string? mailboxId,
        [FromQuery] string? subject,
        [FromQuery] string? from,
        [FromQuery] string? body,
        [FromQuery] DateTime? receivedFrom,
        [FromQuery] DateTime? receivedTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(ApiResponse<PagedResultDto<MyMailListItemDto>>.Fail("未登录", 401));

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var q = from m in _db.UserMailMessages.AsNoTracking()
                join b in _db.UserMailboxes.AsNoTracking() on m.MailboxId equals b.Id
                where m.UserId == userId && !m.IsDeleted
                select new { m, b };

        if (!string.IsNullOrWhiteSpace(mailboxId))
            q = q.Where(x => x.m.MailboxId == mailboxId.Trim());
        if (!string.IsNullOrWhiteSpace(subject))
        {
            var s = subject.Trim();
            q = q.Where(x => x.m.Subject != null && x.m.Subject.Contains(s));
        }
        if (!string.IsNullOrWhiteSpace(from))
        {
            var f = from.Trim();
            q = q.Where(x =>
                (x.m.FromAddress != null && x.m.FromAddress.Contains(f))
                || (x.m.FromName != null && x.m.FromName.Contains(f)));
        }
        if (!string.IsNullOrWhiteSpace(body))
        {
            var b = body.Trim();
            q = q.Where(x =>
                (x.m.BodyText != null && x.m.BodyText.Contains(b))
                || (x.m.Snippet != null && x.m.Snippet.Contains(b)));
        }
        if (receivedFrom.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(receivedFrom.Value.Date, DateTimeKind.Utc);
            q = q.Where(x => x.m.ReceivedAt != null && x.m.ReceivedAt >= fromUtc);
        }
        if (receivedTo.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(receivedTo.Value.Date.AddDays(1), DateTimeKind.Utc);
            q = q.Where(x => x.m.ReceivedAt != null && x.m.ReceivedAt < toUtc);
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(x => x.m.ReceivedAt)
            .ThenByDescending(x => x.m.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new MyMailListItemDto
            {
                Id = x.m.Id,
                MailboxId = x.m.MailboxId,
                MailboxAddress = x.b.Address,
                Subject = x.m.Subject,
                Snippet = x.m.Snippet,
                FromAddress = x.m.FromAddress,
                FromName = x.m.FromName,
                ReceivedAt = x.m.ReceivedAt,
                IsUnread = x.m.IsUnread,
                HasAttachments = x.m.HasAttachments
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<PagedResultDto<MyMailListItemDto>>.Ok(new PagedResultDto<MyMailListItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        }));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<MyMailDetailDto>>> Get(string id, CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(ApiResponse<MyMailDetailDto>.Fail("未登录", 401));

        var row = await (
            from m in _db.UserMailMessages.AsNoTracking()
            join b in _db.UserMailboxes.AsNoTracking() on m.MailboxId equals b.Id
            where m.Id == id && m.UserId == userId && !m.IsDeleted
            select new MyMailDetailDto
            {
                Id = m.Id,
                MailboxId = m.MailboxId,
                MailboxAddress = b.Address,
                Subject = m.Subject,
                Snippet = m.Snippet,
                FromAddress = m.FromAddress,
                FromName = m.FromName,
                ToAddresses = m.ToAddresses,
                ReceivedAt = m.ReceivedAt,
                IsUnread = m.IsUnread,
                HasAttachments = m.HasAttachments,
                BodyText = m.BodyText,
                BodyHtml = m.BodyHtml,
                MessageId = m.MessageId
            }).FirstOrDefaultAsync(ct);

        if (row == null)
            return NotFound(ApiResponse<MyMailDetailDto>.Fail("邮件不存在", 404));

        return Ok(ApiResponse<MyMailDetailDto>.Ok(row));
    }

    [HttpPost("sync")]
    public async Task<ActionResult<ApiResponse<UserMailSyncResultDto>>> Sync(
        [FromBody] MyMailSyncRequest? body,
        CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(ApiResponse<UserMailSyncResultDto>.Fail("未登录", 401));

        var result = await _sync.SyncUserAsync(userId, body?.MailboxId, ct);
        if (result.Errors.Count > 0 && result.UpsertedCount == 0 && result.FetchedCount == 0)
            return Ok(ApiResponse<UserMailSyncResultDto>.Ok(result, string.Join("；", result.Errors)));

        return Ok(ApiResponse<UserMailSyncResultDto>.Ok(result, "同步完成"));
    }

    [HttpPost("{id}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkRead(string id, CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(ApiResponse<object>.Fail("未登录", 401));

        var entity = await _db.UserMailMessages
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && !x.IsDeleted, ct);
        if (entity == null)
            return NotFound(ApiResponse<object>.Fail("邮件不存在", 404));

        if (entity.IsUnread)
        {
            entity.IsUnread = false;
            entity.ModifyTime = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(ApiResponse<object>.Ok(null, "已标记已读"));
    }
}

public sealed class MyMailSummaryDto
{
    public bool HasVerifiedMailbox { get; set; }
    public int VerifiedMailboxCount { get; set; }
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
}

public sealed class MyMailMailboxOptionDto
{
    public string Id { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}

public class MyMailListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string MailboxId { get; set; } = string.Empty;
    public string MailboxAddress { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? Snippet { get; set; }
    public string? FromAddress { get; set; }
    public string? FromName { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public bool IsUnread { get; set; }
    public bool HasAttachments { get; set; }
}

public sealed class MyMailDetailDto : MyMailListItemDto
{
    public string? ToAddresses { get; set; }
    public string? BodyText { get; set; }
    public string? BodyHtml { get; set; }
    public string? MessageId { get; set; }
}

public sealed class MyMailSyncRequest
{
    public string? MailboxId { get; set; }
}

public sealed class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
