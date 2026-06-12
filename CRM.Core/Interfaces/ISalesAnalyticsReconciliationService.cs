using CRM.Core.Models.Analytics;

namespace CRM.Core.Interfaces;

public interface ISalesAnalyticsReconciliationService
{
    /// <summary>将看板 snapshot/todo 与列表同等数据范围+口径的基线聚合对账。</summary>
    Task<SalesAnalyticsReconciliationReportDto> ReconcileAsync(
        string actingUserId,
        SalesAnalyticsQueryParams query,
        CancellationToken cancellationToken = default);

    /// <summary>对多个抽样账号批量对账（联调用）。</summary>
    Task<IReadOnlyList<SalesAnalyticsReconciliationReportDto>> ReconcileSampleUsersAsync(
        string actingUserId,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default);
}
