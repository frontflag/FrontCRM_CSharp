namespace CRM.Core.Models.Sales;

/// <summary>
/// 销售单明细扩展刷新结果（用于前端提示是否有更新数据）。
/// </summary>
public class SalesOrderItemExtendRefreshResult
{
    public string SalesOrderId { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int ChangedItems { get; set; }
    public int ChangedFieldsCount { get; set; }
    public int SyncedStockOutNotifyStatusCount { get; set; }
    public DateTime RefreshedAt { get; set; } = DateTime.UtcNow;
    public List<SalesOrderItemExtendChangeDto> Changes { get; set; } = new();

    public int PackingItemExtendsUpdated { get; set; }
    public int StockItemsUpdated { get; set; }
    public int StockOutItemExtendsUpdated { get; set; }
    public int StockOutHeadersUpdated { get; set; }
    public int ReceivablesUpdated { get; set; }
    public List<SalesOrderSalesPriceLineChangeDto> SalesPriceLineChanges { get; set; } = new();
    public List<SalesOrderReceivableAmountWarningDto> ReceivableWarnings { get; set; } = new();
}

public class SalesOrderSalesPriceLineChangeDto
{
    public string SellOrderItemId { get; set; } = string.Empty;
    public string? SellOrderItemCode { get; set; }
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public short OldCurrency { get; set; }
    public short NewCurrency { get; set; }
    public decimal OldConvertPrice { get; set; }
    public decimal NewConvertPrice { get; set; }
}

public class SalesOrderReceivableAmountWarningDto
{
    public string ReceivableId { get; set; } = string.Empty;
    public string? ReceivableCode { get; set; }
    public string SellOrderItemId { get; set; } = string.Empty;
    public string? SellOrderItemCode { get; set; }
    public decimal Amount { get; set; }
    public decimal VerifiedDone { get; set; }
    public decimal VerifiedToBe { get; set; }
    public decimal InvoiceMatchDone { get; set; }
    public decimal InvoiceMatchToBe { get; set; }
    public bool VerifiedOverAmount { get; set; }
    public bool InvoiceMatchOverAmount { get; set; }
}

public class SalesOrderItemExtendChangeDto
{
    public string SellOrderItemId { get; set; } = string.Empty;
    public string? SellOrderItemCode { get; set; }
    public List<SalesOrderItemExtendFieldChangeDto> Fields { get; set; } = new();
}

public class SalesOrderItemExtendFieldChangeDto
{
    public string Field { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Before { get; set; } = string.Empty;
    public string After { get; set; } = string.Empty;
}

