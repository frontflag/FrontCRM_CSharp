using CRM.Core.Models.Quote;

namespace CRM.Core.Interfaces;

/// <summary>报价主表列表：数据库分页与筛选（与 <see cref="IQuoteService.GetPagedAsync"/> 配合）。</summary>
public interface IQuoteListQuery
{
    Task<PagedResult<Quote>> GetPagedAsync(QuoteQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>与列表相同筛选条件下的汇总（列表页统计卡片；<see cref="QuoteListAggregates.CreatedInRangeCount"/> 仅当起止时间均传入时计算）。</summary>
    Task<QuoteListAggregates> GetAggregatesAsync(QuoteQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>按需求明细行 ID 统计关联报价主表条数（未出现在结果中的 id 视为 0）。</summary>
    Task<IReadOnlyDictionary<string, int>> GetQuoteCountsByRfqItemIdsAsync(
        IReadOnlyCollection<string> rfqItemIds,
        CancellationToken cancellationToken = default);
}

/// <summary>报价列表查询参数（与 <c>GET /api/v1/quotes</c> QueryString 对齐）。</summary>
public sealed class QuoteQueryRequest
{
    /// <summary>综合关键字：报价单号、MPN、备注、需求编号、客户展示名、业务员/采购员登录名、明细品牌/料号等（OR）。</summary>
    public string? Keyword { get; set; }

    public short? Status { get; set; }

    /// <summary>需求明细行 ID；与列表筛选一致。</summary>
    public string? RfqItemId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    /// <summary>可选；与 <see cref="AggregateCreateToExclusiveUtc"/> 同时有效时，用于统计区间内新建报价数（与 RFQ 首页「近 30 天报价数」对齐）。</summary>
    public DateTime? AggregateCreateFromUtc { get; set; }

    /// <summary>可选；上界为排他（CreateTime &lt; 该时刻）。</summary>
    public DateTime? AggregateCreateToExclusiveUtc { get; set; }
}

/// <summary>报价列表汇总（全量筛选结果，非仅当前页）。</summary>
public sealed class QuoteListAggregates
{
    public int TotalCount { get; set; }

    /// <summary>状态为草稿(0)或待审核(1)。</summary>
    public int PendingCount { get; set; }

    /// <summary>状态为已发送(3)。</summary>
    public int SentCount { get; set; }

    /// <summary>状态为已接受(4)。</summary>
    public int AcceptedCount { get; set; }

    /// <summary>在 <see cref="QuoteQueryRequest.AggregateCreateFromUtc"/>～<see cref="QuoteQueryRequest.AggregateCreateToExclusiveUtc"/> 内创建的条数；未传齐起止时为 null。</summary>
    public int? CreatedInRangeCount { get; set; }
}
