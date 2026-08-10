namespace CRM.API.Services;

/// <summary>业务发信失败；<see cref="Code"/> 供前端映射提示。</summary>
public sealed class EmailSendException : InvalidOperationException
{
    public string Code { get; }

    public EmailSendException(string code, string message) : base(message)
    {
        Code = code;
    }
}

public static class MailboxSendErrorCodes
{
    public const string SmtpDisabled = "SmtpDisabled";
    public const string SmtpHostMissing = "SmtpHostMissing";
    public const string NoDefaultMailbox = "NoDefaultMailbox";
    public const string DefaultNotVerified = "DefaultNotVerified";
    public const string SmtpRejected = "SmtpRejected";

    public static string DefaultUserHint => "请先在个人设置 → 我的邮箱中完成平台邮箱验证。";
}
