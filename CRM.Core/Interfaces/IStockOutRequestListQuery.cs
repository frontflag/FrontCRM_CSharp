using System.Threading;

namespace CRM.Core.Interfaces;

/// <summary>出库通知列表：数据库侧分页主键。</summary>
public interface IStockOutRequestListQuery
{
    Task<PagedResult<string>> GetPagedStockOutRequestIdsAsync(
        StockOutRequestListQueryRequest? filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
