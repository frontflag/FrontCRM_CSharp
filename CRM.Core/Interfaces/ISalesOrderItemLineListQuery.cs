namespace CRM.Core.Interfaces;

/// <summary>销售订单明细行列表：数据库分页（与 <c>GET /api/v1/sales-orders/items</c> 配合）。</summary>
public interface ISalesOrderItemLineListQuery
{
    Task<PagedResult<SellOrderItemLineDto>> GetPagedAsync(
        SellOrderItemLineQueryRequest request,
        CancellationToken cancellationToken = default);
}
