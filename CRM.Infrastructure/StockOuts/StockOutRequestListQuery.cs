using CRM.Core.Interfaces;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.StockOuts;

public sealed class StockOutRequestListQuery : IStockOutRequestListQuery
{
    public const int MaxPageSize = 2000;
    private const short PickingTaskStatusCompleted = 100;

    private readonly ApplicationDbContext _db;

    public StockOutRequestListQuery(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<PagedResult<string>> GetPagedStockOutRequestIdsAsync(
        string? keyword,
        string? workflow,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);
        var wf = (workflow ?? "all").Trim().ToLowerInvariant();

        var q =
            from r in _db.StockOutRequests.AsNoTracking()
            join so in _db.SellOrders.AsNoTracking() on r.SalesOrderId equals so.Id into soj
            from so in soj.DefaultIfEmpty()
            select new { r, so };

        if (wf == "done")
            q = q.Where(x => x.r.Status == 1);
        else if (wf == "pending_pick")
        {
            q = q.Where(x =>
                x.r.Status == 0 &&
                !_db.PickingTasks.Any(pt =>
                    pt.StockOutRequestId == x.r.Id && pt.Status == PickingTaskStatusCompleted));
        }
        else if (wf == "picked_pending_out")
        {
            q = q.Where(x =>
                x.r.Status == 0 &&
                _db.PickingTasks.Any(pt =>
                    pt.StockOutRequestId == x.r.Id && pt.Status == PickingTaskStatusCompleted));
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLowerInvariant();
            q = q.Where(x =>
                x.r.RequestCode.ToLower().Contains(k) ||
                (x.so != null && x.so.SellOrderCode.ToLower().Contains(k)) ||
                x.r.MaterialCode.ToLower().Contains(k) ||
                (x.r.MaterialName != null && x.r.MaterialName.ToLower().Contains(k)) ||
                (x.so != null && x.so.CustomerName != null && x.so.CustomerName.ToLower().Contains(k)));
        }

        var total = await q.CountAsync(cancellationToken);
        var ids = await q
            .OrderByDescending(x => x.r.CreateTime)
            .ThenByDescending(x => x.r.RequestDate)
            .ThenBy(x => x.r.Id)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(x => x.r.Id)
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
