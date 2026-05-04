using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Finance;

/// <summary>销项发票主表列表：EF 侧 <c>CountAsync</c> + <c>Skip</c>/<c>Take</c>。</summary>
public sealed class FinanceSellInvoiceListQuery : IFinanceSellInvoiceListQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermissionService;

    public FinanceSellInvoiceListQuery(ApplicationDbContext db, IDataPermissionService dataPermissionService)
    {
        _db = db;
        _dataPermissionService = dataPermissionService;
    }

    /// <inheritdoc />
    public async Task<PagedResult<FinanceSellInvoice>> GetPagedAsync(
        FinanceSellInvoiceQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var q = _db.FinanceSellInvoices.AsNoTracking();
        q = await _dataPermissionService.ApplyFinanceSellInvoiceListDataScopeAsync(
            request.CurrentUserId,
            q,
            _db.Customers.AsNoTracking(),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var k = request.Keyword.Trim().ToLowerInvariant();
            q = q.Where(inv =>
                (inv.CustomerName != null && inv.CustomerName.ToLower().Contains(k)) ||
                (inv.InvoiceCode != null && inv.InvoiceCode.ToLower().Contains(k)) ||
                (inv.InvoiceNo != null && inv.InvoiceNo.ToLower().Contains(k)));
        }

        if (request.InvoiceStatus.HasValue)
            q = q.Where(inv => inv.InvoiceStatus == request.InvoiceStatus.Value);

        if (request.ReceiveStatus.HasValue)
            q = q.Where(inv => inv.ReceiveStatus == request.ReceiveStatus.Value);

        if (request.StartDate.HasValue)
            q = q.Where(inv => inv.MakeInvoiceDate >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            q = q.Where(inv => inv.MakeInvoiceDate <= request.EndDate.Value.AddDays(1));

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(inv => inv.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<FinanceSellInvoice>
        {
            Items = items,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }
}
