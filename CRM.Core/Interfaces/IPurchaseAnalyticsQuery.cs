using CRM.Core.Models.Analytics;

namespace CRM.Core.Interfaces;

public interface IPurchaseAnalyticsQuery
{
    Task<PurchaseAnalyticsDashboardDto> GetDashboardAsync(
        PurchaseAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseAnalyticsTrendPointDto>> GetTrendsAsync(
        PurchaseAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        PurchaseAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default);
}
