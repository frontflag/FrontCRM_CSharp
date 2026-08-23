using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.API.Services;
using CRM.API.Services.Interfaces;
using CRM.Core.Interfaces;
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
    private readonly IEmailSender _emailSender;
    private readonly IDataPermissionService _dataPermission;
    private readonly IRbacService _rbac;

    public MeMailsController(
        ApplicationDbContext db,
        IUserMailSyncService sync,
        IEmailSender emailSender,
        IDataPermissionService dataPermission,
        IRbacService rbac)
    {
        _db = db;
        _sync = sync;
        _emailSender = emailSender;
        _dataPermission = dataPermission;
        _rbac = rbac;
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
            .Where(x => x.UserId == userId && !x.IsDeleted && x.Folder == InboxFolder);
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
                DisplayName = x.DisplayName,
                IsDefaultSend = x.IsDefaultSend
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<List<MyMailMailboxOptionDto>>.Ok(rows));
    }

    [HttpGet("address-book")]
    public async Task<ActionResult<ApiResponse<PagedResultDto<MyMailAddressBookItemDto>>>> AddressBook(
        [FromQuery(Name = "q")] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(ApiResponse<PagedResultDto<MyMailAddressBookItemDto>>.Fail("未登录", 401));

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var summary = await _rbac.GetUserPermissionSummaryAsync(userId);
        var canCustomer = summary.IsSysAdmin
            || summary.PermissionCodes.Contains("customer.read", StringComparer.OrdinalIgnoreCase);
        var canVendor = summary.IsSysAdmin
            || summary.PermissionCodes.Contains("vendor.read", StringComparer.OrdinalIgnoreCase);

        IQueryable<MyMailAddressBookItemDto>? q = null;
        if (canCustomer)
        {
            var customers = await _dataPermission.ApplyCustomerListDataScopeAsync(
                userId, _db.Customers.AsNoTracking(), ct);
            var customerRows =
                from ctact in _db.CustomerContacts.AsNoTracking()
                join c in customers on ctact.CustomerId equals c.Id
                where ctact.Email != null && ctact.Email != ""
                select new MyMailAddressBookItemDto
                {
                    Id = "customer:" + ctact.Id,
                    PartyKind = "customer",
                    PartyId = c.Id,
                    PartyName = c.OfficialName,
                    ContactName = ctact.CName != null && ctact.CName != "" ? ctact.CName : ctact.EName,
                    Email = ctact.Email!
                };
            q = customerRows;
        }

        if (canVendor)
        {
            var vendors = await _dataPermission.ApplyVendorListDataScopeAsync(
                userId, _db.Vendors.AsNoTracking(), ct);
            var vendorRows =
                from vtact in _db.VendorContacts.AsNoTracking()
                join v in vendors on vtact.VendorId equals v.Id
                where vtact.Email != null && vtact.Email != ""
                select new MyMailAddressBookItemDto
                {
                    Id = "vendor:" + vtact.Id,
                    PartyKind = "vendor",
                    PartyId = v.Id,
                    PartyName = v.OfficialName,
                    ContactName = vtact.CName != null && vtact.CName != "" ? vtact.CName : vtact.EName,
                    Email = vtact.Email!
                };
            q = q == null ? vendorRows : q.Concat(vendorRows);
        }

        if (q == null)
        {
            return Ok(ApiResponse<PagedResultDto<MyMailAddressBookItemDto>>.Ok(new PagedResultDto<MyMailAddressBookItemDto>
            {
                Items = [],
                Total = 0,
                Page = page,
                PageSize = pageSize
            }));
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            q = q.Where(x =>
                (x.ContactName != null && x.ContactName.Contains(kw))
                || (x.Email != null && x.Email.Contains(kw))
                || (x.PartyName != null && x.PartyName.Contains(kw)));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(x => x.PartyName)
            .ThenBy(x => x.ContactName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(ApiResponse<PagedResultDto<MyMailAddressBookItemDto>>.Ok(new PagedResultDto<MyMailAddressBookItemDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        }));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResultDto<MyMailListItemDto>>>> List(
        [FromQuery] string? mailboxId,
        [FromQuery] string? subject,
        [FromQuery] string? from,
        [FromQuery] string? body,
        [FromQuery(Name = "q")] string? keyword,
        [FromQuery] bool? isUnread,
        [FromQuery] bool? isStarred,
        [FromQuery] bool? hasRemark,
        [FromQuery] string? folder,
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

        var folderKey = (folder ?? "inbox").Trim().ToLowerInvariant();
        var deletedOnly = folderKey == "deleted";
        var sentOnly = folderKey == "sent";
        var q = from m in _db.UserMailMessages.AsNoTracking()
                join b in _db.UserMailboxes.AsNoTracking() on m.MailboxId equals b.Id
                where m.UserId == userId && m.IsDeleted == deletedOnly
                select new { m, b };
        if (!deletedOnly)
            q = sentOnly
                ? q.Where(x => x.m.Folder == SentFolder)
                : q.Where(x => x.m.Folder == InboxFolder);

        if (!string.IsNullOrWhiteSpace(mailboxId))
            q = q.Where(x => x.m.MailboxId == mailboxId.Trim());
        if (isUnread.HasValue)
            q = q.Where(x => x.m.IsUnread == isUnread.Value);
        if (isStarred == true)
            q = q.Where(x => x.m.IsStarred);
        if (hasRemark == true)
            q = q.Where(x => x.m.Remark != null && x.m.Remark != "");
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            q = q.Where(x =>
                (x.m.Subject != null && x.m.Subject.Contains(kw))
                || (x.m.FromAddress != null && x.m.FromAddress.Contains(kw))
                || (x.m.FromName != null && x.m.FromName.Contains(kw))
                || (x.m.BodyText != null && x.m.BodyText.Contains(kw))
                || (x.m.Snippet != null && x.m.Snippet.Contains(kw)));
        }
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
                IsStarred = x.m.IsStarred,
                Remark = x.m.Remark,
                HasAttachments = x.m.HasAttachments,
                IsDeleted = x.m.IsDeleted
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
            where m.Id == id && m.UserId == userId
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
                IsStarred = m.IsStarred,
                Remark = m.Remark,
                HasAttachments = m.HasAttachments,
                IsDeleted = m.IsDeleted,
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

    [HttpPost("send")]
    public async Task<ActionResult<ApiResponse<object>>> Send(
        [FromBody] MyMailSendRequest? body,
        CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(ApiResponse<object>.Fail("未登录", 401));
        body ??= new MyMailSendRequest();

        var to = SplitAddresses(body.To);
        if (to.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("请填写收件人", 400));
        if (string.IsNullOrWhiteSpace(body.Subject))
            return BadRequest(ApiResponse<object>.Fail("请填写主题", 400));
        if (string.IsNullOrWhiteSpace(body.Body))
            return BadRequest(ApiResponse<object>.Fail("请填写正文", 400));

        string? inReplyTo = null;
        if (!string.IsNullOrWhiteSpace(body.InReplyToMailId))
        {
            var origin = await _db.UserMailMessages.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == body.InReplyToMailId.Trim() && x.UserId == userId,
                    ct);
            inReplyTo = origin?.MessageId;
        }

        try
        {
            await _emailSender.SendAsync(
                userId,
                to,
                SplitAddresses(body.Cc),
                body.Subject.Trim(),
                body.Body,
                htmlBody: null,
                inReplyTo,
                ct);
            try
            {
                await SaveSentCopyAsync(userId, to, SplitAddresses(body.Cc), body.Subject.Trim(), body.Body, ct);
            }
            catch
            {
                // SMTP 已成功，本地已发送落库失败不阻断
            }
            return Ok(ApiResponse<object>.Ok(null, "已发送"));
        }
        catch (EmailSendException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (FormatException)
        {
            return BadRequest(ApiResponse<object>.Fail("收件人或抄送邮箱格式无效", 400));
        }
    }

    [HttpPost("{id}/remark")]
    public async Task<ActionResult<ApiResponse<object>>> SaveRemark(
        string id,
        [FromBody] MyMailRemarkRequest? body,
        CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(ApiResponse<object>.Fail("未登录", 401));

        var text = body?.Remark?.Trim() ?? "";
        if (text.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("备注不能为空", 400));
        if (text.Length > 2000)
            return BadRequest(ApiResponse<object>.Fail("备注不能超过 2000 字", 400));

        var entity = await _db.UserMailMessages
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (entity == null)
            return NotFound(ApiResponse<object>.Fail("邮件不存在", 404));

        entity.Remark = text;
        entity.ModifyTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(null, "备注已保存"));
    }

    [HttpPost("{id}/remark/clear")]
    public async Task<ActionResult<ApiResponse<object>>> ClearRemark(string id, CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(ApiResponse<object>.Fail("未登录", 401));

        var entity = await _db.UserMailMessages
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (entity == null)
            return NotFound(ApiResponse<object>.Fail("邮件不存在", 404));

        if (!string.IsNullOrWhiteSpace(entity.Remark))
        {
            entity.Remark = null;
            entity.ModifyTime = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(ApiResponse<object>.Ok(null, "备注已清除"));
    }

    [HttpPost("{id}/star")]
    public async Task<ActionResult<ApiResponse<object>>> SetStar(
        string id,
        [FromBody] MyMailStarRequest? body,
        CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(ApiResponse<object>.Fail("未登录", 401));

        var entity = await _db.UserMailMessages
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (entity == null)
            return NotFound(ApiResponse<object>.Fail("邮件不存在", 404));

        var starred = body?.Starred ?? false;
        if (entity.IsStarred != starred)
        {
            entity.IsStarred = starred;
            entity.ModifyTime = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(ApiResponse<object>.Ok(null, starred ? "已加星标" : "已取消星标"));
    }

    [HttpPost("{id}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkRead(string id, CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(ApiResponse<object>.Fail("未登录", 401));

        var entity = await _db.UserMailMessages
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
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

    [HttpPost("{id}/restore")]
    public async Task<ActionResult<ApiResponse<object>>> Restore(string id, CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(ApiResponse<object>.Fail("未登录", 401));

        var entity = await _db.UserMailMessages
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (entity == null)
            return NotFound(ApiResponse<object>.Fail("邮件不存在", 404));
        if (!entity.IsDeleted)
            return Ok(ApiResponse<object>.Ok(null, "已在收件箱"));

        var liveDup = await _db.UserMailMessages.AsNoTracking()
            .AnyAsync(
                x => x.Id != entity.Id
                     && x.MailboxId == entity.MailboxId
                     && x.Folder == entity.Folder
                     && x.ImapUid == entity.ImapUid
                     && !x.IsDeleted,
                ct);
        if (liveDup)
            return BadRequest(ApiResponse<object>.Fail("该邮件已在收件箱，无需恢复", 400));

        entity.IsDeleted = false;
        entity.ModifyTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(null, "已恢复到收件箱"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> SoftDelete(string id, CancellationToken ct)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(ApiResponse<object>.Fail("未登录", 401));

        var entity = await _db.UserMailMessages
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId && !x.IsDeleted, ct);
        if (entity == null)
            return NotFound(ApiResponse<object>.Fail("邮件不存在", 404));

        entity.IsDeleted = true;
        entity.ModifyTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(null, "已删除"));
    }

    private const string InboxFolder = "INBOX";
    private const string SentFolder = "SENT";

    private async Task SaveSentCopyAsync(
        string userId,
        IReadOnlyList<string> to,
        IReadOnlyList<string> cc,
        string subject,
        string body,
        CancellationToken ct)
    {
        var box = await _db.UserMailboxes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted && x.IsDefaultSend, ct);
        if (box == null) return;

        var text = body ?? "";
        var snippet = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (snippet.Length > 500) snippet = snippet[..500];
        var recipients = string.Join("; ", to.Concat(cc).Where(x => !string.IsNullOrWhiteSpace(x)));

        _db.UserMailMessages.Add(new UserMailMessage
        {
            UserId = userId,
            MailboxId = box.Id,
            ImapUid = Random.Shared.NextInt64(1, long.MaxValue),
            Folder = SentFolder,
            Subject = subject.Length > 1000 ? subject[..1000] : subject,
            FromAddress = box.Address,
            FromName = string.IsNullOrWhiteSpace(box.DisplayName) ? box.Address : box.DisplayName.Trim(),
            ToAddresses = recipients,
            ReceivedAt = DateTime.UtcNow,
            IsUnread = false,
            Snippet = snippet,
            BodyText = text,
            CreateTime = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    private static List<string> SplitAddresses(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];
        return raw
            .Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
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
    public bool IsDefaultSend { get; set; }
}

public sealed class MyMailAddressBookItemDto
{
    public string Id { get; set; } = string.Empty;
    public string PartyKind { get; set; } = string.Empty;
    public string PartyId { get; set; } = string.Empty;
    public string? PartyName { get; set; }
    public string? ContactName { get; set; }
    public string Email { get; set; } = string.Empty;
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
    public bool IsStarred { get; set; }
    public string? Remark { get; set; }
    public bool HasAttachments { get; set; }
    public bool IsDeleted { get; set; }
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

public sealed class MyMailStarRequest
{
    public bool Starred { get; set; }
}

public sealed class MyMailRemarkRequest
{
    public string? Remark { get; set; }
}

public sealed class MyMailSendRequest
{
    public string? To { get; set; }
    public string? Cc { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? InReplyToMailId { get; set; }
}

public sealed class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
