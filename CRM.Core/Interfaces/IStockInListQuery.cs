namespace CRM.Core.Interfaces;

/// <summary>入库单主列表：数据库侧筛选与分页（返回主键 Id，由 <see cref="IStockInService"/> 组装列表 DTO）。</summary>
public interface IStockInListQuery
{
    Task<PagedResult<string>> GetPagedStockInIdsAsync(
        StockInQueryRequest? request,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
