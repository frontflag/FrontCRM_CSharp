using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.PurchaseOrders;

/// <summary>
/// 采购执行链路列表：经 <see cref="PurchaseOrder"/> 套用 <see cref="IDataPermissionService.ApplyPurchaseOrderDataScopeAsync"/>。
/// </summary>
public static class PurchaseOrderDataScopeQueryHelper
{
    public static async Task<IQueryable<PurchaseOrder>> GetScopedPurchaseOrdersAsync(
        IDataPermissionService dataPermission,
        ApplicationDbContext db,
        string? currentUserId,
        CancellationToken cancellationToken = default)
    {
        var q = db.PurchaseOrders.AsNoTracking();
        return await dataPermission.ApplyPurchaseOrderDataScopeAsync(currentUserId, q, cancellationToken);
    }

    public static async Task<IQueryable<StockInNotify>> FilterArrivalNoticesAsync(
        IDataPermissionService dataPermission,
        ApplicationDbContext db,
        string? currentUserId,
        IQueryable<StockInNotify> query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currentUserId))
            return query;

        var scopedPo = await GetScopedPurchaseOrdersAsync(dataPermission, db, currentUserId, cancellationToken);
        return query.Where(n =>
            n.PurchaseOrderId != null
            && scopedPo.Any(po => po.Id == n.PurchaseOrderId));
    }

    public static async Task<IQueryable<QCInfo>> FilterQcInfosAsync(
        IDataPermissionService dataPermission,
        ApplicationDbContext db,
        string? currentUserId,
        IQueryable<QCInfo> query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currentUserId))
            return query;

        var scopedPo = await GetScopedPurchaseOrdersAsync(dataPermission, db, currentUserId, cancellationToken);
        return query.Where(q =>
            db.StockInNotifies.Any(n =>
                n.Id == q.StockInNotifyId
                && !n.IsDeleted
                && n.PurchaseOrderId != null
                && scopedPo.Any(po => po.Id == n.PurchaseOrderId)));
    }

    public static async Task<IQueryable<StockInBatch>> FilterStockInBatchesAsync(
        IDataPermissionService dataPermission,
        ApplicationDbContext db,
        string? currentUserId,
        IQueryable<StockInBatch> query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currentUserId))
            return query;

        var scopedPo = await GetScopedPurchaseOrdersAsync(dataPermission, db, currentUserId, cancellationToken);
        return query.Where(ib =>
            db.StockInItems.Any(si =>
                si.Id == ib.StockInItemId
                && db.StockInItemExtends.Any(ext =>
                    ext.Id == si.Id
                    && ext.PurchaseOrderItemId != null
                    && db.PurchaseOrderItems.Any(poi =>
                        poi.Id == ext.PurchaseOrderItemId
                        && poi.PurchaseOrderId != null
                        && scopedPo.Any(po => po.Id == poi.PurchaseOrderId)))));
    }
}
