using CRM.Core.Models.Analytics;

namespace CRM.Core.Interfaces;

public interface ISalesAnalyticsReconciliationBaseline
{
    Task<SalesAnalyticsSnapshotDto> GetSnapshotAsync(
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default);

    Task<SalesAnalyticsTodoDto> GetTodoAsync(
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default);
}
