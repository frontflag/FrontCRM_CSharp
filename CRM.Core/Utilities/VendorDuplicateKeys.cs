using System.Text;

namespace CRM.Core.Utilities;

/// <summary>
/// 供应商查重键：去首尾空格、去掉中间空白（含全角）；英文/信用代码不区分大小写；邓白氏再去掉连字符。
/// 空值不参与比对。
/// </summary>
public static class VendorDuplicateKeys
{
    public const int MaxMatches = 10;

    public static string? NormalizeName(string? value) => CompactWhitespace(value);

    public static string? NormalizeEnglish(string? value)
    {
        var n = CompactWhitespace(value);
        return n == null ? null : n.ToUpperInvariant();
    }

    public static string? NormalizeCreditCode(string? value)
    {
        var n = CompactWhitespace(value);
        return n == null ? null : n.ToUpperInvariant();
    }

    public static string? NormalizeDuns(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var sb = new StringBuilder(value.Length);
        foreach (var c in value.Trim())
        {
            if (char.IsWhiteSpace(c) || c == '-')
                continue;
            sb.Append(c);
        }

        return sb.Length == 0 ? null : sb.ToString().ToUpperInvariant();
    }

    public static bool HasAnyKey(string? officialName, string? englishOfficialName, string? creditCode, string? duns) =>
        NormalizeName(officialName) != null
        || NormalizeEnglish(englishOfficialName) != null
        || NormalizeCreditCode(creditCode) != null
        || NormalizeDuns(duns) != null;

    public static bool IsMatch(
        string? inputOfficialName,
        string? inputEnglishOfficialName,
        string? inputCreditCode,
        string? inputDuns,
        string? existingOfficialName,
        string? existingEnglishOfficialName,
        string? existingCreditCode,
        string? existingDuns)
    {
        var official = NormalizeName(inputOfficialName);
        var english = NormalizeEnglish(inputEnglishOfficialName);
        var credit = NormalizeCreditCode(inputCreditCode);
        var duns = NormalizeDuns(inputDuns);
        if (official == null && english == null && credit == null && duns == null)
            return false;

        if (official != null && official == NormalizeName(existingOfficialName))
            return true;
        if (english != null && english == NormalizeEnglish(existingEnglishOfficialName))
            return true;
        if (credit != null && credit == NormalizeCreditCode(existingCreditCode))
            return true;
        if (duns != null && duns == NormalizeDuns(existingDuns))
            return true;
        return false;
    }

    private static string? CompactWhitespace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var sb = new StringBuilder(value.Length);
        foreach (var c in value.Trim())
        {
            if (char.IsWhiteSpace(c))
                continue;
            sb.Append(c);
        }

        return sb.Length == 0 ? null : sb.ToString();
    }
}
