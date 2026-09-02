using CRM.Core.Models.Purchase;

namespace CRM.Core.Interfaces;

public class PurchaseQuoterPoolMemberDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? RealName { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsActive { get; set; }
    public bool IsSelected { get; set; }
}

public class PurchaseQuoterPoolListResult
{
    public IReadOnlyList<PurchaseQuoterPoolMemberDto> Items { get; set; } = Array.Empty<PurchaseQuoterPoolMemberDto>();
    public int SelectedCount { get; set; }
}

public interface IPurchaseQuoterPoolService
{
    Task<int> GetAssigneeCountAsync(CancellationToken cancellationToken = default);

    Task SetAssigneeCountAsync(int count, CancellationToken cancellationToken = default);

    /// <param name="filter">all 或 selected</param>
    Task<PurchaseQuoterPoolListResult> ListMembersAsync(string? filter, CancellationToken cancellationToken = default);

    Task<PurchaseQuoterPoolListResult> SavePoolAsync(IReadOnlyList<string> userIds, CancellationToken cancellationToken = default);

    /// <summary>轮询用：按 sort_order 排序，且仅返回在职用户。</summary>
    Task<IReadOnlyList<string>> GetOrderedActivePoolUserIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>需求明细保护时长（分钟）；0 表示关闭。</summary>
    Task<int> GetDemandProtectionMinutesAsync(CancellationToken cancellationToken = default);

    Task SetDemandProtectionMinutesAsync(int minutes, CancellationToken cancellationToken = default);

    /// <summary>新建需求时默认分配方式（2/3/5）。</summary>
    Task<short> GetDefaultAssignMethodAsync(CancellationToken cancellationToken = default);

    Task SetDefaultAssignMethodAsync(short assignMethod, CancellationToken cancellationToken = default);

    /// <summary>是否允许新建/编辑需求选择指定采购。默认 false。</summary>
    Task<bool> GetAllowDesignatedPurchaserAsync(CancellationToken cancellationToken = default);

    Task SetAllowDesignatedPurchaserAsync(bool allow, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新供应商时是否允许同步已完成业务节点。默认 false。
    /// </summary>
    Task<bool> GetAllowRefreshCompletedBizNodesAsync(CancellationToken cancellationToken = default);

    Task SetAllowRefreshCompletedBizNodesAsync(bool allow, CancellationToken cancellationToken = default);

    /// <summary>分面刷新：是否允许覆盖已完结下游。</summary>
    Task<PurchaseRefreshCompletedFacets> GetRefreshCompletedFacetsAsync(CancellationToken cancellationToken = default);

    Task SetRefreshCompletedFacetsAsync(PurchaseRefreshCompletedFacets facets, CancellationToken cancellationToken = default);
}

public class PurchaseRefreshCompletedFacets
{
    public bool Vendor { get; set; }
    public bool Pn { get; set; } = true;
    public bool Brand { get; set; } = true;
    public bool Qty { get; set; } = true;
    public bool Price { get; set; } = true;

    public bool Allows(PurchaseOrderRefreshFacet facet) => facet switch
    {
        PurchaseOrderRefreshFacet.Vendor => Vendor,
        PurchaseOrderRefreshFacet.Pn => Pn,
        PurchaseOrderRefreshFacet.Brand => Brand,
        PurchaseOrderRefreshFacet.Qty => Qty,
        PurchaseOrderRefreshFacet.Price => Price,
        _ => true
    };
}
