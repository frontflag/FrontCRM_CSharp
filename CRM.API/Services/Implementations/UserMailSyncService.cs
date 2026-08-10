using System.Globalization;
using System.Text;
using CRM.API.Models.DTOs;
using CRM.API.Services.Interfaces;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace CRM.API.Services.Implementations;

public sealed class UserMailSyncService : IUserMailSyncService
{
    public static readonly TimeZoneInfo ShanghaiTz = ResolveShanghai();

    private const string InboxFolder = "INBOX";
    private const int MaxMessagesPerMailbox = 200;
    private const int SnippetMaxLen = 500;

    private readonly ApplicationDbContext _db;
    private readonly IMailboxPasswordCipher _cipher;
    private readonly ILogger<UserMailSyncService> _logger;

    public UserMailSyncService(
        ApplicationDbContext db,
        IMailboxPasswordCipher cipher,
        ILogger<UserMailSyncService> logger)
    {
        _db = db;
        _cipher = cipher;
        _logger = logger;
    }

    public async Task<UserMailSyncResultDto> SyncUserAsync(
        string userId,
        string? mailboxId = null,
        CancellationToken cancellationToken = default)
    {
        var result = new UserMailSyncResultDto();
        if (string.IsNullOrWhiteSpace(userId))
        {
            result.Errors.Add("用户无效");
            return result;
        }

        var tenant = await CompanyProfileBundleLoader.LoadSmtpEmailRawAsync(_db, cancellationToken)
                     ?? new CompanySmtpEmailSettingsDto();
        if (tenant.MailSyncEarliestDate == null)
        {
            result.Errors.Add("管理员尚未配置系统最早同步邮件日期");
            return result;
        }

        var earliestLocal = DateTime.SpecifyKind(tenant.MailSyncEarliestDate.Value.Date, DateTimeKind.Unspecified);
        var earliestUtc = TimeZoneInfo.ConvertTimeToUtc(earliestLocal, ShanghaiTz);

        var q = _db.UserMailboxes.Where(x =>
            x.UserId == userId
            && !x.IsDeleted
            && x.VerifyStatus == UserMailboxVerifyStatus.Ok);
        if (!string.IsNullOrWhiteSpace(mailboxId))
            q = q.Where(x => x.Id == mailboxId.Trim());

        var boxes = await q.ToListAsync(cancellationToken);
        result.MailboxCount = boxes.Count;
        if (boxes.Count == 0)
        {
            result.Errors.Add(string.IsNullOrWhiteSpace(mailboxId)
                ? "没有已验证的邮箱"
                : "指定邮箱不存在、未验证或不属于当前用户");
            return result;
        }

        foreach (var box in boxes)
        {
            try
            {
                var (fetched, upserted) = await SyncOneMailboxAsync(box, tenant, earliestUtc, cancellationToken);
                result.FetchedCount += fetched;
                result.UpsertedCount += upserted;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "同步邮箱失败 mailboxId={Id} userId={UserId}", box.Id, userId);
                var msg = Truncate($"邮箱 {box.Address}：{ex.Message}", 500);
                result.Errors.Add(msg);
                await WriteSyncStateAsync(box, success: false, error: msg, uidValidity: null, cancellationToken);
            }
        }

        return result;
    }

    public async Task<UserMailDailySyncResultDto> SyncAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var userIds = await _db.UserMailboxes.AsNoTracking()
            .Where(x => !x.IsDeleted && x.VerifyStatus == UserMailboxVerifyStatus.Ok)
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var summary = new UserMailDailySyncResultDto { UserCount = userIds.Count };
        foreach (var uid in userIds)
        {
            try
            {
                var r = await SyncUserAsync(uid, null, cancellationToken);
                if (r.Errors.Count == 0) summary.OkCount++;
                else summary.FailCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "每日同步用户失败 userId={UserId}", uid);
                summary.FailCount++;
            }
        }

        return summary;
    }

    private async Task<(int Fetched, int Upserted)> SyncOneMailboxAsync(
        UserMailbox box,
        CompanySmtpEmailSettingsDto tenant,
        DateTime earliestUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(box.PasswordCipher))
            throw new InvalidOperationException("邮箱未保存密码");

        var password = _cipher.Decrypt(box.PasswordCipher, box.CryptoVersion);
        var (host, port, ssl, err) = MailboxVerifyService.ResolveImapEndpoint(box, tenant);
        if (err != null)
            throw new InvalidOperationException(err);

        using var client = new ImapClient();
        var secure = ssl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.Auto;
        await client.ConnectAsync(host, port, secure, cancellationToken);
        await client.AuthenticateAsync(box.Address.Trim(), password, cancellationToken);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
        uint? uidValidity = inbox.UidValidity;

        // DeliveredAfter 为「该日之后」；减 1 天使 earliest 当日纳入
        var query = SearchQuery.NotSeen.And(SearchQuery.DeliveredAfter(earliestUtc.AddDays(-1)));
        var allUids = await inbox.SearchAsync(query, cancellationToken);
        var uidList = allUids
            .OrderByDescending(u => u.Id)
            .Take(MaxMessagesPerMailbox)
            .ToList();

        var fetched = uidList.Count;
        var upserted = 0;

        foreach (var uid in uidList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var message = await inbox.GetMessageAsync(uid, cancellationToken);
            var received = message.Date.UtcDateTime;
            if (received < earliestUtc)
                continue;

            IMessageSummary? summary = null;
            try
            {
                var summaries = await inbox.FetchAsync(new[] { uid }, MessageSummaryItems.Flags, cancellationToken);
                summary = summaries.FirstOrDefault();
            }
            catch
            {
                /* 部分服务器不支持按 UID Fetch Flags；默认按未读落库 */
            }
            var isUnread = summary?.Flags == null || !summary.Flags.Value.HasFlag(MessageFlags.Seen);

            upserted += await UpsertMessageAsync(box, uid.Id, message, isUnread, cancellationToken);
        }

        await client.DisconnectAsync(true, cancellationToken);
        await WriteSyncStateAsync(box, success: true, error: null, uidValidity, cancellationToken);
        return (fetched, upserted);
    }

    private async Task<int> UpsertMessageAsync(
        UserMailbox box,
        uint imapUid,
        MimeMessage message,
        bool isUnread,
        CancellationToken cancellationToken)
    {
        var existing = await _db.UserMailMessages.FirstOrDefaultAsync(
            x => x.MailboxId == box.Id
                 && x.Folder == InboxFolder
                 && x.ImapUid == imapUid
                 && !x.IsDeleted,
            cancellationToken);

        var text = message.TextBody ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(message.HtmlBody))
            text = StripHtmlRough(message.HtmlBody);

        var from = message.From.Mailboxes.FirstOrDefault();
        var to = string.Join("; ", message.To.Mailboxes.Select(m => m.Address));

        var isNew = existing == null;
        if (isNew)
        {
            existing = new UserMailMessage
            {
                Id = Guid.NewGuid().ToString(),
                UserId = box.UserId,
                MailboxId = box.Id,
                ImapUid = imapUid,
                Folder = InboxFolder,
                CreateTime = DateTime.UtcNow,
                IsUnread = isUnread
            };
            await _db.UserMailMessages.AddAsync(existing, cancellationToken);
        }

        existing!.MessageId = Truncate(message.MessageId, 998);
        existing.Subject = Truncate(message.Subject, 1000);
        existing.FromAddress = Truncate(from?.Address, 512);
        existing.FromName = Truncate(from?.Name, 256);
        existing.ToAddresses = to;
        existing.ReceivedAt = message.Date.UtcDateTime;
        // 本地已读优先：用户查看后标已读，不再被服务器未读覆盖
        if (isNew)
            existing.IsUnread = isUnread;
        else if (existing.IsUnread)
            existing.IsUnread = isUnread;

        existing.Snippet = Truncate(text.Replace('\r', ' ').Replace('\n', ' ').Trim(), SnippetMaxLen);
        existing.BodyText = text;
        existing.BodyHtml = message.HtmlBody;
        existing.HasAttachments = message.Attachments.Any();
        existing.SizeBytes = EstimateSize(message);
        existing.ModifyTime = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return 1;
    }

    private async Task WriteSyncStateAsync(
        UserMailbox box,
        bool success,
        string? error,
        uint? uidValidity,
        CancellationToken cancellationToken)
    {
        var state = await _db.UserMailboxSyncStates.FirstOrDefaultAsync(x => x.MailboxId == box.Id, cancellationToken);
        if (state == null)
        {
            state = new UserMailboxSyncState
            {
                MailboxId = box.Id,
                UserId = box.UserId
            };
            await _db.UserMailboxSyncStates.AddAsync(state, cancellationToken);
        }

        state.LastSyncAt = DateTime.UtcNow;
        if (success)
        {
            state.LastSuccessAt = state.LastSyncAt;
            state.LastError = null;
            if (uidValidity.HasValue)
                state.LastUidValidity = uidValidity;
        }
        else
        {
            state.LastError = Truncate(error, 2000);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static int EstimateSize(MimeMessage message)
    {
        try
        {
            using var ms = new MemoryStream();
            message.WriteTo(ms);
            return (int)Math.Min(int.MaxValue, ms.Length);
        }
        catch
        {
            return 0;
        }
    }

    private static string StripHtmlRough(string html)
    {
        var sb = new StringBuilder(html.Length);
        var inTag = false;
        foreach (var ch in html)
        {
            if (ch == '<') { inTag = true; continue; }
            if (ch == '>') { inTag = false; continue; }
            if (!inTag) sb.Append(ch);
        }
        return sb.ToString();
    }

    private static string? Truncate(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var t = s.Trim();
        return t.Length <= max ? t : t[..max];
    }

    private static TimeZoneInfo ResolveShanghai()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai"); }
        catch
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("China Standard Time"); }
            catch { return TimeZoneInfo.Utc; }
        }
    }

    public static string ShanghaiDateKey(DateTime utcNow) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), ShanghaiTz)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static bool IsPastShanghaiDailySlot(DateTime utcNow, int hour = 8, int minute = 30)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), ShanghaiTz);
        return local.Hour > hour || (local.Hour == hour && local.Minute >= minute);
    }
}
