using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.SalesOrders;

/// <summary>销售订单明细列表筛选条件（列表分页与看板共用）。</summary>
internal static class SalesOrderItemLineListFilter
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

        if (request.PurchaseProgressStatus is >= 0 and <= 2)
        {
            var status = request.PurchaseProgressStatus.Value;
            q = status == 0
                ? q.Where(x =>
                    !db.SellOrderItemExtends.Any(ext => ext.Id == x.Item.Id && !ext.IsDeleted)
                    || db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id && !ext.IsDeleted && ext.PurchaseProgressStatus == 0))
                : q.Where(x =>
                    db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id && !ext.IsDeleted && ext.PurchaseProgressStatus == status));
        }

        if (request.StockInProgressStatus is >= 0 and <= 2)
        {
            var status = request.StockInProgressStatus.Value;
            q = status == 0
                ? q.Where(x =>
                    !db.SellOrderItemExtends.Any(ext => ext.Id == x.Item.Id && !ext.IsDeleted)
                    || db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id && !ext.IsDeleted && ext.StockInProgressStatus == 0))
                : q.Where(x =>
                    db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id && !ext.IsDeleted && ext.StockInProgressStatus == status));
        }

        if (request.StockOutProgressStatus is >= 0 and <= 2)
        {
            var status = request.StockOutProgressStatus.Value;
            q = status == 0
                ? q.Where(x =>
                    !db.SellOrderItemExtends.Any(ext => ext.Id == x.Item.Id && !ext.IsDeleted)
                    || db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id && !ext.IsDeleted && ext.StockOutProgressStatus == 0))
                : q.Where(x =>
                    db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id && !ext.IsDeleted && ext.StockOutProgressStatus == status));
        }

        if (request.ReceiptProgressStatus is >= 0 and <= 2)
        {
            var status = request.ReceiptProgressStatus.Value;
            q = status == 0
                ? q.Where(x =>
                    !db.SellOrderItemExtends.Any(ext => ext.Id == x.Item.Id && !ext.IsDeleted)
                    || db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id && !ext.IsDeleted && ext.ReceiptProgressStatus == 0))
                : q.Where(x =>
                    db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id && !ext.IsDeleted && ext.ReceiptProgressStatus == status));
        }

        if (request.InvoiceProgressStatus is >= 0 and <= 2)
        {
            var status = request.InvoiceProgressStatus.Value;
            q = status == 0
                ? q.Where(x =>
                    !db.SellOrderItemExtends.Any(ext => ext.Id == x.Item.Id && !ext.IsDeleted)
                    || db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id && !ext.IsDeleted && ext.InvoiceProgressStatus == 0))
                : q.Where(x =>
                    db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id && !ext.IsDeleted && ext.InvoiceProgressStatus == status));
        }

        if (request.StockOutNotifyProgressStatus is >= 0 and <= 2)
        {
            var notifyStatus = request.StockOutNotifyProgressStatus.Value;
            if (notifyStatus == 0)
            {
                q = q.Where(x =>
                    !db.SellOrderItemExtends.Any(ext => ext.Id == x.Item.Id && !ext.IsDeleted)
                    || db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id && !ext.IsDeleted && ext.QtyStockOutNotify <= 0m));
            }
            else if (notifyStatus == 2)
            {
                q = q.Where(x =>
                    db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id
                        && !ext.IsDeleted
                        && ext.QtyStockOutNotify > 0m
                        && ext.QtyStockOutNotify + 0.0000000001m >= x.Item.Qty));
            }
            else
            {
                q = q.Where(x =>
                    db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id
                        && !ext.IsDeleted
                        && ext.QtyStockOutNotify > 0m
                        && ext.QtyStockOutNotify + 0.0000000001m < x.Item.Qty));
            }
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

        return q;
    }

    public static IQueryable<SellOrderItemLineJoin> ApplyApprovedFilter(IQueryable<SellOrderItemLineJoin> q) =>
        q.Where(x =>
            (short)x.So.Status >= (short)SellOrderMainStatus.Approved
            && x.Item.Status == 0
            && !x.Item.IsDeleted);
}
