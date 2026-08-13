using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Finance;

internal static class FinanceCustomerDisplayEnrichment
{
    public static async Task<IReadOnlyDictionary<string, (string? Zh, string? En)>> LoadMapAsync(
        ApplicationDbContext db,
        IEnumerable<string?> customerIds,
        CancellationToken cancellationToken = default)
    {
        var ids = customerIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (ids.Count == 0)
            return new Dictionary<string, (string? Zh, string? En)>(StringComparer.OrdinalIgnoreCase);

        var rows = await db.Customers.AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .Select(c => new { c.Id, c.OfficialName, c.NickName, c.EnglishOfficialName })
            .ToListAsync(cancellationToken);

        var map = new Dictionary<string, (string? Zh, string? En)>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in rows)
        {
            var zh = !string.IsNullOrWhiteSpace(r.OfficialName) ? r.OfficialName.Trim()
                : (!string.IsNullOrWhiteSpace(r.NickName) ? r.NickName.Trim() : null);
            var en = string.IsNullOrWhiteSpace(r.EnglishOfficialName) ? null : r.EnglishOfficialName.Trim();
            map[r.Id] = (zh, en);
        }

        return map;
    }
}
