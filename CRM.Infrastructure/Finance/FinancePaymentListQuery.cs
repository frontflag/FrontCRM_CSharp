using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Finance;

/// <summary>付款单主表列表：EF 侧 <c>CountAsync</c> + <c>Skip</c>/<c>Take</c>。</summary>
public sealed class FinancePaymentListQuery : IFinancePaymentListQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermissionService;

    public FinancePaymentListQuery(ApplicationDbContext db, IDataPermissionService dataPermissionService)
    {
        _db = db;
        _dataPermissionService = dataPermissionService;
    }

    /// <inheritdoc />
    public async Task<PagedResult<FinancePayment>> GetPagedAsync(
        FinancePaymentQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var q = _db.FinancePayments.AsNoTracking();
        q = await _dataPermissionService.ApplyFinancePaymentListDataScopeAsync(
            request.CurrentUserId,
            q,
            _db.Vendors.AsNoTracking(),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var k = request.Keyword.Trim().ToLowerInvariant();
            q = q.Where(p =>
                (p.FinancePaymentCode != null && p.FinancePaymentCode.ToLower().Contains(k)) ||
                (p.VendorName != null && p.VendorName.ToLower().Contains(k)));
        }

        if (request.Status.HasValue)
            q = q.Where(p => p.Status == request.Status.Value);

        if (request.StartDate.HasValue)
            q = q.Where(p => p.CreateTime >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            q = q.Where(p => p.CreateTime <= request.EndDate.Value.AddDays(1));

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(p => p.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<FinancePayment>
        {
            Items = items,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }
}
