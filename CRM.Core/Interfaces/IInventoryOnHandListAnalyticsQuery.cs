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

    /// <summary>在库折算美金合计。无金额权限时返回 null。口径与在库看板折算美金相同。</summary>
    Task<decimal?> GetConvertedUsdTotalAsync(
        InventoryOnHandSummaryQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    /// <summary>库龄严格大于 <paramref name="minAgeDays"/> 的品牌/客户排行。没有入库日的层不计入。</summary>
    Task<InventoryOnHandListAnalyticsRankingsDto> GetAgedRankingsAsync(
        InventoryOnHandSummaryQueryRequest request,
        int minAgeDays,
        bool maskAmounts,
        CancellationToken cancellationToken = default);
}
