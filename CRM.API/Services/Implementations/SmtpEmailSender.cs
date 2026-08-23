using CRM.API.Services;
using CRM.API.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CRM.API.Services.Implementations;

/// <summary>
/// 业务发信：公司 SMTP/POP 服务器参数 + 员工默认平台邮箱凭据。
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IMailboxSendService _send;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IMailboxSendService send, ILogger<SmtpEmailSender> logger)
    {
        _send = send;
        _logger = logger;
    }

    public async Task SendWithAttachmentAsync(
        string senderUserId,
        string to,
        string subject,
        string? textBody,
        byte[] attachmentBytes,
        string attachmentFileName,
        string attachmentMimeType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(senderUserId))
            throw new EmailSendException(MailboxSendErrorCodes.NoDefaultMailbox, MailboxSendErrorCodes.DefaultUserHint);
        if (string.IsNullOrWhiteSpace(to))
            throw new ArgumentException("收件人邮箱不能为空", nameof(to));

        var (cfg, box, password) = await _send.ResolveSenderAsync(senderUserId, cancellationToken);

        var host = cfg.SmtpHost.Trim();
        var port = cfg.SmtpPort;
        var address = box.Address.Trim();
        var fromName = string.IsNullOrWhiteSpace(box.DisplayName) ? null : box.DisplayName.Trim();
        var useSsl = cfg.UseSsl;

        var message = new MimeMessage();
        message.From.Add(string.IsNullOrEmpty(fromName)
            ? new MailboxAddress(string.Empty, address)
            : new MailboxAddress(fromName, address));
        message.To.Add(MailboxAddress.Parse(to.Trim()));
        message.Subject = subject;

        var builder = new BodyBuilder
        {
            TextBody = string.IsNullOrWhiteSpace(textBody) ? "请查收附件中的采购订单。" : textBody
        };
        builder.Attachments.Add(attachmentFileName, attachmentBytes, ContentType.Parse(attachmentMimeType));
        message.Body = builder.ToMessageBody();

        await DeliverAsync(host, port, useSsl, address, password, message, to, cancellationToken);
    }

    public async Task SendAsync(
        string senderUserId,
        IReadOnlyList<string> to,
        IReadOnlyList<string>? cc,
        string subject,
        string? textBody,
        string? htmlBody,
        string? inReplyToMessageId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(senderUserId))
            throw new EmailSendException(MailboxSendErrorCodes.NoDefaultMailbox, MailboxSendErrorCodes.DefaultUserHint);
        if (to == null || to.Count == 0 || to.All(string.IsNullOrWhiteSpace))
            throw new ArgumentException("收件人邮箱不能为空", nameof(to));

        var (cfg, box, password) = await _send.ResolveSenderAsync(senderUserId, cancellationToken);
        var host = cfg.SmtpHost.Trim();
        var port = cfg.SmtpPort;
        var address = box.Address.Trim();
        var fromName = string.IsNullOrWhiteSpace(box.DisplayName) ? null : box.DisplayName.Trim();
        var useSsl = cfg.UseSsl;

        var message = new MimeMessage();
        message.From.Add(string.IsNullOrEmpty(fromName)
            ? new MailboxAddress(string.Empty, address)
            : new MailboxAddress(fromName, address));
        foreach (var addr in to.Where(x => !string.IsNullOrWhiteSpace(x)))
            message.To.Add(MailboxAddress.Parse(addr.Trim()));
        if (cc != null)
        {
            foreach (var addr in cc.Where(x => !string.IsNullOrWhiteSpace(x)))
                message.Cc.Add(MailboxAddress.Parse(addr.Trim()));
        }
        message.Subject = subject ?? "";
        if (!string.IsNullOrWhiteSpace(inReplyToMessageId))
            message.InReplyTo = inReplyToMessageId.Trim();

        var builder = new BodyBuilder();
        if (!string.IsNullOrWhiteSpace(htmlBody))
            builder.HtmlBody = htmlBody;
        builder.TextBody = string.IsNullOrWhiteSpace(textBody) ? " " : textBody;
        message.Body = builder.ToMessageBody();

        var toLog = string.Join(",", to);
        await DeliverAsync(host, port, useSsl, address, password, message, toLog, cancellationToken);
    }

    private async Task DeliverAsync(
        string host,
        int port,
        bool useSsl,
        string address,
        string password,
        MimeMessage message,
        string toLog,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = new SmtpClient();
            var secure = SmtpSecureOptions.Resolve(port, useSsl);
            await client.ConnectAsync(host, port, secure, cancellationToken);
            await client.AuthenticateAsync(address, password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            _logger.LogInformation("已发送邮件至 {To}，发件人 {From}，主题 {Subject}", toLog, address, message.Subject);
        }
        catch (EmailSendException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMTP 发信失败 from={From} to={To}", address, toLog);
            var detail = string.IsNullOrWhiteSpace(ex.Message) ? "未知错误" : ex.Message.Trim();
            if (detail.Length > 200) detail = detail[..200];
            throw new EmailSendException(MailboxSendErrorCodes.SmtpRejected, $"邮件服务器拒绝发送：{detail}");
        }
    }
}
