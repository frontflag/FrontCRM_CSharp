using CRM.Core.Models.Analytics;

namespace CRM.Core.Interfaces;

/// <summary>库存中心列表看板：与在库汇总列表共用筛选（不含拆分维度分组）。</summary>
public interface IInventoryOnHandListAnalyticsQuery
{
    Task<InventoryOnHandListAnalyticsDashboardDto> GetDashboardAsync(
        InventoryOnHandSummaryQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryOnHandListAnalyticsTrendPointDto>> GetTrendsAsync(
        InventoryOnHandSummaryQueryRequest request,
        string groupBy,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryOnHandListAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        InventoryOnHandSummaryQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    Task<InventoryOnHandListAnalyticsRankingsDto> GetRankingsAsync(
        InventoryOnHandSummaryQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default);
}
