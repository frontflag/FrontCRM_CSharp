using CRM.Core.Models.Analytics;

namespace CRM.Core.Interfaces;

/// <summary>收款记录列表看板：与 <see cref="IFinanceReceiptListQuery"/> 共用筛选。</summary>
public interface IFinanceReceiptListAnalyticsQuery
{
    Task<FinanceReceiptListAnalyticsDashboardDto> GetDashboardAsync(
        FinanceReceiptQueryRequest query,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinanceReceiptListAnalyticsTrendPointDto>> GetTrendsAsync(
        FinanceReceiptQueryRequest query,
        string groupBy,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinanceReceiptListAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        FinanceReceiptQueryRequest query,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    Task<FinanceReceiptListAnalyticsRankingsDto> GetRankingsAsync(
        FinanceReceiptQueryRequest query,
        bool maskAmounts,
        CancellationToken cancellationToken = default);
}
