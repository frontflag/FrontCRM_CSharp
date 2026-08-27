using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.StockIns;

/// <summary>入库单列表：在库侧收窄主键后再由服务层组装展示字段（与 <see cref="StockInQueryRequest"/> 语义对齐）。</summary>
public sealed class StockInListQuery : IStockInListQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public StockInListQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    /// <inheritdoc />
    public async Task<PagedResult<string>> GetPagedStockInIdsAsync(
        StockInQueryRequest? request,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);

        var q = await StockInListFilter.BuildFilteredQueryAsync(_db, _dataPermission, request, cancellationToken);

        var total = await q.CountAsync(cancellationToken);
        var ids = await q
            .OrderByDescending(s => s.CreateTime)
            .ThenByDescending(s => s.Id)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        return new PagedResult<string>
        {
            Items = ids,
            TotalCount = total,
            PageIndex = p,
            PageSize = ps
        };
    }

    /// <inheritdoc />
    public async Task<bool> IsVisibleToUserAsync(
        string? userId,
        string stockInId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stockInId))
            return false;

        var q = _db.StockIns.AsNoTracking()
            .Where(s => s.Id == stockInId && s.StockInType != StockInTypeCode.Transfer);
        q = await ApplyDataScopesAsync(userId, q, cancellationToken);
        return await q.AnyAsync(cancellationToken);
    }

    private async Task<IQueryable<StockIn>> ApplyDataScopesAsync(
        string? userId,
        IQueryable<StockIn> query,
        CancellationToken cancellationToken) =>
        await _dataPermission.ApplyStockInListDataScopeAsync(
            userId,
            query,
            _db.SellOrders.AsNoTracking(),
            _db.SellOrderItems.AsNoTracking(),
            _db.StockInItemExtends.AsNoTracking(),
            _db.PurchaseOrderItems.AsNoTracking(),
            _db.PurchaseOrders.AsNoTracking(),
            cancellationToken);
}
