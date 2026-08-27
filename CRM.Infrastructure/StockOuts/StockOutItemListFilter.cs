using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.StockOuts;

/// <summary>出库明细列表筛选（分页与看板共用）。</summary>
internal static class StockOutItemListFilter
{
    public static async Task<IQueryable<StockOutItemListJoin>> BuildFilteredJoinQueryAsync(
        ApplicationDbContext db,
        IDataPermissionService dataPermission,
        StockOutItemListQuery query,
        CancellationToken cancellationToken)
    {
        var scopedStockOuts = await dataPermission.ApplyStockOutListDataScopeAsync(
            query.CurrentUserId,
            db.StockOuts.AsNoTracking(),
            db.SellOrders.AsNoTracking(),
            db.SellOrderItems.AsNoTracking(),
            db.Customers.AsNoTracking(),
            cancellationToken);

        var q =
            from si in db.StockOutItems.AsNoTracking()
            join so in scopedStockOuts on si.StockOutId equals so.Id
            join sol in db.SellOrderItems.AsNoTracking() on so.SellOrderItemId equals sol.Id into solg
            from sol in solg.DefaultIfEmpty()
            join ord in db.SellOrders.AsNoTracking() on sol.SellOrderId equals ord.Id into ordg
            from ord in ordg.DefaultIfEmpty()
            join hdrCust in db.Customers.AsNoTracking() on so.CustomerId equals hdrCust.Id into hc
            from hdrCust in hc.DefaultIfEmpty()
            join u in db.Users.AsNoTracking() on ord.SalesUserId equals u.Id into ug
            from u in ug.DefaultIfEmpty()
            select new StockOutItemListJoin
            {
                Item = si,
                Header = so,
                SoLine = sol,
                Order = ord,
                HeaderCustomer = hdrCust,
                SalesUser = u
            };

        if (query.Status.HasValue)
        {
            var st = query.Status.Value;
            q = q.Where(x => x.Header.Status == st);
        }

        if (query.StockOutType.HasValue)
        {
            var rawType = query.StockOutType.Value;
            if (StockOutTypeCode.IsSalesStockOut(rawType)
                || StockOutTypeCode.NormalizeForNotify(rawType) == StockOutTypeCode.Sales)
            {
                q = q.Where(x =>
                    x.Header.StockOutType == StockOutTypeCode.Sales
                    || x.Header.StockOutType == StockOutTypeCode.LegacySales);
            }
            else
            {
                var stockOutType = StockOutTypeCode.NormalizeForNotify(rawType);
                q = q.Where(x => x.Header.StockOutType == stockOutType);
            }
        }

        if (!string.IsNullOrWhiteSpace(query.StockOutCode))
        {
            var k = query.StockOutCode.Trim().ToLowerInvariant();
            q = q.Where(x => x.Header.StockOutCode.ToLower().Contains(k));
        }

        if (!string.IsNullOrWhiteSpace(query.StockOutItemCode))
        {
            var k = query.StockOutItemCode.Trim().ToLowerInvariant();
            q = q.Where(x =>
                x.Item.StockOutItemCode != null &&
                x.Item.StockOutItemCode.ToLower().Contains(k));
        }

        if (query.StockOutDateFrom.HasValue)
        {
            var d = SalesAnalyticsDateFilter.ToUtcDateStart(query.StockOutDateFrom.Value);
            q = q.Where(x => x.Header.StockOutDate != null && x.Header.StockOutDate >= d);
        }

        if (query.StockOutDateTo.HasValue)
        {
            var endEx = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(query.StockOutDateTo.Value);
            q = q.Where(x => x.Header.StockOutDate != null && x.Header.StockOutDate < endEx);
        }

        if (!string.IsNullOrWhiteSpace(query.CustomerName))
        {
            var k = query.CustomerName.Trim().ToLowerInvariant();
            q = q.Where(x =>
                (x.HeaderCustomer != null &&
                 x.HeaderCustomer.OfficialName != null &&
                 x.HeaderCustomer.OfficialName.ToLower().Contains(k)) ||
                (x.HeaderCustomer != null &&
                 x.HeaderCustomer.NickName != null &&
                 x.HeaderCustomer.NickName.ToLower().Contains(k)) ||
                (x.Order != null && x.Order.CustomerName != null && x.Order.CustomerName.ToLower().Contains(k)));
        }

        if (!string.IsNullOrWhiteSpace(query.SalesUserName))
        {
            var k = query.SalesUserName.Trim().ToLowerInvariant();
            q = q.Where(x =>
                (x.Order != null &&
                 x.Order.SalesUserName != null &&
                 x.Order.SalesUserName.ToLower().Contains(k)) ||
                (x.SalesUser != null && x.SalesUser.UserName != null && x.SalesUser.UserName.ToLower().Contains(k)));
        }

        if (!string.IsNullOrWhiteSpace(query.PurchasePn))
        {
            var k = query.PurchasePn.Trim().ToLowerInvariant();
            q = q.Where(x => x.Item.PurchasePn != null && x.Item.PurchasePn.ToLower().Contains(k));
        }

        if (!string.IsNullOrWhiteSpace(query.FreightForwarderOrderNo))
        {
            var k = query.FreightForwarderOrderNo.Trim().ToLowerInvariant();
            q = q.Where(x =>
                db.StockOutItemExtends.Any(ext =>
                    ext.Id == x.Item.Id
                    && ext.PurchaseOrderItemId != null
                    && db.PurchaseOrderItems.Any(poi =>
                        poi.Id == ext.PurchaseOrderItemId
                        && db.PurchaseOrders.Any(po =>
                            po.Id == poi.PurchaseOrderId
                            && po.FreightForwarderOrderNo != null
                            && po.FreightForwarderOrderNo.ToLower().Contains(k)))));
        }

        if (!string.IsNullOrWhiteSpace(query.SellOrderItemCode))
        {
            var k = query.SellOrderItemCode.Trim().ToLowerInvariant();
            q = q.Where(x =>
                (x.SoLine != null &&
                 x.SoLine.SellOrderItemCode != null &&
                 x.SoLine.SellOrderItemCode.ToLower().Contains(k))
                || db.StockOutItemExtends.Any(ext =>
                    ext.Id == x.Item.Id
                    && ext.SellOrderItemCode != null
                    && ext.SellOrderItemCode.ToLower().Contains(k)));
        }

        if (!string.IsNullOrWhiteSpace(query.StockInCode))
        {
            var k = query.StockInCode.Trim().ToLowerInvariant();
            q = q.Where(x =>
                db.StockOutItemExtends.Any(e =>
                    e.Id == x.Item.Id &&
                    e.StockInItemId != null &&
                    db.StockInItems.Any(sii =>
                        sii.Id == e.StockInItemId &&
                        db.StockIns.Any(sin =>
                            sin.Id == sii.StockInId &&
                            sin.StockInCode != null &&
                            sin.StockInCode.ToLower().Contains(k)))));
        }

        if (!string.IsNullOrWhiteSpace(query.PackingCode))
        {
            var k = query.PackingCode.Trim().ToLowerInvariant();
            q = q.Where(x =>
                (x.Item.PackingId != null
                 && db.Packings.Any(pk =>
                     !pk.IsDeleted
                     && pk.Id == x.Item.PackingId
                     && pk.Code.ToLower().Contains(k)))
                || (x.Item.PickingTaskItemId != null
                    && db.PickingTaskItems.Any(pti =>
                        !pti.IsDeleted
                        && pti.Id == x.Item.PickingTaskItemId
                        && db.PickingTasks.Any(pt =>
                            !pt.IsDeleted
                            && pt.Id == pti.PickingTaskId
                            && pt.PackingId != null
                            && db.Packings.Any(pk =>
                                !pk.IsDeleted
                                && pk.Id == pt.PackingId
                                && pk.Code.ToLower().Contains(k))))));
        }

        return q;
    }
}
