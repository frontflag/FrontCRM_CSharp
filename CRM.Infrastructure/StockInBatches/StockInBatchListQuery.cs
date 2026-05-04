using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.StockInBatches;

public sealed class StockInBatchListQuery : IStockInBatchListQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;

    public StockInBatchListQuery(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<PagedResult<StockInBatch>> GetPagedAsync(
        string? stockInItemCode,
        string? lot,
        string? serialNumber,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);

        var q = _db.StockInBatches.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(stockInItemCode))
        {
            var needle = stockInItemCode.Trim();
            q = q.Where(x => x.StockInItemCode != null && x.StockInItemCode.Contains(needle));
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
            .ThenBy(x => x.StockInItemCode)
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
