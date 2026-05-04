using CRM.Core.Models.RFQ;

namespace CRM.Core.Interfaces;

/// <summary>需求主表列表：数据库侧筛选与分页（与 <see cref="IRFQService.GetPagedAsync"/> 配合）。</summary>
public interface IRfqMainListQuery
{
    /// <summary>同筛选条件下的分页主表行及列表页统计（非仅当前页）。</summary>
    Task<RfqMainListQueryPage> GetPagedWithAggregatesAsync(
        RFQQueryRequest request,
        CancellationToken cancellationToken = default);
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
