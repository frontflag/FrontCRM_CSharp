using CRM.Core.Models.Purchase;

namespace CRM.Infrastructure.PurchaseOrders;

internal sealed class PurchaseOrderItemLineJoin
{
    public PurchaseOrderItem Item { get; set; } = null!;
    public PurchaseOrder Po { get; set; } = null!;
    public PurchaseOrderItemExtend? Ext { get; set; }
}
