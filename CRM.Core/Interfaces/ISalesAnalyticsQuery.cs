using CRM.Core.Models.Analytics;

namespace CRM.Core.Interfaces;

public interface ISalesAnalyticsQuery
{
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
