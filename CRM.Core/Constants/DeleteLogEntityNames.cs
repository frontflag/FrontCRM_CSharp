namespace CRM.Core.Constants;

/// <summary>删除操作日志中业务实体中文显示名（与 ActionType 文案一致）。</summary>
public static class DeleteLogEntityNames
{
    public const string Customer = "客户";
    public const string CustomerContact = "客户联系人";
    public const string CustomerAddress = "客户地址";
    public const string CustomerBank = "客户银行信息";
    public const string CustomerContactHistory = "客户联系历史";

    public const string Vendor = "供应商";
    public const string VendorContact = "供应商联系人";
    public const string VendorAddress = "供应商地址";
    public const string VendorBank = "供应商银行信息";
    public const string VendorContactHistory = "供应商联系历史";

    public const string Quote = "报价单";
    public const string QuoteItem = "报价明细";
    public const string Rfq = "询价需求";
    public const string RfqItem = "需求明细";
    public const string SalesOrder = "销售订单";
    public const string SellOrderItem = "销售订单明细";
    public const string PurchaseOrder = "采购订单";
    public const string PurchaseOrderItem = "采购订单明细";
    public const string PurchaseRequisition = "采购申请";

    public const string StockIn = "入库单";
    public const string StockOutRequest = "出库通知";
    public const string StockOut = "出库单";
    public const string Packing = "装箱单";
    public const string ArrivalNotice = "到货通知";
    public const string QcInspection = "质检单";
    public const string InventoryStock = "库存明细";
    public const string PickingTask = "拣货单";

    public const string FinancePayment = "付款单";
    public const string FinanceReceipt = "收款单";
    public const string FinanceSellInvoice = "销项发票";
    public const string FinancePurchaseInvoice = "进项发票";

    public const string CustomsDeclaration = "报关单";
    public const string CustomsPendlist = "待报关记录";
    public const string CustomsBroker = "报关公司";
    public const string FreightForwarderCompany = "货代公司";
    public const string Document = "附件文档";
}
