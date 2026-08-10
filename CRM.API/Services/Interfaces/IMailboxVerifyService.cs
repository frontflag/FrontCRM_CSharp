namespace CRM.API.Services.Interfaces;

public interface IMailboxVerifyService
{
    Task<MailboxVerifyResultDto> VerifyAsync(string mailboxId, CancellationToken cancellationToken = default);
}

public sealed class MailboxVerifyResultDto
{
    /// <summary>整体是否通过（平台需 POP+SMTP 皆成功；其他仅 POP）。</summary>
    public bool Success { get; set; }

    /// <summary>汇总文案（写入 verify_message）。</summary>
    public string Message { get; set; } = string.Empty;

    public bool PopOk { get; set; }
    public string PopMessage { get; set; } = string.Empty;

    /// <summary>非平台邮箱为 null（未做 SMTP 验证）。</summary>
    public bool? SmtpOk { get; set; }
    public string? SmtpMessage { get; set; }
}
