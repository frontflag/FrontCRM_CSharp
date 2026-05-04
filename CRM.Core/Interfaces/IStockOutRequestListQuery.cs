using System.Threading;

namespace CRM.Core.Interfaces;

/// <summary>出库通知列表：数据库侧分页主键。</summary>
public interface IStockOutRequestListQuery
{
    /// <param name="workflow">all | done | pending_pick | picked_pending_out（与前端筛选一致）</param>
    Task<PagedResult<string>> GetPagedStockOutRequestIdsAsync(
        string? keyword,
        string? workflow,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
