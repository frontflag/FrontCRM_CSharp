using CRM.Core.Constants;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.SalesOrders;

/// <summary>
/// 销售订单主表左栏 <c>quickFilter</c>：码与明细列表相同；行级项为「至少一行命中」EXISTS。
/// </summary>
internal static class SalesOrderListQuickFilter
{
    public static IQueryable<SellOrder> Apply(
        ApplicationDbContext db,
        IQueryable<SellOrder> q,
        string? quickFilter)
    {
        if (string.IsNullOrWhiteSpace(quickFilter)
            || !SellOrderItemListQuickFilterCodes.IsKnown(quickFilter))
            return q;

        var code = quickFilter.Trim();
        return code switch
        {
            SellOrderItemListQuickFilterCodes.PendingSubmitAudit =>
                q.Where(o =>
                    o.Status == SellOrderMainStatus.New
                    || o.Status == SellOrderMainStatus.AuditFailed),
            SellOrderItemListQuickFilterCodes.HasSalesOrderDocs => ApplyDocs(db, q, hasDocs: true),
            SellOrderItemListQuickFilterCodes.NoSalesOrderDocs => ApplyDocs(db, q, hasDocs: false),
            _ => ApplyMatchingLineExists(db, q, code)
        };
    }

    private static IQueryable<SellOrder> ApplyDocs(
        ApplicationDbContext db,
        IQueryable<SellOrder> q,
        bool hasDocs)
    {
        if (hasDocs)
        {
            return q.Where(o => db.UploadDocuments.Any(d =>
                !d.IsDeleted
                && d.BizType == CrossSideDocumentAttachmentPolicy.BizSalesOrder
                && d.BizId == o.Id));
        }

        return q.Where(o => !db.UploadDocuments.Any(d =>
            !d.IsDeleted
            && d.BizType == CrossSideDocumentAttachmentPolicy.BizSalesOrder
            && d.BizId == o.Id));
    }

    private static IQueryable<SellOrder> ApplyMatchingLineExists(
        ApplicationDbContext db,
        IQueryable<SellOrder> q,
        string quickFilter)
    {
        var lines =
            from item in db.SellOrderItems.AsNoTracking()
            join so in db.SellOrders.AsNoTracking() on item.SellOrderId equals so.Id
            select new SellOrderItemLineJoin { Item = item, So = so };
        var ids = SalesOrderItemLineListFilter.ApplyQuickFilter(db, lines, quickFilter)
            .Select(x => x.So.Id);
        return q.Where(o => ids.Contains(o.Id));
    }
}
