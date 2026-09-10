using CRM.Core.Models.Customs;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.API.Utilities;

/// <summary>报关单 ↔ 装箱单：头表 packing_id，或装箱单 customs_declaration_id 兜底。</summary>
internal static class CustomsDeclarationPackingLookup
{
    public readonly record struct PackingRef(string Id, string Code);

    public static IQueryable<CustomsDeclaration> WherePackingCode(
        IQueryable<CustomsDeclaration> dq,
        ApplicationDbContext db,
        string packingCode)
    {
        var k = packingCode.Trim();
        if (string.IsNullOrEmpty(k))
            return dq;

        return dq.Where(d =>
            db.Packings.Any(p =>
                !p.IsDeleted
                && EF.Functions.ILike(p.Code, $"%{k}%")
                && ((d.PackingId != null && d.PackingId != "" && p.Id == d.PackingId)
                    || (p.CustomsDeclarationId != null && p.CustomsDeclarationId == d.Id))));
    }

    public static async Task<IReadOnlyDictionary<string, PackingRef>> LoadByDeclarationsAsync(
        ApplicationDbContext db,
        IReadOnlyList<(string DeclarationId, string? PackingId)> rows,
        CancellationToken cancellationToken = default)
    {
        var decIds = rows
            .Select(x => x.DeclarationId.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var packingIds = rows
            .Select(x => x.PackingId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (decIds.Count == 0)
            return new Dictionary<string, PackingRef>(StringComparer.OrdinalIgnoreCase);

        var packings = await db.Packings.AsNoTracking()
            .Where(p => !p.IsDeleted
                        && ((packingIds.Count > 0 && packingIds.Contains(p.Id))
                            || (p.CustomsDeclarationId != null
                                && p.CustomsDeclarationId != ""
                                && decIds.Contains(p.CustomsDeclarationId))))
            .Select(p => new { p.Id, p.Code, p.CustomsDeclarationId })
            .ToListAsync(cancellationToken);

        var byPackingId = packings
            .Where(p => !string.IsNullOrWhiteSpace(p.Id) && !string.IsNullOrWhiteSpace(p.Code))
            .GroupBy(p => p.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => new PackingRef(g.First().Id.Trim(), g.First().Code.Trim()),
                StringComparer.OrdinalIgnoreCase);
        var byDecId = packings
            .Where(p => !string.IsNullOrWhiteSpace(p.CustomsDeclarationId) && !string.IsNullOrWhiteSpace(p.Code))
            .GroupBy(p => p.CustomsDeclarationId!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => new PackingRef(g.First().Id.Trim(), g.First().Code.Trim()),
                StringComparer.OrdinalIgnoreCase);

        var map = new Dictionary<string, PackingRef>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var decId = row.DeclarationId.Trim();
            if (decId.Length == 0 || map.ContainsKey(decId))
                continue;
            var headerId = row.PackingId?.Trim();
            if (!string.IsNullOrEmpty(headerId) && byPackingId.TryGetValue(headerId, out var byHeader))
            {
                map[decId] = byHeader;
                continue;
            }

            if (byDecId.TryGetValue(decId, out var byReverse))
                map[decId] = byReverse;
        }

        return map;
    }
}
