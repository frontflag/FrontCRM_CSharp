using System.Text.RegularExpressions;

namespace CRM.Core.Utilities;

/// <summary>
/// 客户/供应商企业邮箱：只存后缀（如 @huawei.com），小写、前置 @。空不参与查重。
/// </summary>
public static class CompanyEmailSuffix
{
    public const int MaxLength = 128;

    public const string InvalidFormatMessage = "企业邮箱须为邮箱后缀，如 @xxx.com";

    /// <summary>公共邮箱后缀（下拉不展示；手输仍允许保存）。</summary>
    public static readonly HashSet<string> PublicSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "@qq.com",
        "@gmail.com",
        "@163.com",
        "@126.com",
        "@sina.com",
        "@sina.cn",
        "@hotmail.com",
        "@outlook.com",
        "@live.com",
        "@msn.com",
        "@yahoo.com",
        "@yahoo.com.cn",
        "@icloud.com",
        "@me.com",
        "@foxmail.com",
        "@yeah.net",
        "@139.com",
        "@189.cn",
        "@sohu.com",
        "@aliyun.com",
        "@tom.com",
        "@aol.com",
        "@proton.me",
        "@protonmail.com",
        "@mail.com",
        "@gmx.com",
        "@yandex.com"
    };

    private static readonly Regex DomainRegex = new(
        @"^(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z]{2,63}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool IsPublic(string? suffix)
    {
        var n = NormalizeOrNull(suffix);
        return n != null && PublicSuffixes.Contains(n);
    }

    /// <summary>空或仅空白 → null；完整邮箱取 @ 后域名；无 @ 则整段当域名。</summary>
    public static string? NormalizeOrNull(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var raw = input.Trim().ToLowerInvariant().Replace(" ", "").Replace("\u3000", "");
        if (raw.Length == 0)
            return null;

        string domain;
        var at = raw.LastIndexOf('@');
        if (at >= 0)
            domain = raw[(at + 1)..];
        else
            domain = raw.TrimStart('@');

        domain = domain.Trim().Trim('.');
        if (domain.Length == 0)
            return null;

        return "@" + domain;
    }

    public static bool TryNormalize(string? input, out string? suffix, out string? error)
    {
        suffix = null;
        error = null;
        if (string.IsNullOrWhiteSpace(input))
            return true;

        var n = NormalizeOrNull(input);
        if (n == null || n.Length > MaxLength || !DomainRegex.IsMatch(n[1..]))
        {
            error = InvalidFormatMessage;
            return false;
        }

        suffix = n;
        return true;
    }

    public static string DuplicateMessage(string partyKind, string suffix, string? partyName)
    {
        var name = string.IsNullOrWhiteSpace(partyName) ? "" : partyName.Trim();
        return string.IsNullOrEmpty(name)
            ? $"企业邮箱 {suffix} 已被其他{partyKind}使用"
            : $"企业邮箱 {suffix} 已被{partyKind}「{name}」占用";
    }
}
