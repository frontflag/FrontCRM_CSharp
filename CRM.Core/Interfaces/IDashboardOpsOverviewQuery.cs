using CRM.Core.Models.Dashboard;

namespace CRM.Core.Interfaces;

/// <summary>控制台业务总览：物流明细条数与财务已完成核销流水条数（近 30 天由调用方传入日期）。</summary>
public interface IDashboardOpsOverviewQuery
{
    Task<DashboardLogisticsOverviewDto> GetLogisticsAsync(
        string? currentUserId,
        DashboardOpsDateRange range,
        CancellationToken cancellationToken = default);

    Task<DashboardFinanceWriteOffOverviewDto> GetFinanceWriteOffsAsync(
        string? currentUserId,
        DashboardOpsDateRange range,
        DashboardFinanceWriteOffFlags flags,
        CancellationToken cancellationToken = default);
}
