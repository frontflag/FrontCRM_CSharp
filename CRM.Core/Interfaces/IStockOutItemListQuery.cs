using System.Threading;

namespace CRM.Core.Interfaces;

/// <summary>出库明细行列表：数据库侧分页主键（筛选与 <see cref="StockOutItemListQuery"/> 一致）。</summary>
public interface IStockOutItemListQuery
{
    Task<PagedResult<string>> GetPagedStockOutItemIdsAsync(
        StockOutItemListQuery? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
