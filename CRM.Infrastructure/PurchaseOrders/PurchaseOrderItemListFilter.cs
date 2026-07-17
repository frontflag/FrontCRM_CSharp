using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.PurchaseOrders;

/// <summary>采购订单明细列表筛选条件（列表分页与看板共用）。</summary>
internal static partial class PurchaseOrderItemListFilter
{
    public static async Task<IQueryable<PurchaseOrderItemLineJoin>> BuildFilteredJoinQueryAsync(
        ApplicationDbContext db,
        IDataPermissionService dataPermission,
        PurchaseOrderItemListQueryRequest request,
        CancellationToken cancellationToken)
    {
        var scopedPo = await dataPermission.ApplyPurchaseOrderDataScopeAsync(
            request.CurrentUserId,
            db.PurchaseOrders.AsNoTracking(),
            cancellationToken);

        var q =
            from item in db.PurchaseOrderItems.AsNoTracking()
            join po in scopedPo on item.PurchaseOrderId equals po.Id
            join ext in db.PurchaseOrderItemExtends.AsNoTracking().Where(e => !e.IsDeleted)
                on item.Id equals ext.Id into extGroup
            from ext in extGroup.DefaultIfEmpty()
            select new PurchaseOrderItemLineJoin { Item = item, Po = po, Ext = ext };

        if (request.StartDate.HasValue)
        {
            var s = SalesAnalyticsDateFilter.ToUtcDateStart(request.StartDate.Value);
            q = q.Where(x => x.Po.CreateTime >= s);
        }

        if (request.EndDate.HasValue)
        {
            var e = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(request.EndDate.Value);
            q = q.Where(x => x.Po.CreateTime < e);
        }

        if (!string.IsNullOrWhiteSpace(request.PurchaseOrderCode))
        {
            var c = request.PurchaseOrderCode.Trim();
            q = q.Where(x =>
                x.Po.PurchaseOrderCode != null &&
                x.Po.PurchaseOrderCode.ToLower().Contains(c.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.FreightForwarderOrderNo))
        {
            var f = request.FreightForwarderOrderNo.Trim();
            q = q.Where(x =>
                x.Po.FreightForwarderOrderNo != null &&
                x.Po.FreightForwarderOrderNo.ToLower().Contains(f.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.VendorName))
        {
            var v = request.VendorName.Trim();
            q = q.Where(x =>
                x.Po.VendorName != null &&
                x.Po.VendorName.ToLower().Contains(v.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.PurchaseUserName))
        {
            var p = request.PurchaseUserName.Trim();
            q = q.Where(x =>
                x.Po.PurchaseUserName != null &&
                x.Po.PurchaseUserName.ToLower().Contains(p.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.Pn))
        {
            var pn = request.Pn.Trim();
            q = q.Where(x =>
                x.Item.PN != null &&
                x.Item.PN.ToLower().Contains(pn.ToLower()));
        }

        if (request.OrderType.HasValue)
            q = q.Where(x => x.Po.Type == request.OrderType.Value);

        if (!string.IsNullOrWhiteSpace(request.TransactionCurrency))
        {
            var kind = request.TransactionCurrency.Trim().ToLowerInvariant();
            if (kind is "rmb" or "cny" or "人民币")
                q = q.Where(x => x.Item.Currency == (short)CurrencyCode.RMB);
            else if (kind is "foreign" or "外币")
                q = q.Where(x => x.Item.Currency != (short)CurrencyCode.RMB);
        }

        var hasQuickFilter = !string.IsNullOrWhiteSpace(request.QuickFilter)
            && PurchaseOrderItemListQuickFilterCodes.IsKnown(request.QuickFilter);

        if (!hasQuickFilter)
        {
            if (request.PaymentProgressStatus is >= 0 and <= 2)
            {
                var status = request.PaymentProgressStatus.Value;
                q = status == 0
                    ? q.Where(x => x.Ext == null || x.Ext.PaymentProgressStatus == 0)
                    : q.Where(x => x.Ext != null && x.Ext.PaymentProgressStatus == status);
            }

            if (request.PurchaseProgressStatus is >= 0 and <= 2)
            {
                var status = request.PurchaseProgressStatus.Value;
                q = status == 0
                    ? q.Where(x => x.Ext == null || x.Ext.PurchaseProgressStatus == 0)
                    : q.Where(x => x.Ext != null && x.Ext.PurchaseProgressStatus == status);
            }

            if (request.StockInProgressStatus is >= 0 and <= 2)
            {
                var status = request.StockInProgressStatus.Value;
                q = status == 0
                    ? q.Where(x => x.Ext == null || x.Ext.StockInProgressStatus == 0)
                    : q.Where(x => x.Ext != null && x.Ext.StockInProgressStatus == status);
            }

            if (request.InvoiceProgressStatus is >= 0 and <= 2)
            {
                var status = request.InvoiceProgressStatus.Value;
                q = status == 0
                    ? q.Where(x => x.Ext == null || x.Ext.InvoiceProgressStatus == 0)
                    : q.Where(x => x.Ext != null && x.Ext.InvoiceProgressStatus == status);
            }
        }

        q = ApplyQuickFilter(q, request.QuickFilter);
        return q;
    }

    public static IQueryable<PurchaseOrderItemLineJoin> ApplyApprovedFilter(IQueryable<PurchaseOrderItemLineJoin> q) =>
        q.Where(x =>
            x.Po.Status >= 10
            && x.Item.Status != -2
            && !x.Item.IsDeleted);
}
