using CRM.Core.Interfaces;
using CRM.Core.Models.Sales;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.SalesOrders;

/// <summary>销售订单主表列表：EF 数据库分页。</summary>
public sealed class SalesOrderListQuery : ISalesOrderListQuery
{
    /// <summary>单页上限（与采购订单主表列表对齐，便于大批量导出）。</summary>
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public SalesOrderListQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    /// <inheritdoc />
    public async Task<PagedResult<SellOrder>> GetPagedAsync(
        SalesOrderQueryRequest request,
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

        return new PagedResult<SellOrder>
        {
            Items = items,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<SalesOrderListAggregates> GetAggregatesAsync(
        SalesOrderQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var q = await BuildFilteredQueryAsync(request, cancellationToken);
        return new SalesOrderListAggregates
        {
            TotalCount = await q.CountAsync(cancellationToken),
            PendingCount = await q.CountAsync(
                o => o.Status == SellOrderMainStatus.New || o.Status == SellOrderMainStatus.PendingAudit,
                cancellationToken),
            ApprovedPlusCount = await q.CountAsync(
                o => o.Status == SellOrderMainStatus.Approved
                    || o.Status == SellOrderMainStatus.InProgress
                    || o.Status == SellOrderMainStatus.Completed,
                cancellationToken),
            TotalAmountSum = await q.SumAsync(o => (decimal?)o.Total, cancellationToken) ?? 0m
        };
    }

    private async Task<IQueryable<SellOrder>> BuildFilteredQueryAsync(
        SalesOrderQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = _db.SellOrders.AsNoTracking();
        q = await _dataPermission.ApplySellOrderDataScopeAsync(request.CurrentUserId, q, cancellationToken);

        var hasSplitFilters = !string.IsNullOrWhiteSpace(request.SellOrderCodeFilter)
            || !string.IsNullOrWhiteSpace(request.CustomerNameFilter);

        if (!string.IsNullOrWhiteSpace(request.Keyword) && !hasSplitFilters)
        {
            var k = request.Keyword.Trim();
            q = q.Where(o =>
                (o.SellOrderCode != null && o.SellOrderCode.ToLower().Contains(k.ToLower())) ||
                (o.CustomerName != null && o.CustomerName.ToLower().Contains(k.ToLower())));
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(request.SellOrderCodeFilter))
            {
                var c = request.SellOrderCodeFilter.Trim();
                q = q.Where(o => o.SellOrderCode != null && o.SellOrderCode.ToLower().Contains(c.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(request.CustomerNameFilter))
            {
                var v = request.CustomerNameFilter.Trim();
                q = q.Where(o => o.CustomerName != null && o.CustomerName.ToLower().Contains(v.ToLower()));
            }
        }

        if (request.Status.HasValue)
            q = q.Where(o => (short)o.Status == request.Status.Value);

        if (request.StartDate.HasValue)
            q = q.Where(o => o.CreateTime >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            q = q.Where(o => o.CreateTime <= request.EndDate.Value.AddDays(1));

        return q;
    }
}
