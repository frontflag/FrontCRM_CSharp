using CRM.Core.Models.Analytics;

namespace CRM.Core.Interfaces;

public interface ILogisticsAnalyticsService
{
    Task<(bool Ok, string? Error, LogisticsAnalyticsResolvedScope? Scope)> ResolveScopeAsync(
        string userId,
        LogisticsAnalyticsQueryParams query,
        CancellationToken cancellationToken = default);

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
