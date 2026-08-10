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

        try
        {
            using var client = new SmtpClient();
            var secure = useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
            await client.ConnectAsync(host, port, secure, cancellationToken);
            await client.AuthenticateAsync(address, password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            _logger.LogInformation("已发送邮件至 {To}，发件人 {From}，主题 {Subject}", to, address, subject);
        }
        catch (EmailSendException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMTP 发信失败 from={From} to={To}", address, to);
            var detail = string.IsNullOrWhiteSpace(ex.Message) ? "未知错误" : ex.Message.Trim();
            if (detail.Length > 200) detail = detail[..200];
            throw new EmailSendException(MailboxSendErrorCodes.SmtpRejected, $"邮件服务器拒绝发送：{detail}");
        }
    }
}
