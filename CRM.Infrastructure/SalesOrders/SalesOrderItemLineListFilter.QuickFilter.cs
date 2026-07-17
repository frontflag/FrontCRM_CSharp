using CRM.Core.Constants;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.SalesOrders;

internal static partial class SalesOrderItemLineListFilter
{
    private const short PoItemCancelled = -1;
    private const short PoItemRemoved = -2;
    private const short PoMainCancelled = -2;
    private const short PrOpenStatus0 = 0;
    private const short PrOpenStatus1 = 1;

    public static IQueryable<SellOrderItemLineJoin> ApplyQuickFilter(
        ApplicationDbContext db,
        IQueryable<SellOrderItemLineJoin> q,
        string? quickFilter)
    {
        if (string.IsNullOrWhiteSpace(quickFilter)
            || !SellOrderItemListQuickFilterCodes.IsKnown(quickFilter))
            return q;

        return quickFilter.Trim() switch
        {
            SellOrderItemListQuickFilterCodes.PendingSubmitAudit => ApplyPendingSubmitAudit(q),
            SellOrderItemListQuickFilterCodes.PendingSubmitPurchaseReq => ApplyPendingSubmitPurchaseReq(db, q),
            SellOrderItemListQuickFilterCodes.PendingSubmitStockOutNotify => ApplyPendingSubmitStockOutNotify(db, q),
            SellOrderItemListQuickFilterCodes.AppliedPendingPo => ApplyAppliedPendingPo(db, q),
            SellOrderItemListQuickFilterCodes.PurchasedPendingStockIn => ApplyPurchasedPendingStockIn(db, q),
            SellOrderItemListQuickFilterCodes.NotifyPendingPacking => ApplyNotifyPendingPacking(db, q),
            SellOrderItemListQuickFilterCodes.PackedPendingStockOut => ApplyPackedPendingStockOut(db, q),
            SellOrderItemListQuickFilterCodes.InStockPendingOut => ApplyInStockPendingOut(db, q),
            SellOrderItemListQuickFilterCodes.UsedStocking => ApplyUsedStocking(db, q),
            SellOrderItemListQuickFilterCodes.StockOutPendingReceipt => ApplyStockOutPendingReceipt(db, q),
            SellOrderItemListQuickFilterCodes.ReceiptPartial => ApplyReceiptProgress(db, q, 1),
            SellOrderItemListQuickFilterCodes.ReceiptComplete => ApplyReceiptProgress(db, q, 2),
            _ => q
        };
    }

    private static IQueryable<SellOrderItemLineJoin> ApplyActiveLineFilter(IQueryable<SellOrderItemLineJoin> q) =>
        q.Where(x => x.Item.Status == 0 && !x.Item.IsDeleted);

    private static IQueryable<SellOrderItemLineJoin> ApplyApprovedMainOrderFilter(IQueryable<SellOrderItemLineJoin> q) =>
        q.Where(x =>
            x.So.Status == SellOrderMainStatus.Approved
            || x.So.Status == SellOrderMainStatus.InProgress
            || x.So.Status == SellOrderMainStatus.Completed);

    private static IQueryable<SellOrderItemLineJoin> ApplyPendingSubmitAudit(IQueryable<SellOrderItemLineJoin> q) =>
        ApplyActiveLineFilter(q).Where(x =>
            x.So.Status == SellOrderMainStatus.New
            || x.So.Status == SellOrderMainStatus.AuditFailed);

    private static IQueryable<SellOrderItemLineJoin> ApplyPendingSubmitPurchaseReq(
        ApplicationDbContext db,
        IQueryable<SellOrderItemLineJoin> q)
    {
        q = ApplyActiveLineFilter(ApplyApprovedMainOrderFilter(q));
        return q.Where(x =>
            x.Item.Qty
            - (db.PurchaseOrderItems
                .Where(poi => poi.SellOrderItemId == x.Item.Id
                    && poi.Status != PoItemCancelled
                    && poi.Status != PoItemRemoved
                    && db.PurchaseOrders.Any(po =>
                        po.Id == poi.PurchaseOrderId && po.Status != PoMainCancelled))
                .Select(poi => (decimal?)poi.Qty)
                .Sum() ?? 0m)
            - (db.PurchaseRequisitions
                .Where(pr => pr.SellOrderItemId == x.Item.Id
                    && (pr.Status == PrOpenStatus0 || pr.Status == PrOpenStatus1))
                .Select(pr => (decimal?)pr.Qty)
                .Sum() ?? 0m)
            > 0m);
    }

    private static IQueryable<SellOrderItemLineJoin> ApplyAppliedPendingPo(
        ApplicationDbContext db,
        IQueryable<SellOrderItemLineJoin> q)
    {
        q = ApplyActiveLineFilter(q);
        return q.Where(x =>
            db.PurchaseRequisitions.Any(pr =>
                pr.SellOrderItemId == x.Item.Id
                && (pr.Status == PrOpenStatus0 || pr.Status == PrOpenStatus1)
                && pr.Qty > 0m)
            && !db.PurchaseOrderItems.Any(poi =>
                poi.SellOrderItemId == x.Item.Id
                && poi.Status != PoItemCancelled
                && poi.Status != PoItemRemoved
                && poi.Qty > 0m
                && db.PurchaseOrders.Any(po =>
                    po.Id == poi.PurchaseOrderId && po.Status != PoMainCancelled)));
    }

    private static IQueryable<SellOrderItemLineJoin> ApplyPurchasedPendingStockIn(
        ApplicationDbContext db,
        IQueryable<SellOrderItemLineJoin> q)
    {
        q = ApplyActiveLineFilter(ApplyApprovedMainOrderFilter(q));
        return q.Where(x =>
            db.PurchaseOrderItems.Any(poi =>
                poi.SellOrderItemId == x.Item.Id
                && poi.Status != PoItemCancelled
                && poi.Status != PoItemRemoved
                && poi.Qty > 0m
                && db.PurchaseOrders.Any(po =>
                    po.Id == poi.PurchaseOrderId && po.Status != PoMainCancelled))
            && db.SellOrderItemExtends.Any(ext =>
                ext.Id == x.Item.Id
                && !ext.IsDeleted
                && ext.PurchaseProgressStatus >= 2
                && ext.StockInProgressStatus < 2
                && (
                    ext.StockInProgressStatus == 1
                    || ext.QtyStockOutNotify + 0.0000000001m < x.Item.Qty)));
    }

    private static IQueryable<SellOrderItemLineJoin> ApplyNotifyPendingPacking(
        ApplicationDbContext db,
        IQueryable<SellOrderItemLineJoin> q) =>
        ApplyActiveLineFilter(q).Where(x =>
            db.StockOutRequests.Any(n =>
                n.SalesOrderItemId == x.Item.Id
                && !n.IsDeleted
                && n.Status == StockOutRequestStatusCode.PendingPacking));

    private static IQueryable<SellOrderItemLineJoin> ApplyPackedPendingStockOut(
        ApplicationDbContext db,
        IQueryable<SellOrderItemLineJoin> q) =>
        ApplyActiveLineFilter(q).Where(x =>
            db.StockOutRequests.Any(n =>
                n.SalesOrderItemId == x.Item.Id
                && !n.IsDeleted
                && n.Status == StockOutRequestStatusCode.Packed)
            && (
                !db.SellOrderItemExtends.Any(ext => ext.Id == x.Item.Id && !ext.IsDeleted)
                || db.SellOrderItemExtends.Any(ext =>
                    ext.Id == x.Item.Id
                    && !ext.IsDeleted
                    && ext.StockOutProgressStatus < 2)));

    /// <summary>在库待出：同一扩展行入库进度∈{1,2} 且出库进度&lt;2。</summary>
    private static IQueryable<SellOrderItemLineJoin> ApplyInStockPendingOut(
        ApplicationDbContext db,
        IQueryable<SellOrderItemLineJoin> q)
    {
        q = ApplyActiveLineFilter(ApplyApprovedMainOrderFilter(q));
        return q.Where(x =>
            db.SellOrderItemExtends.Any(ext =>
                ext.Id == x.Item.Id
                && !ext.IsDeleted
                && ext.StockInProgressStatus >= 1
                && ext.StockOutProgressStatus < 2));
    }

    /// <summary>
    /// 使用备货：与操作面板 stockingUsage 存在性同口径
    /// （packing_item → pickingtaskitem IsStockingSupplement 且 PickedQty&gt;0）。
    /// </summary>
    private static IQueryable<SellOrderItemLineJoin> ApplyUsedStocking(
        ApplicationDbContext db,
        IQueryable<SellOrderItemLineJoin> q)
    {
        q = ApplyActiveLineFilter(ApplyApprovedMainOrderFilter(q));
        return q.Where(x =>
            db.PickingTaskItems.Any(pti =>
                !pti.IsDeleted
                && pti.IsStockingSupplement
                && pti.PickedQty > 0
                && pti.PackingItemId != null
                && db.PickingTasks.Any(pt =>
                    pt.Id == pti.PickingTaskId && !pt.IsDeleted)
                && db.PackingItems.Any(pi =>
                    pi.Id == pti.PackingItemId
                    && !pi.IsDeleted
                    && (
                        pi.SellOrderItemId == x.Item.Id
                        || (pi.StockOutNotifyId != null
                            && db.StockOutRequests.Any(n =>
                                n.Id == pi.StockOutNotifyId
                                && !n.IsDeleted
                                && n.SalesOrderItemId == x.Item.Id))))));
    }

    private static IQueryable<SellOrderItemLineJoin> ApplyStockOutPendingReceipt(
        ApplicationDbContext db,
        IQueryable<SellOrderItemLineJoin> q) =>
        ApplyActiveLineFilter(q).Where(x =>
            db.SellOrderItemExtends.Any(ext =>
                ext.Id == x.Item.Id
                && !ext.IsDeleted
                && (ext.StockOutProgressStatus == 1 || ext.StockOutProgressStatus == 2)
                && (ext.ReceiptProgressStatus == 0 || ext.ReceiptAmountNot > 0m)));

    private static IQueryable<SellOrderItemLineJoin> ApplyReceiptProgress(
        ApplicationDbContext db,
        IQueryable<SellOrderItemLineJoin> q,
        short status) =>
        ApplyActiveLineFilter(q).Where(x =>
            db.SellOrderItemExtends.Any(ext =>
                ext.Id == x.Item.Id && !ext.IsDeleted && ext.ReceiptProgressStatus == status));

    private static IQueryable<SellOrderItemLineJoin> ApplyPendingSubmitStockOutNotify(
        ApplicationDbContext db,
        IQueryable<SellOrderItemLineJoin> q)
    {
        var minPoStatus = PurchaseOrderMainStatusCodes.VendorConfirmedOrBeyond;
        q = ApplyActiveLineFilter(ApplyApprovedMainOrderFilter(q));

        q = q.Where(x =>
            !db.SellOrderItemExtends.Any(ext => ext.Id == x.Item.Id && !ext.IsDeleted)
            || db.SellOrderItemExtends.Any(ext =>
                ext.Id == x.Item.Id && !ext.IsDeleted && ext.QtyStockOutNotifyNot > 0m));

        return q.Where(x =>
            (
                !db.SellOrderItemExtends.Any(ext => ext.Id == x.Item.Id && !ext.IsDeleted)
                || db.SellOrderItemExtends.Any(ext =>
                    ext.Id == x.Item.Id && !ext.IsDeleted && ext.StockOutProgressStatus != 2)
            )
            && (
                db.SellOrderItemExtends.Any(ext =>
                    ext.Id == x.Item.Id && !ext.IsDeleted && ext.PurchasedStock_AvailableQty > 0)
                || (
                    db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.Item.Id && !ext.IsDeleted && ext.PurchaseProgressStatus != 0)
                    && db.PurchaseOrderItems.Any(poi =>
                        poi.SellOrderItemId == x.Item.Id
                        && poi.Status != PoItemCancelled
                        && poi.Status != PoItemRemoved
                        && db.PurchaseOrders.Any(po =>
                            po.Id == poi.PurchaseOrderId && po.Status != PoMainCancelled))
                    && !db.PurchaseOrderItems.Any(poi =>
                        poi.SellOrderItemId == x.Item.Id
                        && poi.Status != PoItemCancelled
                        && poi.Status != PoItemRemoved
                        && (
                            !db.PurchaseOrders.Any(po =>
                                po.Id == poi.PurchaseOrderId && po.Status != PoMainCancelled)
                            || db.PurchaseOrders.Any(po =>
                                po.Id == poi.PurchaseOrderId
                                && po.Status != PoMainCancelled
                                && po.Status < minPoStatus)))
                )
            ));
    }
}
