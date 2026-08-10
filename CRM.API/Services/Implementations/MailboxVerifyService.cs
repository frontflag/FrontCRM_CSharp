using CRM.API.Models.DTOs;
using CRM.API.Services.Interfaces;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using MailKit.Net.Pop3;
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

        string popHost;
        int popPort;
        bool popSsl;
        if (box.Kind == UserMailboxKind.Platform)
        {
            if (string.IsNullOrWhiteSpace(tenant.PopHost))
                return EarlyFail("管理员尚未配置租户 POP 服务器");
            popHost = tenant.PopHost.Trim();
            popPort = tenant.PopPort is >= 1 and <= 65535 ? tenant.PopPort : 995;
            popSsl = tenant.PopUseSsl;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(box.PopHost))
                return EarlyFail("请填写 POP 服务器地址");
            popHost = box.PopHost.Trim();
            popPort = box.PopPort is >= 1 and <= 65535 ? box.PopPort.Value : 995;
            popSsl = box.PopUseSsl;
        }

        var address = box.Address.Trim();
        var result = new MailboxVerifyResultDto();

        // 1) 先验 POP
        try
        {
            using var pop = new Pop3Client();
            var secure = popSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.Auto;
            await pop.ConnectAsync(popHost, popPort, secure, cancellationToken);
            await pop.AuthenticateAsync(address, password, cancellationToken);
            _ = pop.Count;
            await pop.DisconnectAsync(true, cancellationToken);
            result.PopOk = true;
            result.PopMessage = "POP 收信验证成功";
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "POP 验证失败 mailboxId={Id}", mailboxId);
            result.PopOk = false;
            result.PopMessage = ShortMsg("POP 收信验证失败", ex);
            result.Success = false;
            result.Message = result.PopMessage;
            return result;
        }

        // 2) 平台再验 SMTP；其他邮箱仅 POP
        if (box.Kind != UserMailboxKind.Platform)
        {
            result.Success = true;
            result.Message = result.PopMessage;
            return result;
        }

        if (string.IsNullOrWhiteSpace(tenant.SmtpHost))
        {
            result.SmtpOk = false;
            result.SmtpMessage = "SMTP 发信验证失败：管理员尚未配置租户 SMTP 服务器";
            result.Success = false;
            result.Message = $"{result.PopMessage}；{result.SmtpMessage}";
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
            var secure = ResolveSmtpSecure(port, tenant.UseSsl);
            await smtp.ConnectAsync(tenant.SmtpHost.Trim(), port, secure, cancellationToken);
            await smtp.AuthenticateAsync(address, password, cancellationToken);
            await smtp.SendAsync(message, cancellationToken);
            await smtp.DisconnectAsync(true, cancellationToken);
            result.SmtpOk = true;
            result.SmtpMessage = "SMTP 发信验证成功";
            result.Success = true;
            result.Message = $"{result.PopMessage}；{result.SmtpMessage}";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "SMTP 测试信失败 mailboxId={Id}", mailboxId);
            result.SmtpOk = false;
            result.SmtpMessage = ShortMsg("SMTP 发信验证失败", ex);
            result.Success = false;
            result.Message = $"{result.PopMessage}；{result.SmtpMessage}";
            return result;
        }
    }

    private static SecureSocketOptions ResolveSmtpSecure(int port, bool useSsl)
    {
        if (!useSsl) return SecureSocketOptions.Auto;
        // 465 多为隐式 SSL；587 多为 STARTTLS
        if (port == 465) return SecureSocketOptions.SslOnConnect;
        return SecureSocketOptions.StartTls;
    }

    private static MailboxVerifyResultDto EarlyFail(string message) => new()
    {
        Success = false,
        Message = message,
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
