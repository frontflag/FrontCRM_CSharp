using CRM.Core.Models.Analytics;

namespace CRM.Core.Interfaces;

/// <summary>入库单列表看板：与 <see cref="IStockInListQuery"/> 共用筛选。</summary>
public interface IStockInListAnalyticsQuery
{
    Task<StockInListAnalyticsDashboardDto> GetDashboardAsync(
        StockInQueryRequest query,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockInListAnalyticsTrendPointDto>> GetTrendsAsync(
        StockInQueryRequest query,
        string groupBy,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        StockInQueryRequest query,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    Task<StockInListAnalyticsRankingsDto> GetRankingsAsync(
        StockInQueryRequest query,
        bool maskAmounts,
        CancellationToken cancellationToken = default);
}
