using CRM.API.Models.DTOs;
using CRM.API.Services.Interfaces;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace CRM.API.Services.Implementations;

public sealed class MailboxVerifyService : IMailboxVerifyService
{
    public const string PlatformVerifySubject = "CRM平台邮箱验证";

    private readonly ApplicationDbContext _db;
    private readonly IMailboxPasswordCipher _cipher;
    private readonly ILogger<MailboxVerifyService> _logger;

    public MailboxVerifyService(
        ApplicationDbContext db,
        IMailboxPasswordCipher cipher,
        ILogger<MailboxVerifyService> logger)
    {
        _db = db;
        _cipher = cipher;
        _logger = logger;
    }

    public async Task<MailboxVerifyResultDto> VerifyAsync(string mailboxId, CancellationToken cancellationToken = default)
    {
        var box = await _db.UserMailboxes.FirstOrDefaultAsync(
            x => x.Id == mailboxId && !x.IsDeleted, cancellationToken);
        if (box == null)
            return EarlyFail("邮箱不存在");

        if (string.IsNullOrWhiteSpace(box.PasswordCipher))
            return EarlyFail("请先保存邮箱密码");

        string password;
        try
        {
            password = _cipher.Decrypt(box.PasswordCipher, box.CryptoVersion);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解密邮箱密码失败 mailboxId={Id}", mailboxId);
            return EarlyFail("密码解密失败，请重新保存密码");
        }

        if (string.IsNullOrEmpty(password))
            return EarlyFail("请先保存邮箱密码");

        var tenant = await CompanyProfileBundleLoader.LoadSmtpEmailRawAsync(_db, cancellationToken)
                     ?? new CompanySmtpEmailSettingsDto();

        var (imapHost, imapPort, imapSsl, resolveError) = ResolveImapEndpoint(box, tenant);
        if (resolveError != null)
            return EarlyFail(resolveError);

        var address = box.Address.Trim();
        var result = new MailboxVerifyResultDto();

        // 1) 先验 IMAP
        try
        {
            using var imap = new ImapClient();
            var secure = imapSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.Auto;
            await imap.ConnectAsync(imapHost, imapPort, secure, cancellationToken);
            await imap.AuthenticateAsync(address, password, cancellationToken);
            var inbox = imap.Inbox;
            await inbox.OpenAsync(MailKit.FolderAccess.ReadOnly, cancellationToken);
            _ = inbox.Count;
            await imap.DisconnectAsync(true, cancellationToken);
            result.ImapOk = true;
            result.ImapMessage = "IMAP 收信验证成功";
            result.PopOk = true;
            result.PopMessage = result.ImapMessage;
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "IMAP 验证失败 mailboxId={Id}", mailboxId);
            result.ImapOk = false;
            result.ImapMessage = ShortMsg("IMAP 收信验证失败", ex);
            result.PopOk = false;
            result.PopMessage = result.ImapMessage;
            result.Success = false;
            result.Message = result.ImapMessage;
            return result;
        }

        // 2) 平台再验 SMTP；其他邮箱仅 IMAP
        if (box.Kind != UserMailboxKind.Platform)
        {
            result.Success = true;
            result.Message = result.ImapMessage;
            return result;
        }

        if (string.IsNullOrWhiteSpace(tenant.SmtpHost))
        {
            result.SmtpOk = false;
            result.SmtpMessage = "SMTP 发信验证失败：管理员尚未配置租户 SMTP 服务器";
            result.Success = false;
            result.Message = $"{result.ImapMessage}；{result.SmtpMessage}";
            return result;
        }

        try
        {
            var message = new MimeMessage();
            var fromName = string.IsNullOrWhiteSpace(box.DisplayName) ? string.Empty : box.DisplayName.Trim();
            message.From.Add(string.IsNullOrEmpty(fromName)
                ? new MailboxAddress(string.Empty, address)
                : new MailboxAddress(fromName, address));
            message.To.Add(MailboxAddress.Parse(address));
            message.Subject = PlatformVerifySubject;
            message.Body = new TextPart("plain") { Text = PlatformVerifySubject };

            using var smtp = new SmtpClient();
            var port = tenant.SmtpPort is >= 1 and <= 65535 ? tenant.SmtpPort : 587;
            var secure = SmtpSecureOptions.Resolve(port, tenant.UseSsl);
            await smtp.ConnectAsync(tenant.SmtpHost.Trim(), port, secure, cancellationToken);
            await smtp.AuthenticateAsync(address, password, cancellationToken);
            await smtp.SendAsync(message, cancellationToken);
            await smtp.DisconnectAsync(true, cancellationToken);
            result.SmtpOk = true;
            result.SmtpMessage = "SMTP 发信验证成功";
            result.Success = true;
            result.Message = $"{result.ImapMessage}；{result.SmtpMessage}";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "SMTP 测试信失败 mailboxId={Id}", mailboxId);
            result.SmtpOk = false;
            result.SmtpMessage = ShortMsg("SMTP 发信验证失败", ex);
            result.Success = false;
            result.Message = $"{result.ImapMessage}；{result.SmtpMessage}";
            return result;
        }
    }

    /// <summary>解析 IMAP 端点：平台用公司设置；个人用行上配置。Imap 空时回退历史 Pop 字段。</summary>
    public static (string Host, int Port, bool Ssl, string? Error) ResolveImapEndpoint(
        UserMailbox box,
        CompanySmtpEmailSettingsDto tenant)
    {
        if (box.Kind == UserMailboxKind.Platform)
        {
            var host = FirstNonEmpty(tenant.ImapHost, tenant.PopHost);
            if (string.IsNullOrWhiteSpace(host))
                return ("", 0, true, "管理员尚未配置租户 IMAP 服务器");
            var port = tenant.ImapPort is >= 1 and <= 65535
                ? tenant.ImapPort
                : (tenant.PopPort is >= 1 and <= 65535 ? tenant.PopPort : 993);
            var ssl = !string.IsNullOrWhiteSpace(tenant.ImapHost) ? tenant.ImapUseSsl : tenant.PopUseSsl;
            return (host.Trim(), port, ssl, null);
        }

        var personalHost = FirstNonEmpty(box.ImapHost, box.PopHost);
        if (string.IsNullOrWhiteSpace(personalHost))
            return ("", 0, true, "请填写 IMAP 服务器地址");
        var personalPort = box.ImapPort is >= 1 and <= 65535
            ? box.ImapPort.Value
            : (box.PopPort is >= 1 and <= 65535 ? box.PopPort.Value : 993);
        var personalSsl = !string.IsNullOrWhiteSpace(box.ImapHost) ? box.ImapUseSsl : box.PopUseSsl;
        return (personalHost.Trim(), personalPort, personalSsl, null);
    }

    private static string? FirstNonEmpty(string? a, string? b)
    {
        if (!string.IsNullOrWhiteSpace(a)) return a;
        if (!string.IsNullOrWhiteSpace(b)) return b;
        return null;
    }

    private static MailboxVerifyResultDto EarlyFail(string message) => new()
    {
        Success = false,
        Message = message,
        ImapOk = false,
        ImapMessage = message,
        PopOk = false,
        PopMessage = message
    };

    private static string ShortMsg(string prefix, Exception ex)
    {
        var detail = ex.Message?.Trim() ?? "未知错误";
        if (detail.Length > 200) detail = detail[..200] + "…";
        return $"{prefix}：{detail}";
    }
}
