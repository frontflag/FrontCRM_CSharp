using CRM.Core.Models.Analytics;

namespace CRM.Core.Interfaces;

public interface IFinanceAnalyticsQuery
{
    Task<FinanceAnalyticsDashboardDto> GetDashboardAsync(
        FinanceAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinanceAnalyticsTrendPointDto>> GetTrendsAsync(
        FinanceAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        FinanceAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default);
}
