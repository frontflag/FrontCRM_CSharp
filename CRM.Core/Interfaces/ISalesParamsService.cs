namespace CRM.Core.Interfaces;

/// <summary>销售参数（sysparam）读写。</summary>
public interface ISalesParamsService
{
    /// <summary>
    /// 刷新客户时是否允许同步已完成业务节点（出库通知已出库、装箱已出库完成、出库单已出库等）。
    /// 默认 false。
    /// </summary>
    Task<bool> GetAllowRefreshCompletedBizNodesAsync(CancellationToken cancellationToken = default);

    Task SetAllowRefreshCompletedBizNodesAsync(bool allow, CancellationToken cancellationToken = default);
}
