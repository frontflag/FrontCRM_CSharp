namespace CRM.Core.Interfaces;

/// <summary>质检单主列表：数据库侧筛选与分页（仅返回主键 Id 列表，由 <see cref="ILogisticsService"/> 做行展示填充）。</summary>
public interface IQcListQuery
{
    Task<PagedResult<string>> GetPagedQcIdsAsync(
        QcQueryRequest? request,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>按质检单 Id 批量统计质检图片数量（口径与有/无质检图片预设一致）。</summary>
    Task<IReadOnlyDictionary<string, int>> GetQcImageCountsAsync(
        IReadOnlyCollection<string> qcIds,
        CancellationToken cancellationToken = default);
}
