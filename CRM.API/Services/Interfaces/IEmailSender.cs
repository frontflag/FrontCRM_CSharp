namespace CRM.API.Services.Interfaces;

public interface IEmailSender
{
    /// <summary>发送带单个附件的邮件。</summary>
    Task SendWithAttachmentAsync(
        string to,
        string subject,
        string? textBody,
        byte[] attachmentBytes,
        string attachmentFileName,
        string attachmentMimeType,
        CancellationToken cancellationToken = default);
}
