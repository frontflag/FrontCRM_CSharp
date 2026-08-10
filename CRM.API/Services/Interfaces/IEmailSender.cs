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
}
