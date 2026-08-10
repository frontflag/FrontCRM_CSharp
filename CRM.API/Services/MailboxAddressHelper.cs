namespace CRM.API.Services;

public static class MailboxAddressHelper
{
    /// <summary>规范化后缀为以 @ 开头的小写域名，如 @xxx.com。</summary>
    public static string? NormalizeSuffix(string? raw)
    {
        var s = raw?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(s)) return null;
        if (!s.StartsWith('@')) s = "@" + s;
        if (s.Length < 3 || !s.Contains('.')) return null;
        return s;
    }

    public static string BuildPlatformAddress(string localPart, string suffix)
    {
        var local = (localPart ?? string.Empty).Trim().TrimStart('@');
        var suf = NormalizeSuffix(suffix) ?? "@";
        return local + suf;
    }

    public static string ExtractLocalPart(string? emailOrLocal)
    {
        var s = (emailOrLocal ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(s)) return "xxx";
        var at = s.IndexOf('@');
        if (at <= 0) return s;
        var local = s[..at].Trim();
        return string.IsNullOrEmpty(local) ? "xxx" : local;
    }

    public static bool IsValidEmail(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return false;
        try
        {
            var addr = new System.Net.Mail.MailAddress(address.Trim());
            return !string.IsNullOrEmpty(addr.Address);
        }
        catch
        {
            return false;
        }
    }
}
