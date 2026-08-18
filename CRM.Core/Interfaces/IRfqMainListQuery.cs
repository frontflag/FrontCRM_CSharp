using CRM.Core.Models.Analytics;
using CRM.Core.Models.RFQ;

namespace CRM.Core.Interfaces;

/// <summary>需求主表列表：数据库侧筛选与分页（与 <see cref="IRFQService.GetPagedAsync"/> 配合）。</summary>
public interface IRfqMainListQuery
{
    /// <summary>同筛选条件下的分页主表行及列表页统计（非仅当前页）。</summary>
    Task<RfqMainListQueryPage> GetPagedWithAggregatesAsync(
        RFQQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<RfqListAnalyticsDashboardDto> GetListAnalyticsDashboardAsync(
        RFQQueryRequest request,
        bool maskCustomerNames,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RfqListAnalyticsTrendPointDto>> GetListAnalyticsTrendsAsync(
        RFQQueryRequest request,
        string groupBy,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetListAnalyticsBreakdownsAsync(
        RFQQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<RfqListAnalyticsRankingsDto> GetListAnalyticsRankingsAsync(
        RFQQueryRequest request,
        bool maskCustomerNames,
        CancellationToken cancellationToken = default);

    /// <summary>已软删需求主表分页（忽略全局过滤器，仍套销售/采购数据范围）。</summary>
    Task<RfqMainListQueryPage> GetDeletedPagedAsync(
        RFQQueryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>各需求最新一次整单删除操作日志（删除人/时间/明细 ID）。</summary>
    Task<IReadOnlyDictionary<string, RfqHeaderDeleteLogInfo>> GetLatestRfqHeaderDeleteLogsAsync(
        IReadOnlyList<string> recordIds,
        CancellationToken cancellationToken = default);
}

/// <summary>需求整单删除日志摘要（联 log_operation）。</summary>
public sealed class RfqHeaderDeleteLogInfo
{
    public DateTime OperationTime { get; init; }
    public string? OperatorUserName { get; init; }
    public string? ExtraInfo { get; init; }
}

/// <summary>需求主表列表一次查询结果：当前页实体 + 全量筛选维度统计。</summary>
public sealed class RfqMainListQueryPage
{
    public IReadOnlyList<RFQ> Items { get; init; } = Array.Empty<RFQ>();
    public int TotalCount { get; init; }
    public int PageIndex { get; init; }
    public int PageSize { get; init; }
    public RfqMainListAggregates Aggregates { get; init; } = new();
}

/// <summary>需求主表列表统计卡片（与全量筛选条件一致）。</summary>
public sealed class RfqMainListAggregates
{
    public int Total { get; init; }
    public int Pending { get; init; }
    public int Processing { get; init; }
    public int Quoted { get; init; }
}
