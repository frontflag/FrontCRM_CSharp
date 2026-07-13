using CRM.Core.Models.Analytics;

namespace CRM.Core.Interfaces;

/// <summary>需求明细行列表：数据库侧筛选与分页（与 <see cref="IRFQService.GetPagedItemsAsync"/> 配合）。</summary>
public interface IRfqItemListQuery
{
    Task<PagedResult<RFQItemListItem>> GetPagedAsync(
        RFQItemQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<RfqListAnalyticsDashboardDto> GetListAnalyticsDashboardAsync(
        RFQItemQueryRequest request,
        bool maskCustomerNames,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RfqListAnalyticsTrendPointDto>> GetListAnalyticsTrendsAsync(
        RFQItemQueryRequest request,
        string groupBy,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetListAnalyticsBreakdownsAsync(
        RFQItemQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<RfqItemListAnalyticsRankingsDto> GetListAnalyticsRankingsAsync(
        RFQItemQueryRequest request,
        bool maskCustomerNames,
        CancellationToken cancellationToken = default);
}
