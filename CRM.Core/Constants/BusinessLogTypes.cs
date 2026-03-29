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
}
