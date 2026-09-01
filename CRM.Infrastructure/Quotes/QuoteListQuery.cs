using CRM.Core.Interfaces;
using CRM.Core.Models.Quote;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Quotes;

public sealed partial class QuoteListQuery : IQuoteListQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;
    private readonly IRbacService _rbacService;
    private readonly IPurchaseQuoterPoolService _purchaseQuoterPoolService;

    public QuoteListQuery(
        ApplicationDbContext db,
        IDataPermissionService dataPermission,
        IRbacService rbacService,
        IPurchaseQuoterPoolService purchaseQuoterPoolService)
    {
        _db = db;
        _dataPermission = dataPermission;
        _rbacService = rbacService;
        _purchaseQuoterPoolService = purchaseQuoterPoolService;
    }

    /// <inheritdoc />
    public async Task<PagedResult<Quote>> GetPagedAsync(
        QuoteQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var filtered = await QuoteListFilter.BuildFilteredQuotesQueryAsync(
            _db, _dataPermission, request, cancellationToken);
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
        var q = await QuoteListFilter.BuildFilteredQuotesQueryAsync(
            _db, _dataPermission, request, cancellationToken);
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

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, int>> GetQuoteCountsByRfqItemIdsAsync(
        IReadOnlyCollection<string> rfqItemIds,
        string? currentUserId = null,
        CancellationToken cancellationToken = default,
        bool skipListDataScope = false)
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
            .Where(x => !x.IsDeleted && x.RFQItemId != null && ids.Contains(x.RFQItemId));
        if (!skipListDataScope)
        {
            q = await _dataPermission.ApplyQuoteListDataScopeAsync(
                currentUserId,
                q,
                _db.RFQs.AsNoTracking(),
                _db.RFQItems.AsNoTracking(),
                cancellationToken);
        }

        var rows = await q
            .GroupBy(x => x.RFQItemId!)
            .Select(g => new { RfqItemId = g.Key, Cnt = g.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.RfqItemId, x => x.Cnt, StringComparer.OrdinalIgnoreCase);
    }
}
