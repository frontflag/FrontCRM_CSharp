using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces;

/// <summary>库存总览列表：在 <c>stock</c> 主表上做 Count + Skip/Take，供总览分页接口使用。</summary>
public interface IInventoryMaterialOverviewStockPageQuery
{
    public const int MaxPageSize = 2000;

    /// <returns>当前页 <c>StockInfo</c> 实体（已排序）；以及满足筛选的总行数。</returns>
    Task<(IReadOnlyList<StockInfo> Items, int TotalCount)> GetStockPageAsync(
        string? warehouseId,
        string? materialModel,
        string? stockCode,
        short? stockType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
