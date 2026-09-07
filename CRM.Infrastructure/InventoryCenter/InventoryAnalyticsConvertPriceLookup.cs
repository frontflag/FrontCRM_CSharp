using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.InventoryCenter;

/// <summary>库存看板折算USD：批量取采购明细 <c>convert_price</c>。</summary>
internal static class InventoryAnalyticsConvertPriceLookup
{
    public static async Task<Dictionary<string, decimal>> LoadAsync(
        ApplicationDbContext db,
        IEnumerable<string?> purchaseOrderItemIds,
        int chunkSize,
        CancellationToken cancellationToken)
    {
        var ids = purchaseOrderItemIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        if (ids.Count == 0)
            return map;

        for (var i = 0; i < ids.Count; i += chunkSize)
        {
            var chunk = ids.Skip(i).Take(Math.Min(chunkSize, ids.Count - i)).ToList();
            var rows = await db.PurchaseOrderItems.AsNoTracking()
                .Where(p => chunk.Contains(p.Id))
                .Select(p => new { p.Id, p.ConvertPrice })
                .ToListAsync(cancellationToken);
            foreach (var row in rows)
                map[row.Id] = row.ConvertPrice;
        }

        return map;
    }
}
