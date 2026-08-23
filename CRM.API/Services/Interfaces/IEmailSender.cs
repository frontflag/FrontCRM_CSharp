namespace CRM.API.Services.Interfaces;

public interface IEmailSender
{
    /// <summary>使用指定用户的默认平台邮箱发送带附件邮件。</summary>
    Task SendWithAttachmentAsync(
        string senderUserId,
        string to,
        string subject,
        string? textBody,
        byte[] attachmentBytes,
        string attachmentFileName,
        string attachmentMimeType,
        CancellationToken cancellationToken = default);

    /// <summary>使用默认平台邮箱发送纯文本/HTML 邮件（我的邮件回复/写新信）。</summary>
    Task SendAsync(
        string senderUserId,
        IReadOnlyList<string> to,
        IReadOnlyList<string>? cc,
        string subject,
        string? textBody,
        string? htmlBody,
        string? inReplyToMessageId,
        CancellationToken cancellationToken = default);
}
