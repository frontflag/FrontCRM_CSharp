using CRM.Core.Constants;
using CRM.Core.Models.Sales;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.RfqListQueries;

internal static partial class RfqItemListFilter
{
    /// <summary>界面三星对应存盘值（与 RFQCreate el-rate max=3 一致）。</summary>
    private const short ImportantImportanceValue = 3;

    public static IQueryable<RfqItemListJoin> ApplyQuickFilter(
        ApplicationDbContext db,
        IQueryable<RfqItemListJoin> q,
        string? quickFilter)
    {
        if (string.IsNullOrWhiteSpace(quickFilter) || !RfqItemListQuickFilterCodes.IsKnown(quickFilter))
            return q;

        return quickFilter.Trim() switch
        {
            RfqItemListQuickFilterCodes.Important => q.Where(x => x.Rfq.Importance == ImportantImportanceValue),
            RfqItemListQuickFilterCodes.Converted => ApplyConverted(db, q),
            RfqItemListQuickFilterCodes.PendingQuote => q.Where(x => x.Item.Status == 0),
            RfqItemListQuickFilterCodes.NoQuote => q.Where(x => x.Item.Status == 5),
            RfqItemListQuickFilterCodes.MultiQuote => ApplyMultiQuote(db, q),
            _ => q
        };
    }

    /// <summary>与看板成单明细一致：SO 明细有效 + 报价回挂 + SO 主单 ≥ 审核通过。</summary>
    private static IQueryable<RfqItemListJoin> ApplyConverted(
        ApplicationDbContext db,
        IQueryable<RfqItemListJoin> q) =>
        q.Where(x =>
            db.SellOrderItems.AsNoTracking().Any(oi =>
                !oi.IsDeleted
                && oi.Status == 0
                && oi.QuoteId != null
                && db.SellOrders.AsNoTracking().Any(so =>
                    so.Id == oi.SellOrderId && so.Status >= SellOrderMainStatus.Approved)
                && db.Quotes.AsNoTracking().Any(quote =>
                    quote.Id == oi.QuoteId
                    && quote.RFQItemId != null
                    && quote.RFQItemId == x.Item.Id)));

    private static IQueryable<RfqItemListJoin> ApplyMultiQuote(
        ApplicationDbContext db,
        IQueryable<RfqItemListJoin> q) =>
        q.Where(x =>
            db.Quotes.AsNoTracking().Count(quote =>
                quote.RFQItemId != null && quote.RFQItemId == x.Item.Id) >= 2);
}
