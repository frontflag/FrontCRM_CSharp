namespace CRM.Core.Constants;

/// <summary>导出审计 ExtraInfo.exportKind 取值（入库/出库/库存 CSV 导出）。</summary>
public static class ExportAuditKinds
{
    public const string StockInList = "stockInList";
    public const string StockOutList = "stockOutList";
    public const string InventoryStockList = "inventoryStockList";
    public const string InventoryStockItemList = "inventoryStockItemList";

    public const string BatchReconciliationIn = "batchReconciliationIn";
    public const string BatchReconciliationOut = "batchReconciliationOut";
    public const string StockInBatch = "stockInBatch";
    public const string StockOutBatch = "stockOutBatch";
    public const string PurchaseOrderStockInBatch = "purchaseOrderStockInBatch";
    public const string SalesOrderStockOutBatch = "salesOrderStockOutBatch";

    public const string FinancePaymentList = "financePaymentList";
    public const string FinancePurchaseInvoiceList = "financePurchaseInvoiceList";
    public const string FinanceReceivableList = "financeReceivableList";
    public const string FinanceCustomerAdvanceList = "financeCustomerAdvanceList";
    public const string FinanceReceiptList = "financeReceiptList";
    public const string FinanceSellInvoiceList = "financeSellInvoiceList";
    public const string FinanceFfPayableList = "financeFfPayableList";
}

/// <summary>列表级 / 批次核销导出 ActionType。</summary>
public static class InventoryExportActionTypes
{
    public const string StockInListExport = "StockInListExport";
    public const string StockOutListExport = "StockOutListExport";
    public const string InventoryStockListExport = "InventoryStockListExport";
    public const string InventoryStockItemListExport = "InventoryStockItemListExport";
    public const string BatchReconciliationInExport = "BatchReconciliationInExport";
    public const string BatchReconciliationOutExport = "BatchReconciliationOutExport";
}

/// <summary>财务列表导出 ActionType。</summary>
public static class FinanceExportActionTypes
{
    public const string PaymentListExport = "FinancePaymentListExport";
    public const string PurchaseInvoiceListExport = "FinancePurchaseInvoiceListExport";
    public const string ReceivableListExport = "FinanceReceivableListExport";
    public const string CustomerAdvanceListExport = "FinanceCustomerAdvanceListExport";
    public const string ReceiptListExport = "FinanceReceiptListExport";
    public const string SellInvoiceListExport = "FinanceSellInvoiceListExport";
    public const string FfPayableListExport = "FinanceFfPayableListExport";
}

/// <summary>批次导出请求上的来源标识（前端显式传入，避免仅凭筛选条件误挂单据）。</summary>
public static class BatchExportSources
{
    public const string List = "list";
    public const string StockIn = "stockIn";
    public const string Packing = "packing";
    public const string PurchaseOrder = "purchaseOrder";
    public const string SalesOrder = "salesOrder";
}
