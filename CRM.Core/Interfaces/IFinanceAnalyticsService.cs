using CRM.Core.Models.Analytics;

namespace CRM.Core.Interfaces;

public interface IFinanceAnalyticsService
{
    Task<(bool Ok, string? Error, FinanceAnalyticsResolvedScope? Scope)> ResolveScopeAsync(
        string userId,
        FinanceAnalyticsQueryParams query,
        CancellationToken cancellationToken = default);

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
