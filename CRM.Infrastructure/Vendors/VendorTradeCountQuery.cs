using CRM.Core.Interfaces;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Vendors;

/// <summary>供应商交易次数：有效采购货款付款单 × distinct 采购明细。</summary>
public sealed class VendorTradeCountQuery : IVendorTradeCountQuery
{
    private readonly ApplicationDbContext _db;

    public VendorTradeCountQuery(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, int>> GetTradeCountsAsync(
        IReadOnlyCollection<string> vendorIds,
        CancellationToken cancellationToken = default)
    {
        var ids = (vendorIds ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in ids)
            result[id] = 0;
        if (ids.Count == 0)
            return result;

        var rows = await (
            from item in _db.FinancePaymentItems.AsNoTracking()
            join pay in _db.FinancePayments.AsNoTracking() on item.FinancePaymentId equals pay.Id
            join poi in _db.PurchaseOrderItems.AsNoTracking() on item.PurchaseOrderItemId equals poi.Id
            where pay.Status != VendorTradeCountRules.PaymentCancelled
                  && pay.Status != VendorTradeCountRules.PaymentAuditFailed
                  && item.PurchaseOrderItemId != null
                  && item.PurchaseOrderItemId != ""
                  && ids.Contains(pay.VendorId)
            group item by pay.VendorId into g
            select new
            {
                VendorId = g.Key,
                Count = g.Select(x => x.PurchaseOrderItemId).Distinct().Count()
            }).ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.VendorId)) continue;
            result[row.VendorId] = row.Count;
        }

        return result;
    }
}
