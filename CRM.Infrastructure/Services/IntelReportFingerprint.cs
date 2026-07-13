using CRM.Infrastructure.Ai;

namespace CRM.Infrastructure.Services;

internal static class IntelReportFingerprint
{
    public static string Build(string companyName, string? creditCode)
    {
        var input = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["company_name"] = NormalizeKey(companyName),
            ["credit_code"] = NormalizeKey(creditCode)
        };
        var json = AiJsonHelper.CanonicalFingerprintJson(input, new[] { "company_name", "credit_code" });
        return AiJsonHelper.ComputeSha256Hex(json);
    }

    private static string NormalizeKey(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
}
