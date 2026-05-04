using System.Threading;

namespace CRM.Core.Interfaces;

/// <summary>出库单主表列表：数据库侧分页主键（与 <see cref="IStockInListQuery"/> 模式一致）。</summary>
public interface IStockOutListQuery
{
    Task<PagedResult<string>> GetPagedStockOutIdsAsync(
        string? keyword,
        string? sourceCode,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
