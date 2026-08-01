using CRM.Core.Models.Analytics;

namespace CRM.Core.Interfaces;

public interface IPurchaseAnalyticsService
{
    Task<(bool Ok, string? Error, PurchaseAnalyticsResolvedScope? Scope)> ResolveScopeAsync(
        string userId,
        PurchaseAnalyticsQueryParams query,
        CancellationToken cancellationToken = default);

    Task<PurchaseAnalyticsDashboardDto> GetDashboardAsync(
        PurchaseAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseAnalyticsTrendPointDto>> GetTrendsAsync(
        PurchaseAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        PurchaseAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default);

    Task<PurchaseAnalyticsVendorDto> GetVendorAsync(
        PurchaseAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default);
}
