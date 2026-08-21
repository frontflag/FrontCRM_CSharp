using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.PurchaseOrders;

/// <summary>按 PN+品牌合计备货类型在库可用量（与销售侧备货池口径一致）。</summary>
internal static class StockingAvailableQtyLookup
{
    public static async Task ApplyAsync(
        ApplicationDbContext db,
        IReadOnlyList<PurchaseOrderItemListLineRaw> lines,
        CancellationToken cancellationToken)
    {
        if (lines.Count == 0)
            return;

        var keys = lines
            .Select(l => PurchasePnBrandKey.Combine(l.Pn, l.Brand))
            .Where(k => k.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (keys.Count == 0)
            return;

        var pnKeys = keys
            .Select(k => k.Split('\u001f')[0])
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var stockRows = await db.StockItems.AsNoTracking()
            .Where(s => s.StockType == StockInventoryTypeCodes.Stocking
                        && (s.TransferType == null
                            || s.TransferType != StockItemTransferTypeCodes.ManualTransferSource)
                        && s.PurchasePn != null
                        && s.PurchaseBrand != null
                        && pnKeys.Contains(s.PurchasePn.ToUpper()))
            .Select(s => new { s.PurchasePn, s.PurchaseBrand, s.QtyRepertoryAvailable })
            .ToListAsync(cancellationToken);

        var sums = SumClampedAvailableByPnBrand(stockRows.Select(r => (r.PurchasePn, r.PurchaseBrand, r.QtyRepertoryAvailable)));

        foreach (var line in lines)
        {
            var key = PurchasePnBrandKey.Combine(line.Pn, line.Brand);
            if (key.Length == 0)
            {
                line.StockingAvailableQty = 0;
                continue;
            }

            sums.TryGetValue(key, out var qty);
            line.StockingAvailableQty = qty;
        }
    }

    /// <summary>仅保留可用库存数量 &gt; 0 的备货采购明细（与列表「可用库存数量」同口径）。</summary>
    public static async Task<IQueryable<PurchaseOrderItemLineJoin>> RestrictToPositiveAvailableAsync(
        ApplicationDbContext db,
        IQueryable<PurchaseOrderItemLineJoin> q,
        CancellationToken cancellationToken)
    {
        var keys = await LoadPositivePnBrandKeysAsync(db, cancellationToken);
        if (keys.Count == 0)
            return q.Where(x => false);

        return q.Where(x =>
            x.Item.PN != null
            && x.Item.Brand != null
            && keys.Contains(x.Item.PN.Trim().ToUpper() + "\u001f" + x.Item.Brand.Trim().ToUpper()));
    }

    private static async Task<List<string>> LoadPositivePnBrandKeysAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var stockRows = await db.StockItems.AsNoTracking()
            .Where(s => s.StockType == StockInventoryTypeCodes.Stocking
                        && (s.TransferType == null
                            || s.TransferType != StockItemTransferTypeCodes.ManualTransferSource)
                        && s.PurchasePn != null
                        && s.PurchaseBrand != null)
            .Select(s => new { s.PurchasePn, s.PurchaseBrand, s.QtyRepertoryAvailable })
            .ToListAsync(cancellationToken);

        return SumClampedAvailableByPnBrand(stockRows.Select(r => (r.PurchasePn, r.PurchaseBrand, r.QtyRepertoryAvailable)))
            .Where(kv => kv.Value > 0)
            .Select(kv => kv.Key)
            .ToList();
    }

    private static Dictionary<string, int> SumClampedAvailableByPnBrand(
        IEnumerable<(string? Pn, string? Brand, int Qty)> rows)
    {
        var sums = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var key = PurchasePnBrandKey.Combine(row.Pn, row.Brand);
            if (key.Length == 0)
                continue;
            if (!sums.TryGetValue(key, out var acc))
                acc = 0;
            var next = acc + row.Qty;
            sums[key] = next < 0 ? 0 : next;
        }

        return sums;
    }
}
