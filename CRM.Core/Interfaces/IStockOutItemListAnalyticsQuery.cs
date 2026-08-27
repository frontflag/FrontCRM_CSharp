using CRM.Core.Models.Analytics;

namespace CRM.Core.Interfaces;

/// <summary>出库明细列表看板：与 <see cref="IStockOutItemListQuery"/> 共用筛选。</summary>
public interface IStockOutItemListAnalyticsQuery
{
    Task<StockOutItemListAnalyticsDashboardDto> GetDashboardAsync(
        StockOutItemListQuery query,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockOutItemListAnalyticsTrendPointDto>> GetTrendsAsync(
        StockOutItemListQuery query,
        string groupBy,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        StockOutItemListQuery query,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    Task<StockOutItemListAnalyticsRankingsDto> GetRankingsAsync(
        StockOutItemListQuery query,
        bool maskAmounts,
        CancellationToken cancellationToken = default);
}
