using CRM.Core.Interfaces;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.StockOuts;

/// <summary>出库明细分页主键查询（与 <see cref="StockOutItemListQuery"/> 筛选语义对齐）。</summary>
public sealed class StockOutItemEfListQuery : IStockOutItemListQuery
{
    public const int MaxPageSize = 100;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public StockOutItemEfListQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    /// <inheritdoc />
    public async Task<PagedResult<string>> GetPagedStockOutItemIdsAsync(
        StockOutItemListQuery? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        query ??= new StockOutItemListQuery();
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);

        var q = await StockOutItemListFilter.BuildFilteredJoinQueryAsync(
            _db, _dataPermission, query, cancellationToken);

        var total = await q.CountAsync(cancellationToken);
        var ids = await q
            .OrderByDescending(x => x.Header.StockOutDate ?? DateTime.MinValue)
            .ThenBy(x => x.Header.StockOutCode)
            .ThenBy(x => x.Item.Id)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(x => x.Item.Id)
            .ToListAsync(cancellationToken);

        return new PagedResult<string>
        {
            Items = ids,
            TotalCount = total,
            PageIndex = p,
            PageSize = ps
        };
    }
}
