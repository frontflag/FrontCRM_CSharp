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
    private readonly IDataPermissionService _dataPermission;

    public QuoteListQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    /// <inheritdoc />
    public async Task<PagedResult<Quote>> GetPagedAsync(
        QuoteQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var filtered = await BuildFilteredQueryAsync(request, cancellationToken);
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
        var q = await BuildFilteredQueryAsync(request, cancellationToken);
        var total = await q.CountAsync(cancellationToken);
        var newCount = await q.CountAsync(x => x.Status == (short)QuoteMainStatus.New, cancellationToken);
        var wonCount = await q.CountAsync(x => x.Status == (short)QuoteMainStatus.Won, cancellationToken);
        var closedCount = await q.CountAsync(x => x.Status == (short)QuoteMainStatus.Closed, cancellationToken);

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
            NewCount = newCount,
            WonCount = wonCount,
            ClosedCount = closedCount,
            CreatedInRangeCount = inRange
        };
    }

    private async Task<IQueryable<Quote>> BuildFilteredQueryAsync(
        QuoteQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = _db.Quotes.AsNoTracking();
        q = await _dataPermission.ApplyQuoteListDataScopeAsync(
            request.CurrentUserId,
            q,
            _db.RFQs.AsNoTracking(),
            _db.RFQItems.AsNoTracking(),
            cancellationToken);

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
        string? currentUserId = null,
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

        var q = _db.Quotes.AsNoTracking()
            .Where(x => x.RFQItemId != null && ids.Contains(x.RFQItemId));
        q = await _dataPermission.ApplyQuoteListDataScopeAsync(
            currentUserId,
            q,
            _db.RFQs.AsNoTracking(),
            _db.RFQItems.AsNoTracking(),
            cancellationToken);

        var rows = await q
            .GroupBy(x => x.RFQItemId!)
            .Select(g => new { RfqItemId = g.Key, Cnt = g.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.RfqItemId, x => x.Cnt, StringComparer.OrdinalIgnoreCase);
    }
}
