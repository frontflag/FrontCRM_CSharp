using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces;

/// <summary>盘点计划列表的数据库分页。</summary>
public interface IInventoryCountPlanListQuery
{
    public const int MaxPageSize = 2000;

    Task<PagedResult<InventoryCountPlan>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
