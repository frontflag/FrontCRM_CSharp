using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Quote;
using CRM.Core.Models.RFQ;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Quotes;

/// <summary>报价主表列表：EF 数据库分页。</summary>
public sealed class QuoteListQuery : IQuoteListQuery
{
    /// <summary>单页上限（与采购/销售主表列表对齐）。</summary>
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;

    public QuoteListQuery(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<PagedResult<Quote>> GetPagedAsync(
        QuoteQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var filtered = BuildFilteredQuery(request);
        var total = await filtered.CountAsync(cancellationToken);
        var items = await filtered
            .OrderByDescending(q => q.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Quote>
        {
            Items = items,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<QuoteListAggregates> GetAggregatesAsync(
        QuoteQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var q = BuildFilteredQuery(request);
        var total = await q.CountAsync(cancellationToken);
        var pending = await q.CountAsync(x => x.Status == 0 || x.Status == 1, cancellationToken);
        var sent = await q.CountAsync(x => x.Status == 3, cancellationToken);
        var accepted = await q.CountAsync(x => x.Status == 4, cancellationToken);

        int? inRange = null;
        if (request.AggregateCreateFromUtc.HasValue && request.AggregateCreateToExclusiveUtc.HasValue)
        {
            var from = request.AggregateCreateFromUtc.Value;
            var toEx = request.AggregateCreateToExclusiveUtc.Value;
            inRange = await q.CountAsync(
                x => x.CreateTime >= from && x.CreateTime < toEx,
                cancellationToken);
        }

        return new QuoteListAggregates
        {
            TotalCount = total,
            PendingCount = pending,
            SentCount = sent,
            AcceptedCount = accepted,
            CreatedInRangeCount = inRange
        };
    }

    private IQueryable<Quote> BuildFilteredQuery(QuoteQueryRequest request)
    {
        var q = _db.Quotes.AsNoTracking();

        if (request.Status.HasValue)
            q = q.Where(x => x.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.RfqItemId))
        {
            var rid = request.RfqItemId.Trim();
            q = q.Where(x => x.RFQItemId != null && x.RFQItemId == rid);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var k = request.Keyword.Trim();
            var kl = k.ToLower();
            q = q.Where(quote =>
                (quote.QuoteCode != null && quote.QuoteCode.ToLower().Contains(kl)) ||
                (quote.Mpn != null && quote.Mpn.ToLower().Contains(kl)) ||
                (quote.Remark != null && quote.Remark.ToLower().Contains(kl)) ||
                _db.Set<RFQ>().Any(r =>
                    r.Id == quote.RFQId &&
                    r.RfqCode != null &&
                    r.RfqCode.ToLower().Contains(kl)) ||
                _db.Set<CustomerInfo>().Any(c =>
                    quote.CustomerId != null &&
                    c.Id == quote.CustomerId &&
                    ((c.OfficialName != null && c.OfficialName.ToLower().Contains(kl)) ||
                     (c.NickName != null && c.NickName.ToLower().Contains(kl)) ||
                     (c.CustomerCode != null && c.CustomerCode.ToLower().Contains(kl)))) ||
                _db.Set<RFQ>().Any(r =>
                    r.Id == quote.RFQId &&
                    r.CustomerId != null &&
                    _db.Set<CustomerInfo>().Any(c2 =>
                        c2.Id == r.CustomerId &&
                        ((c2.OfficialName != null && c2.OfficialName.ToLower().Contains(kl)) ||
                         (c2.NickName != null && c2.NickName.ToLower().Contains(kl)) ||
                         (c2.CustomerCode != null && c2.CustomerCode.ToLower().Contains(kl))))) ||
                _db.Users.Any(u =>
                    quote.SalesUserId != null &&
                    u.Id == quote.SalesUserId &&
                    u.UserName != null &&
                    u.UserName.ToLower().Contains(kl)) ||
                _db.Users.Any(u =>
                    quote.PurchaseUserId != null &&
                    u.Id == quote.PurchaseUserId &&
                    u.UserName != null &&
                    u.UserName.ToLower().Contains(kl)) ||
                _db.QuoteItems.Any(qi =>
                    qi.QuoteId == quote.Id &&
                    ((qi.Brand != null && qi.Brand.ToLower().Contains(kl)) ||
                     (qi.Mpn != null && qi.Mpn.ToLower().Contains(kl)))));
        }

        return q;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, int>> GetQuoteCountsByRfqItemIdsAsync(
        IReadOnlyCollection<string> rfqItemIds,
        CancellationToken cancellationToken = default)
    {
        var ids = rfqItemIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(500)
            .ToList();
        if (ids.Count == 0)
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var rows = await _db.Quotes.AsNoTracking()
            .Where(q => q.RFQItemId != null && ids.Contains(q.RFQItemId))
            .GroupBy(q => q.RFQItemId!)
            .Select(g => new { RfqItemId = g.Key, Cnt = g.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.RfqItemId, x => x.Cnt, StringComparer.OrdinalIgnoreCase);
    }
}
