using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Finance;

public sealed class FinanceCustomerAdvanceListQuery : IFinanceCustomerAdvanceListQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public FinanceCustomerAdvanceListQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    public async Task<PagedResult<FinanceCustomerAdvance>> GetPagedAsync(
        FinanceCustomerAdvanceQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var q = _db.FinanceCustomerAdvances.AsNoTracking();
        q = await _dataPermission.ApplyFinanceCustomerAdvanceListDataScopeAsync(
            request.CurrentUserId, q, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.CustomerId))
            q = q.Where(a => a.CustomerId == request.CustomerId.Trim());

        if (request.Currency.HasValue)
            q = q.Where(a => a.Currency == request.Currency.Value);

        if (request.OnlyPositiveBalance == true)
            q = q.Where(a => a.Balance > 0m);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var k = request.Keyword.Trim().ToLowerInvariant();
            q = q.Where(a =>
                a.CustomerId.ToLower().Contains(k)
                || (a.CustomerName != null && a.CustomerName.ToLower().Contains(k)));
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(a => a.Balance)
            .ThenBy(a => a.CustomerId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<FinanceCustomerAdvance>
        {
            Items = items,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<FinanceCustomerAdvanceLedger>> GetLedgerPagedAsync(
        FinanceCustomerAdvanceLedgerQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var q = _db.FinanceCustomerAdvanceLedgers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.CustomerId))
            q = q.Where(l => l.CustomerId == request.CustomerId.Trim());

        if (request.Currency.HasValue)
            q = q.Where(l => l.Currency == request.Currency.Value);

        if (request.LedgerType.HasValue)
            q = q.Where(l => l.LedgerType == request.LedgerType.Value);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(l => l.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<FinanceCustomerAdvanceLedger>
        {
            Items = items,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }
}
