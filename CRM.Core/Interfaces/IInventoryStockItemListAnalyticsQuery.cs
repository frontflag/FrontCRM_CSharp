using CRM.Core.Models.Analytics;
using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces;

/// <summary>库存明细列表看板：与列表共用筛选与数据范围。</summary>
public interface IInventoryStockItemListAnalyticsQuery
{
    Task<InventoryStockItemListAnalyticsDashboardDto> GetDashboardAsync(
        InventoryStockItemListQuery request,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryOnHandListAnalyticsTrendPointDto>> GetTrendsAsync(
        InventoryStockItemListQuery request,
        string groupBy,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryOnHandListAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        InventoryStockItemListQuery request,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    Task<InventoryOnHandListAnalyticsRankingsDto> GetRankingsAsync(
        InventoryStockItemListQuery request,
        bool maskAmounts,
        CancellationToken cancellationToken = default);
}
