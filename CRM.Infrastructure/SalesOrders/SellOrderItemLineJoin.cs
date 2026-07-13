using CRM.Core.Models.Sales;

namespace CRM.Infrastructure.SalesOrders;

/// <summary>销售订单明细列表筛选联表行（item + 主单）。</summary>
internal sealed class SellOrderItemLineJoin
{
    public SellOrderItem Item { get; set; } = null!;
    public SellOrder So { get; set; } = null!;
}
