using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.InventoryCenter;

public sealed class InventoryCountPlanListQuery : IInventoryCountPlanListQuery
{
    private readonly ApplicationDbContext _db;

    public InventoryCountPlanListQuery(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<PagedResult<InventoryCountPlan>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, IInventoryCountPlanListQuery.MaxPageSize);

        var q = _db.InventoryCountPlans.AsNoTracking()
            .OrderByDescending(x => x.PlanMonth)
            .ThenByDescending(x => x.CreateTime);

        var total = await q.CountAsync(cancellationToken);
        var items = await q.Skip((p - 1) * ps).Take(ps).ToListAsync(cancellationToken);

        return new PagedResult<InventoryCountPlan>
        {
            Items = items,
            TotalCount = total,
            PageIndex = p,
            PageSize = ps
        };
    }
}
