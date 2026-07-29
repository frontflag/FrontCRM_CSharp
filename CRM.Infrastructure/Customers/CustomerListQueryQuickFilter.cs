using CRM.Core.Constants;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Sales;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Customers;

internal static class CustomerListQueryQuickFilter
{
    public static IQueryable<CustomerInfo> Apply(
        ApplicationDbContext db,
        IQueryable<CustomerInfo> q,
        string? quickFilter)
    {
        if (string.IsNullOrWhiteSpace(quickFilter) || !CustomerListQuickFilterCodes.IsKnown(quickFilter))
            return q;

        var code = quickFilter.Trim();
        var today = DateTime.UtcNow.Date;

        return code switch
        {
            CustomerListQuickFilterCodes.HasDemand => q.Where(c =>
                db.RFQs.AsNoTracking().Any(r => r.CustomerId == c.Id)),

            CustomerListQuickFilterCodes.DemandLast7Days => ApplyDemandInWindow(db, q, today.AddDays(-6), today.AddDays(1)),
            CustomerListQuickFilterCodes.DemandLast30Days => ApplyDemandInWindow(db, q, today.AddDays(-29), today.AddDays(1)),

            CustomerListQuickFilterCodes.DemandStale6m => ApplyDemandStale(db, q, today.AddMonths(-6)),
            CustomerListQuickFilterCodes.DemandStale1y => ApplyDemandStale(db, q, today.AddYears(-1)),

            CustomerListQuickFilterCodes.HasDeal => q.Where(c =>
                db.SellOrders.AsNoTracking().Any(so =>
                    so.CustomerId == c.Id
                    && so.Status >= SellOrderMainStatus.Approved
                    && so.Status != SellOrderMainStatus.Cancelled
                    && so.Status != SellOrderMainStatus.AuditFailed)),

            CustomerListQuickFilterCodes.DealLast7Days => ApplyDealInWindow(db, q, today.AddDays(-6), today.AddDays(1)),
            CustomerListQuickFilterCodes.DealLast30Days => ApplyDealInWindow(db, q, today.AddDays(-29), today.AddDays(1)),

            CustomerListQuickFilterCodes.DealStale6m => ApplyDealStale(db, q, today.AddMonths(-6)),
            CustomerListQuickFilterCodes.DealStale1y => ApplyDealStale(db, q, today.AddYears(-1)),

            // 客单在库（与客户首页「在库」KPI 同口径）：StockType=客单、QtyRepertory>0，挂 CustomerId 或销售明细客户
            CustomerListQuickFilterCodes.PendingShipment => q.Where(c =>
                db.StockItems.AsNoTracking().Any(si =>
                    si.QtyRepertory > 0
                    && si.StockType == StockInventoryTypeCodes.CustomerOrder
                    && (
                        si.CustomerId == c.Id
                        || (si.SellOrderItemId != null
                            && db.SellOrderItems.AsNoTracking().Any(soi =>
                                soi.Id == si.SellOrderItemId
                                && db.SellOrders.AsNoTracking().Any(so =>
                                    so.Id == soi.SellOrderId && so.CustomerId == c.Id)))))),

            // 未结应收台账（与客户首页「应收」KPI 同口径）：VerifiedToBe > 0
            CustomerListQuickFilterCodes.HasReceivable => q.Where(c =>
                db.FinanceReceivables.AsNoTracking().Any(r =>
                    r.CustomerId == c.Id && r.VerifiedToBe > 0m)),

            _ => q
        };
    }

    private static IQueryable<CustomerInfo> ApplyDemandInWindow(
        ApplicationDbContext db,
        IQueryable<CustomerInfo> q,
        DateTime startInclusive,
        DateTime endExclusive) =>
        q.Where(c =>
            db.RFQs.AsNoTracking().Any(r =>
                r.CustomerId == c.Id && r.CreateTime >= startInclusive && r.CreateTime < endExclusive));

    private static IQueryable<CustomerInfo> ApplyDemandStale(
        ApplicationDbContext db,
        IQueryable<CustomerInfo> q,
        DateTime recentCutoffInclusive) =>
        q.Where(c =>
            db.RFQs.AsNoTracking().Any(r => r.CustomerId == c.Id)
            && !db.RFQs.AsNoTracking().Any(r => r.CustomerId == c.Id && r.CreateTime >= recentCutoffInclusive));

    private static IQueryable<CustomerInfo> ApplyDealInWindow(
        ApplicationDbContext db,
        IQueryable<CustomerInfo> q,
        DateTime startInclusive,
        DateTime endExclusive) =>
        q.Where(c =>
            db.SellOrders.AsNoTracking().Any(so =>
                so.CustomerId == c.Id
                && so.Status >= SellOrderMainStatus.Approved
                && so.Status != SellOrderMainStatus.Cancelled
                && so.Status != SellOrderMainStatus.AuditFailed
                && so.CreateTime >= startInclusive
                && so.CreateTime < endExclusive));

    private static IQueryable<CustomerInfo> ApplyDealStale(
        ApplicationDbContext db,
        IQueryable<CustomerInfo> q,
        DateTime recentCutoffInclusive) =>
        q.Where(c =>
            db.SellOrders.AsNoTracking().Any(so =>
                so.CustomerId == c.Id
                && so.Status >= SellOrderMainStatus.Approved
                && so.Status != SellOrderMainStatus.Cancelled
                && so.Status != SellOrderMainStatus.AuditFailed)
            && !db.SellOrders.AsNoTracking().Any(so =>
                so.CustomerId == c.Id
                && so.Status >= SellOrderMainStatus.Approved
                && so.Status != SellOrderMainStatus.Cancelled
                && so.Status != SellOrderMainStatus.AuditFailed
                && so.CreateTime >= recentCutoffInclusive));
}
