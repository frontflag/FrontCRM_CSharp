namespace CRM.Core.Models.Paging;

/// <summary>列表排序方向。</summary>
public enum ListSortOrder
{
    Ascending = 0,
    Descending = 1
}

/// <summary>
/// 通用列表分页与关键字、排序入参基类。各模块列表请求可继承此类并增加业务筛选字段，
/// 或使用 <see cref="PagedListQueryBase{TConditions}"/> 将业务条件放在独立对象中。
/// </summary>
public class PagedListQueryBase
{
    /// <summary>通用关键字（由具体接口约定匹配列，如单号/名称）。</summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 排序字段标识（与接口文档约定一致，如 <c>createTime</c>、<c>purchaseOrderCode</c>）；
    /// 服务端应对允许字段做白名单映射，禁止直接反射拼接未校验字段名。
    /// </summary>
    public string? SortField { get; set; }

    /// <summary>排序方向；默认降序（与多数「最新在前」列表一致）。</summary>
    public ListSortOrder SortOrder { get; set; } = ListSortOrder.Descending;

    /// <summary>当前页码，从 1 开始。</summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>每页记录数。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>规范化页码（至少为 1）。</summary>
    public int GetNormalizedPageIndex() => PageIndex < 1 ? 1 : PageIndex;

    /// <summary>规范化每页条数，并限制在 [<paramref name="min"/>, <paramref name="max"/>] 区间内。</summary>
    public int GetNormalizedPageSize(int min = 1, int max = 200)
    {
        if (PageSize < min) return min;
        if (PageSize > max) return max;
        return PageSize;
    }

    /// <summary>用于 Skip 的偏移量。</summary>
    public int GetSkip(int minPageSize = 1, int maxPageSize = 200) =>
        (GetNormalizedPageIndex() - 1) * GetNormalizedPageSize(minPageSize, maxPageSize);

    /// <summary>是否降序（便于在 switch 表达式中映射到 OrderBy / OrderByDescending）。</summary>
    public bool IsSortDescending() => SortOrder == ListSortOrder.Descending;
}

/// <summary>
/// 带独立「查询条件」对象的通用分页基类；<typeparamref name="TConditions"/> 建议为 POCO，仅承载业务筛选字段。
/// </summary>
public class PagedListQueryBase<TConditions> : PagedListQueryBase where TConditions : class, new()
{
    /// <summary>业务查询条件（日期区间、状态、类型等）。</summary>
    public TConditions Conditions { get; set; } = new();
}
