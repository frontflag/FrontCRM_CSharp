using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Finance;

/// <summary>付款单列表筛选（分页与看板共用）。</summary>
internal static class FinancePaymentListFilter
{
    public static async Task<IQueryable<FinancePayment>> BuildFilteredQueryAsync(
        ApplicationDbContext db,
        IDataPermissionService dataPermission,
        FinancePaymentQueryRequest? request,
        CancellationToken cancellationToken)
    {
        var q = db.FinancePayments.AsNoTracking();
        if (request != null && !string.IsNullOrWhiteSpace(request.FinancePaymentCode))
            q = db.FinancePayments.IgnoreQueryFilters().AsNoTracking();

        q = await dataPermission.ApplyFinancePaymentListDataScopeAsync(
            request?.CurrentUserId,
            q,
            db.Vendors.AsNoTracking(),
            cancellationToken);

        if (request == null)
            return q;

        if (!string.IsNullOrWhiteSpace(request.FinancePaymentCode))
        {
            var c = request.FinancePaymentCode.Trim().ToLowerInvariant();
            q = q.Where(p => p.FinancePaymentCode.ToLower().Contains(c));
        }
        else if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var k = request.Keyword.Trim().ToLowerInvariant();
            q = q.Where(p =>
                (p.FinancePaymentCode != null && p.FinancePaymentCode.ToLower().Contains(k)) ||
                (p.VendorName != null && p.VendorName.ToLower().Contains(k)));
        }

        if (!string.IsNullOrWhiteSpace(request.FreightForwarderOrderNo))
            q = await ApplyFreightForwarderOrderNoFilterAsync(
                db, q, request.FreightForwarderOrderNo.Trim(), cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.PurchaseOrderCode))
        {
            var code = request.PurchaseOrderCode.Trim().ToLowerInvariant();
            var poIds = await db.PurchaseOrders.AsNoTracking()
                .Where(po => po.PurchaseOrderCode != null && po.PurchaseOrderCode.ToLower().Contains(code))
                .Select(po => po.Id)
                .ToListAsync(cancellationToken);
            q = await ApplyLinkedPurchaseOrderFilterAsync(db, q, poIds, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.PurchaseUserName))
        {
            var name = request.PurchaseUserName.Trim().ToLowerInvariant();
            var poIds = await db.PurchaseOrders.AsNoTracking()
                .Where(po => po.PurchaseUserName != null && po.PurchaseUserName.ToLower().Contains(name))
                .Select(po => po.Id)
                .ToListAsync(cancellationToken);
            q = await ApplyLinkedPurchaseOrderFilterAsync(db, q, poIds, cancellationToken);
        }

        if (request.PurchaseCurrency.HasValue)
        {
            var currency = request.PurchaseCurrency.Value;
            var poIds = await db.PurchaseOrders.AsNoTracking()
                .Where(po => po.Currency == currency)
                .Select(po => po.Id)
                .ToListAsync(cancellationToken);
            q = await ApplyLinkedPurchaseOrderFilterAsync(db, q, poIds, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.BankSlipNo))
        {
            var b = request.BankSlipNo.Trim().ToLowerInvariant();
            q = q.Where(p => p.BankSlipNo != null && p.BankSlipNo.ToLower().Contains(b));
        }

        if (request.PaymentMode.HasValue)
            q = q.Where(p => p.PaymentMode == request.PaymentMode.Value);

        if (!string.IsNullOrWhiteSpace(request.VendorName))
        {
            var v = request.VendorName.Trim().ToLowerInvariant();
            q = q.Where(p => p.VendorName != null && p.VendorName.ToLower().Contains(v));
        }

        if (!string.IsNullOrWhiteSpace(request.Remark))
        {
            var r = request.Remark.Trim().ToLowerInvariant();
            q = q.Where(p => p.Remark != null && p.Remark.ToLower().Contains(r));
        }

        if (request.Status.HasValue)
            q = q.Where(p => p.Status == request.Status.Value);

        if (request.StartDate.HasValue)
        {
            var startUtc = PostgreSqlDateTime.ToUtc(request.StartDate.Value);
            q = q.Where(p => p.PaymentDate != null && p.PaymentDate >= startUtc);
        }

        if (request.EndDate.HasValue)
        {
            var endExclusiveUtc = PostgreSqlDateTime.ToUtc(request.EndDate.Value).AddDays(1);
            q = q.Where(p => p.PaymentDate != null && p.PaymentDate <= endExclusiveUtc);
        }

        return q;
    }

    private static async Task<IQueryable<FinancePayment>> ApplyFreightForwarderOrderNoFilterAsync(
        ApplicationDbContext db,
        IQueryable<FinancePayment> q,
        string freightForwarderOrderNo,
        CancellationToken cancellationToken)
    {
        var ff = freightForwarderOrderNo.ToLowerInvariant();
        var matchingPoIds = await db.PurchaseOrders.AsNoTracking()
            .Where(po =>
                po.FreightForwarderOrderNo != null &&
                po.FreightForwarderOrderNo.ToLower().Contains(ff))
            .Select(po => po.Id)
            .ToListAsync(cancellationToken);

        if (matchingPoIds.Count == 0)
            return q.Where(_ => false);

        return await ApplyLinkedPurchaseOrderFilterAsync(db, q, matchingPoIds, cancellationToken);
    }

    private static async Task<IQueryable<FinancePayment>> ApplyLinkedPurchaseOrderFilterAsync(
        ApplicationDbContext db,
        IQueryable<FinancePayment> q,
        List<string> matchingPoIds,
        CancellationToken cancellationToken)
    {
        if (matchingPoIds.Count == 0)
            return q.Where(_ => false);

        var matchingPoiIds = await db.PurchaseOrderItems.AsNoTracking()
            .Where(poi => matchingPoIds.Contains(poi.PurchaseOrderId))
            .Select(poi => poi.Id)
            .ToListAsync(cancellationToken);

        return q.Where(p => db.FinancePaymentItems.Any(item =>
            item.FinancePaymentId == p.Id &&
            ((item.PurchaseOrderId != null && matchingPoIds.Contains(item.PurchaseOrderId))
             || (item.PurchaseOrderItemId != null && matchingPoiIds.Contains(item.PurchaseOrderItemId)))));
    }
}
