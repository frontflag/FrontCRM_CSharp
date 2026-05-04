using CRM.Core.Interfaces;
using CRM.Core.Models.Purchase;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.PurchaseOrders;

/// <summary>采购订单列表：EF 数据库分页（与内存全表方案行为对齐）。</summary>
public sealed class PurchaseOrderListQuery : IPurchaseOrderListQuery
{
    /// <summary>单页上限；与采购订单明细页批量拉主单（pageSize=2000）对齐，普通列表 UI 仍建议使用较小分页。</summary>
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public PurchaseOrderListQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    /// <inheritdoc />
    public async Task<PagedResult<PurchaseOrder>> GetPagedAsync(
        PurchaseOrderQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var filtered = await BuildFilteredQueryAsync(request, cancellationToken);
        var total = await filtered.CountAsync(cancellationToken);
        var items = await filtered
            .OrderByDescending(o => o.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PurchaseOrder>
        {
            Items = items,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderListAggregates> GetAggregatesAsync(
        PurchaseOrderQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var q = await BuildFilteredQueryAsync(request, cancellationToken);
        return new PurchaseOrderListAggregates
        {
            TotalCount = await q.CountAsync(cancellationToken),
            PendingConfirmCount = await q.CountAsync(o => o.Status == 20, cancellationToken),
            InProgressCount = await q.CountAsync(o => o.Status == 50, cancellationToken),
            TotalAmountSum = await q.SumAsync(o => (decimal?)o.Total, cancellationToken) ?? 0m
        };
    }

    private async Task<IQueryable<PurchaseOrder>> BuildFilteredQueryAsync(
        PurchaseOrderQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = _db.PurchaseOrders.AsNoTracking();
        q = await _dataPermission.ApplyPurchaseOrderDataScopeAsync(request.CurrentUserId, q, cancellationToken);

        var hasSplitFilters = !string.IsNullOrWhiteSpace(request.PurchaseOrderCodeFilter)
            || !string.IsNullOrWhiteSpace(request.VendorNameFilter);

        if (!string.IsNullOrWhiteSpace(request.Keyword) && !hasSplitFilters)
        {
            var k = request.Keyword.Trim();
            q = q.Where(o =>
                (o.PurchaseOrderCode != null && o.PurchaseOrderCode.ToLower().Contains(k.ToLower())) ||
                (o.VendorName != null && o.VendorName.ToLower().Contains(k.ToLower())));
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(request.PurchaseOrderCodeFilter))
            {
                var c = request.PurchaseOrderCodeFilter.Trim();
                q = q.Where(o => o.PurchaseOrderCode != null && o.PurchaseOrderCode.ToLower().Contains(c.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(request.VendorNameFilter))
            {
                var v = request.VendorNameFilter.Trim();
                q = q.Where(o => o.VendorName != null && o.VendorName.ToLower().Contains(v.ToLower()));
            }
        }

        if (request.Status.HasValue)
            q = q.Where(o => o.Status == request.Status.Value);

        if (request.OrderType.HasValue)
            q = q.Where(o => o.Type == request.OrderType.Value);

        if (request.StartDate.HasValue)
            q = q.Where(o => o.CreateTime >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            q = q.Where(o => o.CreateTime <= request.EndDate.Value.AddDays(1));

        return q;
    }
}
