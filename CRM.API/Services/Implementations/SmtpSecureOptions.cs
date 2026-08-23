using MailKit.Security;

namespace CRM.API.Services.Implementations;

/// <summary>
/// 公司「使用 SSL」+ 端口共同决定 SMTP 握手方式。
/// 465 为隐式 SSL；587/25 等为连接后 STARTTLS。验证发信与业务发信必须共用，否则阿里企业邮等会直接断开。
/// </summary>
internal static class SmtpSecureOptions
{
    public static SecureSocketOptions Resolve(int port, bool useSsl)
    {
        if (!useSsl) return SecureSocketOptions.Auto;
        return port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
    }
}
