using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Utilities;
using CRM.Infrastructure.Common;
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

        if (!string.IsNullOrWhiteSpace(request.SellOrderItemCode))
        {
            var soc = request.SellOrderItemCode.Trim().ToLower();
            q = q.Where(x =>
                x.Item.SellOrderItemId != null
                && db.SellOrderItems.Any(soi =>
                    soi.Id == x.Item.SellOrderItemId
                    && soi.SellOrderItemCode != null
                    && soi.SellOrderItemCode.ToLower().Contains(soc)));
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
            var paymentStatuses = ProgressStatusFilterHelper.Normalize(request.PaymentProgressStatus);
            if (paymentStatuses.Count > 0)
            {
                var includeZero = paymentStatuses.Contains((short)0);
                var nonZero = paymentStatuses.Where(s => s != 0).ToList();
                q = q.Where(x =>
                    (includeZero && (x.Ext == null || x.Ext.PaymentProgressStatus == 0))
                    || (nonZero.Count > 0 && x.Ext != null && nonZero.Contains(x.Ext.PaymentProgressStatus)));
            }

            var purchaseStatuses = ProgressStatusFilterHelper.Normalize(request.PurchaseProgressStatus);
            if (purchaseStatuses.Count > 0)
            {
                var includeZero = purchaseStatuses.Contains((short)0);
                var nonZero = purchaseStatuses.Where(s => s != 0).ToList();
                q = q.Where(x =>
                    (includeZero && (x.Ext == null || x.Ext.PurchaseProgressStatus == 0))
                    || (nonZero.Count > 0 && x.Ext != null && nonZero.Contains(x.Ext.PurchaseProgressStatus)));
            }

            var stockInStatuses = ProgressStatusFilterHelper.Normalize(request.StockInProgressStatus);
            if (stockInStatuses.Count > 0)
            {
                var includeZero = stockInStatuses.Contains((short)0);
                var nonZero = stockInStatuses.Where(s => s != 0).ToList();
                q = q.Where(x =>
                    (includeZero && (x.Ext == null || x.Ext.StockInProgressStatus == 0))
                    || (nonZero.Count > 0 && x.Ext != null && nonZero.Contains(x.Ext.StockInProgressStatus)));
            }

            var invoiceStatuses = ProgressStatusFilterHelper.Normalize(request.InvoiceProgressStatus);
            if (invoiceStatuses.Count > 0)
            {
                var includeZero = invoiceStatuses.Contains((short)0);
                var nonZero = invoiceStatuses.Where(s => s != 0).ToList();
                q = q.Where(x =>
                    (includeZero && (x.Ext == null || x.Ext.InvoiceProgressStatus == 0))
                    || (nonZero.Count > 0 && x.Ext != null && nonZero.Contains(x.Ext.InvoiceProgressStatus)));
            }
        }

        q = ApplyQuickFilter(q, request.QuickFilter);

        if (PurchaseOrderItemAnalyticsDatasets.IsReportApproved(request.AnalyticsDataset))
            q = ApplyReportViewLens(db, q, request);

        return q;
    }

    private static IQueryable<PurchaseOrderItemLineJoin> ApplyReportViewLens(
        ApplicationDbContext db,
        IQueryable<PurchaseOrderItemLineJoin> q,
        PurchaseOrderItemListQueryRequest request)
    {
        var viewLevel = (request.AnalyticsViewLevel ?? string.Empty).Trim().ToLowerInvariant();
        if (viewLevel == SalesAnalyticsViewLevels.Personal
            && !string.IsNullOrWhiteSpace(request.PurchaseUserId))
        {
            var uid = request.PurchaseUserId.Trim();
            return q.Where(x => x.Po.PurchaseUserId == uid);
        }

        if (viewLevel == SalesAnalyticsViewLevels.Department)
        {
            var deptId = request.AnalyticsDepartmentId?.Trim();
            if (string.IsNullOrWhiteSpace(deptId))
                return q;

            if (string.Equals(deptId, PurchaseAnalyticsScopeValidator.UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase))
            {
                var withPrimary = db.RbacUserDepartments.AsNoTracking()
                    .Where(ud => ud.IsPrimary)
                    .Select(ud => ud.UserId);
                return q.Where(x =>
                    x.Po.PurchaseUserId == null
                    || !withPrimary.Contains(x.Po.PurchaseUserId));
            }

            var userIdsInDept = db.RbacUserDepartments.AsNoTracking()
                .Where(ud => ud.IsPrimary && ud.DepartmentId == deptId)
                .Select(ud => ud.UserId);
            return q.Where(x => x.Po.PurchaseUserId != null && userIdsInDept.Contains(x.Po.PurchaseUserId));
        }

        return q;
    }

    public static IQueryable<PurchaseOrderItemLineJoin> ApplyApprovedFilter(IQueryable<PurchaseOrderItemLineJoin> q) =>
        q.Where(x =>
            x.Po.Status >= 10
            && x.Item.Status != -2
            && !x.Item.IsDeleted);
}
