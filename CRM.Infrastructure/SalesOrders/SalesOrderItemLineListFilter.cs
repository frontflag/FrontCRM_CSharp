using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;
using CRM.Infrastructure.Common;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.SalesOrders;

/// <summary>销售订单明细列表筛选条件（列表分页与看板共用）。</summary>
internal static partial class SalesOrderItemLineListFilter
{
    public static async Task<IQueryable<SellOrderItemLineJoin>> BuildFilteredJoinQueryAsync(
        ApplicationDbContext db,
        IDataPermissionService dataPermission,
        SellOrderItemLineQueryRequest request,
        CancellationToken cancellationToken)
    {
        var scopedSo = await dataPermission.ApplySellOrderDataScopeAsync(
            request.CurrentUserId,
            db.SellOrders.AsNoTracking(),
            cancellationToken);

        var q =
            from item in db.SellOrderItems.AsNoTracking()
            join so in scopedSo on item.SellOrderId equals so.Id
            select new SellOrderItemLineJoin { Item = item, So = so };

        if (request.OrderCreateStart.HasValue)
        {
            var s = SalesAnalyticsDateFilter.ToUtcDateStart(request.OrderCreateStart.Value);
            q = q.Where(x => x.So.CreateTime >= s);
        }

        if (request.OrderCreateEnd.HasValue)
        {
            var e = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(request.OrderCreateEnd.Value);
            q = q.Where(x => x.So.CreateTime < e);
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerName))
        {
            var k = request.CustomerName.Trim();
            q = q.Where(x =>
                x.So.CustomerName != null &&
                x.So.CustomerName.ToLower().Contains(k.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.SalesUserName))
        {
            var sk = request.SalesUserName.Trim();
            q = q.Where(x =>
                x.So.SalesUserName != null &&
                x.So.SalesUserName.ToLower().Contains(sk.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.SellOrderCode))
        {
            var c = request.SellOrderCode.Trim();
            q = q.Where(x =>
                x.So.SellOrderCode != null &&
                x.So.SellOrderCode.ToLower().Contains(c.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.Pn))
        {
            var pn = request.Pn.Trim();
            q = q.Where(x =>
                x.Item.PN != null &&
                x.Item.PN.ToLower().Contains(pn.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerSo))
        {
            var k = request.CustomerSo.Trim();
            q = q.Where(x =>
                x.Item.CustomerSo != null &&
                x.Item.CustomerSo.ToLower().Contains(k.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerPn))
        {
            var k = request.CustomerPn.Trim();
            q = q.Where(x =>
                x.Item.CustomerPn != null &&
                x.Item.CustomerPn.ToLower().Contains(k.ToLower()));
        }

        var hasQuickFilter = !string.IsNullOrWhiteSpace(request.QuickFilter)
            && SellOrderItemListQuickFilterCodes.IsKnown(request.QuickFilter);

        if (!hasQuickFilter)
        {
            var purchaseStatuses = ProgressStatusFilterHelper.Normalize(request.PurchaseProgressStatus);
            if (purchaseStatuses.Count > 0)
            {
                var includeZero = purchaseStatuses.Contains((short)0);
                var nonZero = purchaseStatuses.Where(s => s != 0).ToList();
                q = q.Where(x =>
                    (includeZero && (
                        !db.SellOrderItemExtends.Any(ext => ext.Id == x.Item.Id && !ext.IsDeleted)
                        || db.SellOrderItemExtends.Any(ext =>
                            ext.Id == x.Item.Id && !ext.IsDeleted && ext.PurchaseProgressStatus == 0)))
                    || (nonZero.Count > 0 && db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id && !ext.IsDeleted && nonZero.Contains(ext.PurchaseProgressStatus))));
            }

            var stockInStatuses = ProgressStatusFilterHelper.Normalize(request.StockInProgressStatus);
            if (stockInStatuses.Count > 0)
            {
                var includeZero = stockInStatuses.Contains((short)0);
                var nonZero = stockInStatuses.Where(s => s != 0).ToList();
                q = q.Where(x =>
                    (includeZero && (
                        !db.SellOrderItemExtends.Any(ext => ext.Id == x.Item.Id && !ext.IsDeleted)
                        || db.SellOrderItemExtends.Any(ext =>
                            ext.Id == x.Item.Id && !ext.IsDeleted && ext.StockInProgressStatus == 0)))
                    || (nonZero.Count > 0 && db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id && !ext.IsDeleted && nonZero.Contains(ext.StockInProgressStatus))));
            }

            var stockOutStatuses = ProgressStatusFilterHelper.Normalize(request.StockOutProgressStatus);
            if (stockOutStatuses.Count > 0)
            {
                var includeZero = stockOutStatuses.Contains((short)0);
                var nonZero = stockOutStatuses.Where(s => s != 0).ToList();
                q = q.Where(x =>
                    (includeZero && (
                        !db.SellOrderItemExtends.Any(ext => ext.Id == x.Item.Id && !ext.IsDeleted)
                        || db.SellOrderItemExtends.Any(ext =>
                            ext.Id == x.Item.Id && !ext.IsDeleted && ext.StockOutProgressStatus == 0)))
                    || (nonZero.Count > 0 && db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id && !ext.IsDeleted && nonZero.Contains(ext.StockOutProgressStatus))));
            }

            var receiptStatuses = ProgressStatusFilterHelper.Normalize(request.ReceiptProgressStatus);
            if (receiptStatuses.Count > 0)
            {
                var includeZero = receiptStatuses.Contains((short)0);
                var nonZero = receiptStatuses.Where(s => s != 0).ToList();
                q = q.Where(x =>
                    (includeZero && (
                        !db.SellOrderItemExtends.Any(ext => ext.Id == x.Item.Id && !ext.IsDeleted)
                        || db.SellOrderItemExtends.Any(ext =>
                            ext.Id == x.Item.Id && !ext.IsDeleted && ext.ReceiptProgressStatus == 0)))
                    || (nonZero.Count > 0 && db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id && !ext.IsDeleted && nonZero.Contains(ext.ReceiptProgressStatus))));
            }

            var invoiceStatuses = ProgressStatusFilterHelper.Normalize(request.InvoiceProgressStatus);
            if (invoiceStatuses.Count > 0)
            {
                var includeZero = invoiceStatuses.Contains((short)0);
                var nonZero = invoiceStatuses.Where(s => s != 0).ToList();
                q = q.Where(x =>
                    (includeZero && (
                        !db.SellOrderItemExtends.Any(ext => ext.Id == x.Item.Id && !ext.IsDeleted)
                        || db.SellOrderItemExtends.Any(ext =>
                            ext.Id == x.Item.Id && !ext.IsDeleted && ext.InvoiceProgressStatus == 0)))
                    || (nonZero.Count > 0 && db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id && !ext.IsDeleted && nonZero.Contains(ext.InvoiceProgressStatus))));
            }

            var notifyStatuses = ProgressStatusFilterHelper.Normalize(request.StockOutNotifyProgressStatus);
            if (notifyStatuses.Count > 0)
            {
                var want0 = notifyStatuses.Contains((short)0);
                var want1 = notifyStatuses.Contains((short)1);
                var want2 = notifyStatuses.Contains((short)2);
                q = q.Where(x =>
                    (want0 && (
                        !db.SellOrderItemExtends.Any(ext => ext.Id == x.Item.Id && !ext.IsDeleted)
                        || db.SellOrderItemExtends.Any(ext =>
                            ext.Id == x.Item.Id && !ext.IsDeleted && ext.QtyStockOutNotify <= 0m)))
                    || (want2 && db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id
                        && !ext.IsDeleted
                        && ext.QtyStockOutNotify > 0m
                        && ext.QtyStockOutNotify + 0.0000000001m >= x.Item.Qty))
                    || (want1 && db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id
                        && !ext.IsDeleted
                        && ext.QtyStockOutNotify > 0m
                        && ext.QtyStockOutNotify + 0.0000000001m < x.Item.Qty)));
            }

            if (request.StockOutPending)
            {
                q = q.Where(x =>
                    x.Item.Status == 0
                    && x.So.Status != SellOrderMainStatus.Cancelled
                    && x.So.Status != SellOrderMainStatus.AuditFailed
                    && db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id
                        && !ext.IsDeleted
                        && (ext.StockOutProgressStatus == 0 || ext.StockOutProgressStatus == 1)));
            }

            if (request.InvoicePending)
            {
                q = q.Where(x =>
                    x.Item.Status == 0
                    && x.So.Status != SellOrderMainStatus.Cancelled
                    && x.So.Status != SellOrderMainStatus.AuditFailed
                    && db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id
                        && !ext.IsDeleted
                        && (ext.InvoiceProgressStatus < 2 || ext.InvoiceAmountNot > 0)));
            }
        }
        else if (hasQuickFilter)
        {
            q = ApplyQuickFilter(db, q, request.QuickFilter);
        }

        if (!string.IsNullOrWhiteSpace(request.TransactionCurrency))
        {
            var kind = request.TransactionCurrency.Trim().ToLowerInvariant();
            if (kind is "rmb" or "cny" or "人民币")
                q = q.Where(x => x.Item.Currency == (short)CurrencyCode.RMB);
            else if (kind is "foreign" or "外币")
                q = q.Where(x => x.Item.Currency != (short)CurrencyCode.RMB);
        }

        if (!string.IsNullOrWhiteSpace(request.SalesUserId))
        {
            var uid = request.SalesUserId.Trim();
            q = q.Where(x => x.So.SalesUserId == uid);
        }

        if (!string.IsNullOrWhiteSpace(request.PurchaseUserAccount))
        {
            var keyword = request.PurchaseUserAccount.Trim().ToLower();
            q = q.Where(x =>
                db.PurchaseOrderItems.Any(poi =>
                    poi.SellOrderItemId == x.Item.Id
                    && poi.Status != -1
                    && poi.Status != -2
                    && db.PurchaseOrders.Any(po =>
                        po.Id == poi.PurchaseOrderId
                        && po.Status != -2
                        && (
                            (po.PurchaseUserName != null && po.PurchaseUserName.ToLower().Contains(keyword))
                            || (po.PurchaseUserId != null && db.Users.Any(u =>
                                u.Id == po.PurchaseUserId
                                && (
                                    u.UserName.ToLower().Contains(keyword)
                                    || (u.RealName != null && u.RealName.ToLower().Contains(keyword))
                                )))
                        ))));
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerId))
        {
            var cid = request.CustomerId.Trim();
            q = q.Where(x => x.So.CustomerId == cid);
        }

        // 报表成单透镜：部门 / 个人（公司层仅数据权限）
        if (SalesOrderItemAnalyticsDatasets.IsReportApproved(request.AnalyticsDataset))
        {
            q = ApplyReportViewLens(db, q, request);
        }

        return q;
    }

    private static IQueryable<SellOrderItemLineJoin> ApplyReportViewLens(
        ApplicationDbContext db,
        IQueryable<SellOrderItemLineJoin> q,
        SellOrderItemLineQueryRequest request)
    {
        var viewLevel = (request.AnalyticsViewLevel ?? string.Empty).Trim().ToLowerInvariant();
        if (viewLevel == SalesAnalyticsViewLevels.Personal
            && !string.IsNullOrWhiteSpace(request.SalesUserId))
        {
            var uid = request.SalesUserId.Trim();
            return q.Where(x => x.So.SalesUserId == uid);
        }

        if (viewLevel == SalesAnalyticsViewLevels.Department)
        {
            var deptId = request.AnalyticsDepartmentId?.Trim();
            if (string.IsNullOrWhiteSpace(deptId))
                return q;

            if (string.Equals(deptId, SalesAnalyticsScopeValidator.UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase))
            {
                var withPrimary = db.RbacUserDepartments.AsNoTracking()
                    .Where(ud => ud.IsPrimary)
                    .Select(ud => ud.UserId);
                return q.Where(x =>
                    x.So.SalesUserId == null
                    || !withPrimary.Contains(x.So.SalesUserId));
            }

            var userIdsInDept = db.RbacUserDepartments.AsNoTracking()
                .Where(ud => ud.IsPrimary && ud.DepartmentId == deptId)
                .Select(ud => ud.UserId);
            return q.Where(x => x.So.SalesUserId != null && userIdsInDept.Contains(x.So.SalesUserId));
        }

        return q;
    }

    public static IQueryable<SellOrderItemLineJoin> ApplyApprovedFilter(IQueryable<SellOrderItemLineJoin> q) =>
        q.Where(x =>
            (short)x.So.Status >= (short)SellOrderMainStatus.Approved
            && x.Item.Status == 0
            && !x.Item.IsDeleted);
}
