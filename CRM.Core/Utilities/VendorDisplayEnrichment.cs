using CRM.Core.Interfaces;
using CRM.Core.Models.Vendor;

namespace CRM.Core.Utilities;

/// <summary>供应商展示字段补全（英文名称等，不落库）。</summary>
public static class VendorDisplayEnrichment
{
    public static async Task<IReadOnlyDictionary<string, string>> LoadEnglishOfficialNameMapAsync(
        IRepository<VendorInfo> vendorRepo,
        IEnumerable<string?> vendorIds)
    {
        var ids = vendorIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (ids.Length == 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var vendors = (await vendorRepo.FindAsync(v => ids.Contains(v.Id))).ToList();
        return vendors
            .Where(v => !string.IsNullOrWhiteSpace(v.EnglishOfficialName))
            .GroupBy(v => v.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.First().EnglishOfficialName!.Trim(),
                StringComparer.OrdinalIgnoreCase);
    }

    public static string? ResolveEnglishOfficialName(
        IReadOnlyDictionary<string, string> englishMap,
        string? vendorId)
    {
        if (string.IsNullOrWhiteSpace(vendorId)) return null;
        var key = vendorId.Trim();
        return englishMap.TryGetValue(key, out var english) && !string.IsNullOrWhiteSpace(english)
            ? english.Trim()
            : null;
    }
}
