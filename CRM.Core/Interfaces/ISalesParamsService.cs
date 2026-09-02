using CRM.Core.Models.Sales;

namespace CRM.Core.Interfaces;

/// <summary>销售参数（sysparam）读写。</summary>
public interface ISalesParamsService
{
    /// <summary>
    /// 刷新客户时是否允许同步已完成业务节点（出库通知已出库、装箱已出库完成、出库单已出库等）。
    /// 默认 false。与 <see cref="GetRefreshCompletedFacetsAsync"/> 的 Customer 同步。
    /// </summary>
    Task<bool> GetAllowRefreshCompletedBizNodesAsync(CancellationToken cancellationToken = default);

    Task SetAllowRefreshCompletedBizNodesAsync(bool allow, CancellationToken cancellationToken = default);

    /// <summary>分面刷新：是否允许覆盖已完结下游。</summary>
    Task<SalesRefreshCompletedFacets> GetRefreshCompletedFacetsAsync(CancellationToken cancellationToken = default);

    Task SetRefreshCompletedFacetsAsync(SalesRefreshCompletedFacets facets, CancellationToken cancellationToken = default);
}

public class SalesRefreshCompletedFacets
{
    public bool Customer { get; set; }
    public bool Pn { get; set; } = true;
    public bool Brand { get; set; } = true;
    public bool Qty { get; set; } = true;
    public bool Price { get; set; } = true;

    public bool Allows(SalesOrderRefreshFacet facet) => facet switch
    {
        SalesOrderRefreshFacet.Customer => Customer,
        SalesOrderRefreshFacet.Pn => Pn,
        SalesOrderRefreshFacet.Brand => Brand,
        SalesOrderRefreshFacet.Qty => Qty,
        SalesOrderRefreshFacet.Price => Price,
        _ => true
    };
}
