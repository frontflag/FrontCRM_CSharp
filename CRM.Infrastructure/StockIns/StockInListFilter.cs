using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.StockIns;

/// <summary>入库单列表筛选（分页与看板共用）。</summary>
internal static class StockInListFilter
{
    public static async Task<IQueryable<StockIn>> BuildFilteredQueryAsync(
        ApplicationDbContext db,
        IDataPermissionService dataPermission,
        StockInQueryRequest? request,
        CancellationToken cancellationToken)
    {
        var q = db.StockIns.AsNoTracking()
            .Where(s => s.StockInType != StockInTypeCode.Transfer);

        if (request != null)
        {
            if (!string.IsNullOrWhiteSpace(request.StockInCode))
            {
                var k = request.StockInCode.Trim().ToLowerInvariant();
                q = q.Where(s => s.StockInCode.ToLower().Contains(k));
            }

            if (!string.IsNullOrWhiteSpace(request.WarehouseId))
            {
                var wid = request.WarehouseId.Trim();
                q = q.Where(s => s.WarehouseId == wid);
            }

            if (!string.IsNullOrWhiteSpace(request.Remark))
            {
                var k = request.Remark.Trim().ToLowerInvariant();
                q = q.Where(s => s.Remark != null && s.Remark.ToLower().Contains(k));
            }

            if (request.StockInDateStart.HasValue)
            {
                var d = SalesAnalyticsDateFilter.ToUtcDateStart(request.StockInDateStart.Value);
                q = q.Where(s => s.StockInDate >= d);
            }

            if (request.StockInDateEnd.HasValue)
            {
                var endEx = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(request.StockInDateEnd.Value);
                q = q.Where(s => s.StockInDate < endEx);
            }

            if (request.Status.HasValue)
            {
                var status = request.Status.Value;
                q = q.Where(s => s.Status == status);
            }

            if (request.ItemCurrency.HasValue)
            {
                var currency = request.ItemCurrency.Value;
                q = q.Where(s =>
                    db.StockInItems.Any(i =>
                        i.StockInId == s.Id &&
                        !i.IsDeleted &&
                        db.StockItems.Any(st =>
                            !st.IsDeleted &&
                            st.StockInItemId == i.Id &&
                            (i.Currency ?? st.PurchaseCurrency) == currency)));
            }

            if (request.StockInType.HasValue)
            {
                var rawType = request.StockInType.Value;
                if (StockInTypeCode.IsPurchaseReceipt(rawType)
                    || StockInTypeCode.NormalizeForNotify(rawType) == StockInTypeCode.Purchase)
                {
                    q = q.Where(s =>
                        s.StockInType == StockInTypeCode.Purchase
                        || s.StockInType == StockInTypeCode.LegacyPurchase);
                }
                else
                {
                    var stockInType = StockInTypeCode.NormalizeForNotify(rawType);
                    q = q.Where(s => s.StockInType == stockInType);
                }
            }

            if (!string.IsNullOrWhiteSpace(request.VendorName))
            {
                var k = request.VendorName.Trim().ToLowerInvariant();
                q = q.Where(s =>
                    s.VendorId != null &&
                    db.Vendors.Any(v =>
                        v.Id == s.VendorId &&
                        ((v.OfficialName != null && v.OfficialName.ToLower().Contains(k)) ||
                         (v.NickName != null && v.NickName.ToLower().Contains(k)) ||
                         (v.Code != null && v.Code.ToLower().Contains(k)))));
            }

            if (!string.IsNullOrWhiteSpace(request.Model))
            {
                var k = request.Model.Trim().ToLowerInvariant();
                q = q.Where(s => db.StockInItems.Any(i =>
                    i.StockInId == s.Id &&
                    (i.MaterialId.ToLower().Contains(k) ||
                     (i.PurchasePn != null && i.PurchasePn.ToLower().Contains(k)) ||
                     (i.PurchaseBrand != null && i.PurchaseBrand.ToLower().Contains(k)) ||
                     (i.Remark != null && i.Remark.ToLower().Contains(k)))));
            }

            if (!string.IsNullOrWhiteSpace(request.PurchaseOrderCode))
            {
                var k = request.PurchaseOrderCode.Trim().ToLowerInvariant();
                q = q.Where(s =>
                    (s.SourceCode != null && s.SourceCode.ToLower().Contains(k)) ||
                    db.StockInItemExtends.Any(e =>
                        e.StockInId == s.Id &&
                        e.PurchaseOrderItemCode != null &&
                        e.PurchaseOrderItemCode.ToLower().Contains(k)) ||
                    db.StockInItemExtends.Any(e =>
                        e.StockInId == s.Id &&
                        e.PurchaseOrderItemId != null &&
                        db.PurchaseOrderItems.Any(poi =>
                            poi.Id == e.PurchaseOrderItemId &&
                            db.PurchaseOrders.Any(po =>
                                po.Id == poi.PurchaseOrderId &&
                                po.PurchaseOrderCode.ToLower().Contains(k)))));
            }

            if (!string.IsNullOrWhiteSpace(request.FreightForwarderOrderNo))
            {
                var k = request.FreightForwarderOrderNo.Trim().ToLowerInvariant();
                q = q.Where(s =>
                    db.StockInItemExtends.Any(e =>
                        e.StockInId == s.Id &&
                        e.PurchaseOrderItemId != null &&
                        db.PurchaseOrderItems.Any(poi =>
                            poi.Id == e.PurchaseOrderItemId &&
                            db.PurchaseOrders.Any(po =>
                                po.Id == poi.PurchaseOrderId &&
                                po.FreightForwarderOrderNo != null &&
                                po.FreightForwarderOrderNo.ToLower().Contains(k)))));
            }

            if (!string.IsNullOrWhiteSpace(request.SourceDisplayNo))
            {
                var k = request.SourceDisplayNo.Trim().ToLowerInvariant();
                q = q.Where(s =>
                    (s.SourceCode != null && s.SourceCode.ToLower().Contains(k)) ||
                    (s.QcCode != null && s.QcCode.ToLower().Contains(k)) ||
                    db.StockInItemExtends.Any(e =>
                        e.StockInId == s.Id &&
                        e.PurchaseOrderItemCode != null &&
                        e.PurchaseOrderItemCode.ToLower().Contains(k)) ||
                    db.StockInItemExtends.Any(e =>
                        e.StockInId == s.Id &&
                        e.PurchaseOrderItemId != null &&
                        db.PurchaseOrderItems.Any(poi =>
                            poi.Id == e.PurchaseOrderItemId &&
                            db.PurchaseOrders.Any(po =>
                                po.Id == poi.PurchaseOrderId &&
                                po.PurchaseOrderCode.ToLower().Contains(k)))));
            }

            if (!string.IsNullOrWhiteSpace(request.SalesOrderCode))
            {
                var k = request.SalesOrderCode.Trim().ToLowerInvariant();
                q = q.Where(s =>
                    db.StockInItemExtends.Any(e =>
                        e.StockInId == s.Id &&
                        e.PurchaseOrderItemId != null &&
                        db.PurchaseOrderItems.Any(poi =>
                            poi.Id == e.PurchaseOrderItemId &&
                            poi.PurchaseOrderId != null &&
                            db.PurchaseOrderItems.Any(poi2 =>
                                poi2.PurchaseOrderId == poi.PurchaseOrderId &&
                                poi2.SellOrderItemId != null &&
                                db.SellOrderItems.Any(soi =>
                                    soi.Id == poi2.SellOrderItemId &&
                                    db.SellOrders.Any(so =>
                                        so.Id == soi.SellOrderId &&
                                        so.SellOrderCode != null &&
                                        so.SellOrderCode.ToLower().Contains(k)))))));
            }
        }

        return await dataPermission.ApplyStockInListDataScopeAsync(
            request?.CurrentUserId,
            q,
            db.SellOrders.AsNoTracking(),
            db.SellOrderItems.AsNoTracking(),
            db.StockInItemExtends.AsNoTracking(),
            db.PurchaseOrderItems.AsNoTracking(),
            db.PurchaseOrders.AsNoTracking(),
            cancellationToken);
    }
}
