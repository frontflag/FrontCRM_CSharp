using CRM.Core.Models.Sales;

namespace CRM.Core.Interfaces;

/// <summary>销售订单主表列表：数据库分页与筛选（与 <see cref="ISalesOrderService.GetPagedAsync"/> 配合）。</summary>
public interface ISalesOrderListQuery
{
    Task<PagedResult<SellOrder>> GetPagedAsync(SalesOrderQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>与列表相同筛选条件下的汇总（列表页统计卡片；金额由调用方按权限决定是否返回）。</summary>
    Task<SalesOrderListAggregates> GetAggregatesAsync(SalesOrderQueryRequest request, CancellationToken cancellationToken = default);
}

/// <summary>销售订单列表汇总（全量筛选结果，非仅当前页）。</summary>
public sealed class SalesOrderListAggregates
{
    public int TotalCount { get; set; }
    /// <summary>主状态为新建(1)或待审核(2)的订单数。</summary>
    public int PendingCount { get; set; }
    /// <summary>主状态为审核通过、进行中或完成的订单数。</summary>
    public int ApprovedPlusCount { get; set; }
    /// <summary>筛选范围内主表 <see cref="SellOrder.Total"/> 之和。</summary>
    public decimal TotalAmountSum { get; set; }
}
