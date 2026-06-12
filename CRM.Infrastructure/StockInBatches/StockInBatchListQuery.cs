using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Infrastructure.Data;
using CRM.Infrastructure.PurchaseOrders;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.StockInBatches;

public sealed class StockInBatchListQuery : IStockInBatchListQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public StockInBatchListQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    /// <inheritdoc />
    public async Task<PagedResult<StockInBatch>> GetPagedAsync(
        string? globalBatchNo,
        string? lot,
        string? serialNumber,
        int page,
        int pageSize,
        string? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);

        var q = _db.StockInBatches.AsNoTracking();
        q = await PurchaseOrderDataScopeQueryHelper.FilterStockInBatchesAsync(
            _dataPermission, _db, currentUserId, q, cancellationToken);

        if (!string.IsNullOrWhiteSpace(globalBatchNo))
        {
            var needle = globalBatchNo.Trim();
            q = q.Where(x => x.GlobalBatchNo.Contains(needle));
        }

        if (!string.IsNullOrWhiteSpace(lot))
        {
            var needle = lot.Trim();
            q = q.Where(x => x.Lot != null && x.Lot.Contains(needle));
        }

        if (!string.IsNullOrWhiteSpace(serialNumber))
        {
            var needle = serialNumber.Trim();
            q = q.Where(x => x.SerialNumber != null && x.SerialNumber.Contains(needle));
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(x => x.CreateTime)
            .ThenBy(x => x.GlobalBatchNo)
            .ThenBy(x => x.Id)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        return new PagedResult<StockInBatch>
        {
            Items = items,
            TotalCount = total,
            PageIndex = p,
            PageSize = ps
        };
    }
}
