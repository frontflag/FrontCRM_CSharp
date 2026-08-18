using System.Threading;

namespace CRM.Core.Interfaces;

/// <summary>出库单主表列表：数据库侧分页主键（与 <see cref="IStockInListQuery"/> 模式一致）。</summary>
public interface IStockOutListQuery
{
    Task<PagedResult<string>> GetPagedStockOutIdsAsync(
        StockOutListQueryRequest? filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>装箱收发货扩展表快递公司（packingId → LogisticsExpressMethod ItemCode）。</summary>
    Task<IReadOnlyDictionary<string, string>> GetPackingExpressCompanyByPackingIdsAsync(
        IReadOnlyCollection<string> packingIds,
        CancellationToken cancellationToken = default);
}
