namespace CRM.Core.Utilities;

/// <summary>运维检查建议文案：仅使用业务单号，禁止 GUID。</summary>
public static class OpsCheckDocumentCodes
{
    public const string Missing = "单号缺失";

    public static bool LooksLikeGuid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return Guid.TryParse(value.Trim(), out _);
    }

    public static bool IsUsableCode(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !LooksLikeGuid(value);

    /// <summary>建议文案用单号：依次取第一个非空且非 GUID 的值。</summary>
    public static string ForSuggestion(string? primary, params string?[] alternates)
    {
        if (IsUsableCode(primary))
            return primary!.Trim();

        foreach (var alt in alternates)
        {
            if (IsUsableCode(alt))
                return alt!.Trim();
        }

        return Missing;
    }

    public static List<string> FilterCodes(IEnumerable<string?>? codes) =>
        (codes ?? Array.Empty<string?>())
            .Where(IsUsableCode)
            .Select(c => c!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
