using CRM.Core.Models.Analytics;

namespace CRM.Core.Interfaces;

public interface ISalesAnalyticsService
{
    Task<(bool Ok, string? Error, SalesAnalyticsResolvedScope? Scope)> ResolveScopeAsync(
        string userId,
        SalesAnalyticsQueryParams query,
        CancellationToken cancellationToken = default);

    Task<SalesAnalyticsDashboardDto> GetDashboardAsync(
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesAnalyticsTrendPointDto>> GetTrendsAsync(
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default);

    Task<SalesAnalyticsCustomerDto> GetCustomerAsync(
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default);

    Task<SalesAnalyticsStockOutProgressDetailDto> GetStockOutProgressDetailAsync(
        SalesAnalyticsResolvedScope scope,
        short? stockOutProgressStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
