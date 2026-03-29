using CRM.API.Services;
using CRM.API.Services.Interfaces;
using CRM.Infrastructure.Data;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CRM.API.Services.Implementations;

/// <summary>
/// 仅从「系统 → 公司信息 → 公司邮箱」读取 SMTP，不使用 appsettings。
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<SmtpEmailSender> _logger;

    private const string ConfigHint = "请在「系统 → 公司信息 → 公司邮箱」中配置并保存。";

    public SmtpEmailSender(ApplicationDbContext db, ILogger<SmtpEmailSender> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SendWithAttachmentAsync(
        string to,
        string subject,
        string? textBody,
        byte[] attachmentBytes,
        string attachmentFileName,
        string attachmentMimeType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(to))
            throw new ArgumentException("收件人邮箱不能为空", nameof(to));

        var cfg = await CompanyProfileBundleLoader.LoadSmtpEmailRawAsync(_db, cancellationToken);
        if (cfg == null)
            throw new InvalidOperationException($"未读取到公司发信参数。{ConfigHint}");

        if (!cfg.Enabled)
            throw new InvalidOperationException($"系统发信未启用。{ConfigHint}需开启「启用系统发信」。");

        if (string.IsNullOrWhiteSpace(cfg.SmtpHost))
            throw new InvalidOperationException($"未配置 SMTP 服务器。{ConfigHint}");

        if (string.IsNullOrWhiteSpace(cfg.FromAddress))
            throw new InvalidOperationException($"未配置发件人邮箱。{ConfigHint}");

        if (cfg.SmtpPort is < 1 or > 65535)
            throw new InvalidOperationException($"SMTP 端口无效（1～65535）。{ConfigHint}");

        if (!string.IsNullOrWhiteSpace(cfg.User) && string.IsNullOrWhiteSpace(cfg.Password))
            throw new InvalidOperationException("已填写 SMTP 账号，请同时填写密码或授权码（或清空账号使用无认证服务器）。");

        var host = cfg.SmtpHost.Trim();
        var port = cfg.SmtpPort;
        var fromAddress = cfg.FromAddress.Trim();
        var fromName = string.IsNullOrWhiteSpace(cfg.FromName) ? "FrontCRM" : cfg.FromName.Trim();
        var useSsl = cfg.UseSsl;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(to.Trim()));
        message.Subject = subject;

        var builder = new BodyBuilder
        {
            TextBody = string.IsNullOrWhiteSpace(textBody) ? "请查收附件中的采购订单。" : textBody
        };
        builder.Attachments.Add(attachmentFileName, attachmentBytes, ContentType.Parse(attachmentMimeType));
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        var secure = useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
        await client.ConnectAsync(host, port, secure, cancellationToken);

        if (!string.IsNullOrWhiteSpace(cfg.User))
            await client.AuthenticateAsync(cfg.User.Trim(), cfg.Password ?? string.Empty, cancellationToken);

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
        _logger.LogInformation("已发送邮件至 {To}，主题 {Subject}", to, subject);
    }
}
