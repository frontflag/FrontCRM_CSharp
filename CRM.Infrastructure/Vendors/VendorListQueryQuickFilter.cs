using CRM.Core.Constants;
using CRM.Core.Models.Vendor;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Vendors;

internal static class VendorListQueryQuickFilter
{
    private const short PoApproved = 10;
    private const short PoCancelled = -2;
    private const short PoAuditFailed = -1;

    public static IQueryable<VendorInfo> Apply(
        ApplicationDbContext db,
        IQueryable<VendorInfo> q,
        string? quickFilter)
    {
        if (string.IsNullOrWhiteSpace(quickFilter) || !VendorListQuickFilterCodes.IsKnown(quickFilter))
            return q;

        var code = quickFilter.Trim();
        var today = DateTime.UtcNow.Date;

        return code switch
        {
            VendorListQuickFilterCodes.HasQuote => q.Where(v =>
                db.QuoteItems.AsNoTracking().Any(qi =>
                    qi.VendorId == v.Id && qi.VendorId != null && qi.VendorId != "")),

            VendorListQuickFilterCodes.QuoteLast7Days => ApplyQuoteInWindow(db, q, today.AddDays(-6), today.AddDays(1)),
            VendorListQuickFilterCodes.QuoteLast30Days => ApplyQuoteInWindow(db, q, today.AddDays(-29), today.AddDays(1)),

            VendorListQuickFilterCodes.QuoteStale6m => ApplyQuoteStale(db, q, today.AddMonths(-6)),
            VendorListQuickFilterCodes.QuoteStale1y => ApplyQuoteStale(db, q, today.AddYears(-1)),

            VendorListQuickFilterCodes.HasPurchase => q.Where(v =>
                db.PurchaseOrders.AsNoTracking().Any(po =>
                    po.VendorId == v.Id
                    && po.Status >= PoApproved
                    && po.Status != PoCancelled
                    && po.Status != PoAuditFailed)),

            VendorListQuickFilterCodes.PurchaseLast7Days => ApplyPurchaseInWindow(db, q, today.AddDays(-6), today.AddDays(1)),
            VendorListQuickFilterCodes.PurchaseLast30Days => ApplyPurchaseInWindow(db, q, today.AddDays(-29), today.AddDays(1)),

            VendorListQuickFilterCodes.PurchaseStale6m => ApplyPurchaseStale(db, q, today.AddMonths(-6)),
            VendorListQuickFilterCodes.PurchaseStale1y => ApplyPurchaseStale(db, q, today.AddYears(-1)),

            VendorListQuickFilterCodes.PendingInbound => q.Where(v =>
                db.PurchaseOrders.AsNoTracking().Any(po =>
                    po.VendorId == v.Id
                    && po.Status >= PoApproved
                    && po.Status != PoCancelled
                    && po.Status != PoAuditFailed
                    && po.StockStatus < 2)),

            VendorListQuickFilterCodes.HasPayable => q.Where(v =>
                db.PurchaseOrders.AsNoTracking().Any(po =>
                    po.VendorId == v.Id
                    && po.Status >= PoApproved
                    && po.Status != PoCancelled
                    && po.Status != PoAuditFailed
                    && po.FinanceStatus < 2)),

            _ => q
        };
    }

    private static IQueryable<VendorInfo> ApplyQuoteInWindow(
        ApplicationDbContext db,
        IQueryable<VendorInfo> q,
        DateTime startInclusive,
        DateTime endExclusive) =>
        q.Where(v =>
            db.QuoteItems.AsNoTracking().Any(qi =>
                qi.VendorId == v.Id
                && qi.VendorId != null
                && qi.VendorId != ""
                && db.Quotes.AsNoTracking().Any(qt =>
                    qt.Id == qi.QuoteId
                    && qt.QuoteDate >= startInclusive
                    && qt.QuoteDate < endExclusive)));

    private static IQueryable<VendorInfo> ApplyQuoteStale(
        ApplicationDbContext db,
        IQueryable<VendorInfo> q,
        DateTime recentCutoffInclusive) =>
        q.Where(v =>
            db.QuoteItems.AsNoTracking().Any(qi =>
                qi.VendorId == v.Id && qi.VendorId != null && qi.VendorId != "")
            && !db.QuoteItems.AsNoTracking().Any(qi =>
                qi.VendorId == v.Id
                && qi.VendorId != null
                && qi.VendorId != ""
                && db.Quotes.AsNoTracking().Any(qt =>
                    qt.Id == qi.QuoteId && qt.QuoteDate >= recentCutoffInclusive)));

    private static IQueryable<VendorInfo> ApplyPurchaseInWindow(
        ApplicationDbContext db,
        IQueryable<VendorInfo> q,
        DateTime startInclusive,
        DateTime endExclusive) =>
        q.Where(v =>
            db.PurchaseOrders.AsNoTracking().Any(po =>
                po.VendorId == v.Id
                && po.Status >= PoApproved
                && po.Status != PoCancelled
                && po.Status != PoAuditFailed
                && po.CreateTime >= startInclusive
                && po.CreateTime < endExclusive));

    private static IQueryable<VendorInfo> ApplyPurchaseStale(
        ApplicationDbContext db,
        IQueryable<VendorInfo> q,
        DateTime recentCutoffInclusive) =>
        q.Where(v =>
            db.PurchaseOrders.AsNoTracking().Any(po =>
                po.VendorId == v.Id
                && po.Status >= PoApproved
                && po.Status != PoCancelled
                && po.Status != PoAuditFailed)
            && !db.PurchaseOrders.AsNoTracking().Any(po =>
                po.VendorId == v.Id
                && po.Status >= PoApproved
                && po.Status != PoCancelled
                && po.Status != PoAuditFailed
                && po.CreateTime >= recentCutoffInclusive));
}
