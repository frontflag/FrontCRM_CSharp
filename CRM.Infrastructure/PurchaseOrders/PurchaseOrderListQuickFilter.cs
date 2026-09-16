using CRM.Core.Constants;
using CRM.Core.Models.Purchase;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.PurchaseOrders;

/// <summary>
/// 采购订单主表左栏 <c>quickFilter</c>：码与明细列表相同；行级项为「至少一行命中」EXISTS。
/// </summary>
internal static class PurchaseOrderListQuickFilter
{
    public static IQueryable<PurchaseOrder> Apply(
        ApplicationDbContext db,
        IQueryable<PurchaseOrder> q,
        string? quickFilter)
    {
        if (string.IsNullOrWhiteSpace(quickFilter)
            || !PurchaseOrderItemListQuickFilterCodes.IsKnown(quickFilter))
            return q;

        var code = quickFilter.Trim();
        return code switch
        {
            PurchaseOrderItemListQuickFilterCodes.PendingSubmitAudit =>
                q.Where(o => o.Status == 1 || o.Status == 2 || o.Status == -1),
            PurchaseOrderItemListQuickFilterCodes.PendingVendorConfirm =>
                q.Where(o => o.Status == 20),
            PurchaseOrderItemListQuickFilterCodes.PayLater =>
                q.Where(o => o.IsPayLater),
            PurchaseOrderItemListQuickFilterCodes.HasPurchaseOrderDocs => ApplyDocs(db, q, hasDocs: true),
            PurchaseOrderItemListQuickFilterCodes.NoPurchaseOrderDocs => ApplyDocs(db, q, hasDocs: false),
            _ => ApplyMatchingLineExists(db, q, code)
        };
    }

    private static IQueryable<PurchaseOrder> ApplyDocs(
        ApplicationDbContext db,
        IQueryable<PurchaseOrder> q,
        bool hasDocs)
    {
        if (hasDocs)
        {
            return q.Where(o => db.UploadDocuments.Any(d =>
                !d.IsDeleted
                && d.BizType == CrossSideDocumentAttachmentPolicy.BizPurchaseOrder
                && d.BizId == o.Id));
        }

        return q.Where(o => !db.UploadDocuments.Any(d =>
            !d.IsDeleted
            && d.BizType == CrossSideDocumentAttachmentPolicy.BizPurchaseOrder
            && d.BizId == o.Id));
    }

    private static IQueryable<PurchaseOrder> ApplyMatchingLineExists(
        ApplicationDbContext db,
        IQueryable<PurchaseOrder> q,
        string quickFilter)
    {
        var lines =
            from item in db.PurchaseOrderItems.AsNoTracking()
            join po in db.PurchaseOrders.AsNoTracking() on item.PurchaseOrderId equals po.Id
            join ext in db.PurchaseOrderItemExtends.AsNoTracking().Where(e => !e.IsDeleted)
                on item.Id equals ext.Id into extGroup
            from ext in extGroup.DefaultIfEmpty()
            select new PurchaseOrderItemLineJoin { Item = item, Po = po, Ext = ext };
        var ids = PurchaseOrderItemListFilter.ApplyQuickFilter(db, lines, quickFilter)
            .Select(x => x.Po.Id);
        return q.Where(o => ids.Contains(o.Id));
    }
}
