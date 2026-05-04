using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Finance;

/// <summary>进项发票主表列表：EF 侧 <c>CountAsync</c> + <c>Skip</c>/<c>Take</c>。</summary>
public sealed class FinancePurchaseInvoiceListQuery : IFinancePurchaseInvoiceListQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermissionService;

    public FinancePurchaseInvoiceListQuery(ApplicationDbContext db, IDataPermissionService dataPermissionService)
    {
        _db = db;
        _dataPermissionService = dataPermissionService;
    }

    /// <inheritdoc />
    public async Task<PagedResult<FinancePurchaseInvoice>> GetPagedAsync(
        FinancePurchaseInvoiceQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var q = _db.FinancePurchaseInvoices.AsNoTracking();
        q = await _dataPermissionService.ApplyFinancePurchaseInvoiceListDataScopeAsync(
            request.CurrentUserId,
            q,
            _db.Vendors.AsNoTracking(),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var k = request.Keyword.Trim().ToLowerInvariant();
            q = q.Where(inv =>
                (inv.VendorName != null && inv.VendorName.ToLower().Contains(k)) ||
                (inv.InvoiceNo != null && inv.InvoiceNo.ToLower().Contains(k)));
        }

        if (request.ConfirmStatus.HasValue)
            q = q.Where(inv => inv.ConfirmStatus == request.ConfirmStatus.Value);

        if (request.InvoiceStatus.HasValue)
            q = q.Where(inv => inv.RedInvoiceStatus == request.InvoiceStatus.Value);

        if (request.StartDate.HasValue)
            q = q.Where(inv => inv.InvoiceDate >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            q = q.Where(inv => inv.InvoiceDate <= request.EndDate.Value.AddDays(1));

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(inv => inv.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<FinancePurchaseInvoice>
        {
            Items = items,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }
}
