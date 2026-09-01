using CRM.Core.Models.Analytics;

namespace CRM.Core.Interfaces;

/// <summary>付款记录列表看板：与 <see cref="IFinancePaymentListQuery"/> 共用筛选。</summary>
public interface IFinancePaymentListAnalyticsQuery
{
    Task<FinancePaymentListAnalyticsDashboardDto> GetDashboardAsync(
        FinancePaymentQueryRequest query,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancePaymentListAnalyticsTrendPointDto>> GetTrendsAsync(
        FinancePaymentQueryRequest query,
        string groupBy,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancePaymentListAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        FinancePaymentQueryRequest query,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    Task<FinancePaymentListAnalyticsRankingsDto> GetRankingsAsync(
        FinancePaymentQueryRequest query,
        bool maskAmounts,
        CancellationToken cancellationToken = default);
}
