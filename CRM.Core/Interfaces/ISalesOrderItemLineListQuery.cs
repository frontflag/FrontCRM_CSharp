namespace CRM.Core.Interfaces;

/// <summary>销售订单明细行列表：数据库分页（与 <c>GET /api/v1/sales-orders/items</c> 配合）。</summary>
public interface ISalesOrderItemLineListQuery
{
    Task<PagedResult<SellOrderItemLineDto>> GetPagedAsync(
        SellOrderItemLineQueryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>按销售明细 Id 批量加载列表行（无数据权限过滤，供业务详情嵌入场景）。</summary>
    Task<List<SellOrderItemLineDto>> GetByIdsAsync(
        IReadOnlyList<string> sellOrderItemIds,
        CancellationToken cancellationToken = default);
}
