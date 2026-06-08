using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Finance;

public sealed class FinanceReceivableListQuery : IFinanceReceivableListQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public FinanceReceivableListQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    /// <inheritdoc />
    public async Task<PagedResult<FinanceReceivable>> GetPagedAsync(
        FinanceReceivableQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var q = _db.FinanceReceivables.AsNoTracking();
        q = await _dataPermission.ApplyFinanceReceivableListDataScopeAsync(
            request.CurrentUserId,
            q,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.CustomerId))
        {
            var cid = request.CustomerId.Trim();
            q = q.Where(r => r.CustomerId == cid);
        }

        if (request.VerificationStatus.HasValue)
            q = q.Where(r => r.VerificationStatus == request.VerificationStatus.Value);

        if (request.OnlyOpen == true)
            q = q.Where(r => r.VerifiedToBe > 0m);

        if (request.StockOutDateFrom.HasValue)
            q = q.Where(r => r.StockOutDate >= request.StockOutDateFrom.Value);

        if (request.StockOutDateTo.HasValue)
            q = q.Where(r => r.StockOutDate < request.StockOutDateTo.Value.AddDays(1));

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var k = request.Keyword.Trim().ToLowerInvariant();
            q = q.Where(r =>
                (r.ReceivableCode != null && r.ReceivableCode.ToLower().Contains(k)) ||
                r.StockOutCode.ToLower().Contains(k) ||
                (r.SellOrderCode != null && r.SellOrderCode.ToLower().Contains(k)) ||
                (r.CustomerName != null && r.CustomerName.ToLower().Contains(k)) ||
                (r.PN != null && r.PN.ToLower().Contains(k)));
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(r => r.StockOutDate ?? r.CreateTime)
            .ThenByDescending(r => r.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<FinanceReceivable>
        {
            Items = items,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }
}
