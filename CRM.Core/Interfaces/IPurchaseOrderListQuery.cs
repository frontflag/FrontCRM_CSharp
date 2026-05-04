using CRM.Core.Models.Purchase;

namespace CRM.Core.Interfaces;

/// <summary>采购订单列表：数据库侧分页与筛选（与 <see cref="IPurchaseOrderService.GetPagedAsync"/> 配合）。</summary>
public interface IPurchaseOrderListQuery
{
    Task<PagedResult<PurchaseOrder>> GetPagedAsync(PurchaseOrderQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>与列表相同筛选条件下的汇总（用于列表页统计卡片；金额是否返回由调用方按权限决定）。</summary>
    Task<PurchaseOrderListAggregates> GetAggregatesAsync(PurchaseOrderQueryRequest request, CancellationToken cancellationToken = default);
}

/// <summary>采购订单列表汇总（全量筛选结果，非仅当前页）。</summary>
public sealed class PurchaseOrderListAggregates
{
    public int TotalCount { get; set; }
    public int PendingConfirmCount { get; set; }
    public int InProgressCount { get; set; }
    /// <summary>筛选范围内主表 <see cref="PurchaseOrder.Total"/> 之和；无金额权限时调用方可忽略。</summary>
    public decimal TotalAmountSum { get; set; }
}
