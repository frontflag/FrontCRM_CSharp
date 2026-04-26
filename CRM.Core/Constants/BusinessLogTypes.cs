namespace CRM.Core.Constants;

/// <summary>
/// 统一操作日志 / 字段变更日志中的业务类型（log_operation.BizType、log_change_fldval.BizType）。
/// </summary>
public static class BusinessLogTypes
{
    public const string Customer = "Customer";
    public const string CustomerContact = "CustomerContact";
    public const string Vendor = "Vendor";
    public const string VendorContact = "VendorContact";
    public const string SalesOrder = "SalesOrder";
    public const string PurchaseOrder = "PurchaseOrder";
    public const string PurchaseRequisition = "PurchaseRequisition";

    /// <summary>库存聚合（库存中心 stock）</summary>
    public const string InventoryStock = "InventoryStock";

    /// <summary>拣货单（库存中心 picking_task）</summary>
    public const string PickingTask = "PickingTask";

    public const string StockIn = "StockIn";
    public const string StockOut = "StockOut";
    public const string QcInspection = "QcInspection";
    public const string CustomsDeclaration = "CustomsDeclaration";
}
