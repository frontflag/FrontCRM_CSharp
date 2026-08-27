using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Sales;

namespace CRM.Infrastructure.StockOuts;

/// <summary>出库明细列表筛选联表行（与分页、看板共用）。</summary>
internal sealed class StockOutItemListJoin
{
    public StockOutItem Item { get; set; } = null!;
    public StockOut Header { get; set; } = null!;
    public SellOrderItem? SoLine { get; set; }
    public SellOrder? Order { get; set; }
    public CustomerInfo? HeaderCustomer { get; set; }
    public User? SalesUser { get; set; }
}
