namespace CRM.Core.Utilities;

/// <summary>需求分配：物料型号精确匹配键（Trim + 忽略大小写）。</summary>
public static class RfqMpnMatch
{
    public static string NormalizeKey(string? mpn) =>
        string.IsNullOrWhiteSpace(mpn) ? string.Empty : mpn.Trim().ToUpperInvariant();

    public static bool IsExactMatch(string? left, string? right) =>
        !string.IsNullOrEmpty(NormalizeKey(left))
        && NormalizeKey(left) == NormalizeKey(right);
}
