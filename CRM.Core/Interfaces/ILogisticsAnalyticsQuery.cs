using CRM.Core.Models.Analytics;

namespace CRM.Core.Interfaces;

public interface ILogisticsAnalyticsQuery
{
    Task<LogisticsAnalyticsDashboardDto> GetDashboardAsync(
        LogisticsAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LogisticsAnalyticsTrendPointDto>> GetTrendsAsync(
        LogisticsAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        LogisticsAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default);

    Task<LogisticsAnalyticsCustomerMatrixDto> GetCustomerMatrixAsync(
        LogisticsAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default);
}
