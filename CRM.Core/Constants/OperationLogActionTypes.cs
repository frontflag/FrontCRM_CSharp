namespace CRM.Core.Constants;

/// <summary>log_operation.ActionType 常用文案（全系统删除/操作溯源口径一致）。</summary>
public static class OperationLogActionTypes
{
    /// <summary>客户/供应商等主体删除（历史口径）。</summary>
    public const string GenericDelete = "删除";

    public const string SellOrderDelete = "销售订单整单删除";
    public const string SellOrderRefreshSalesPrice = "刷新销售价";
    public const string SellOrderItemDelete = "销售明细删除";
    public const string SellOrderItemDeleteWithOrder = "销售明细整单删除";

    public const string PurchaseOrderDelete = "采购订单整单删除";
    public const string PurchaseOrderRefreshPurchasePrice = "刷新采购价";
    public const string PurchaseOrderChangeVendor = "更换供应商";
    public const string PurchaseOrderItemDelete = "采购明细删除";
    public const string PurchaseOrderItemDeleteWithOrder = "采购明细整单删除";

    public const string PurchaseRequisitionSoftDelete = "采购申请普通删除";
    public const string PurchaseRequisitionForceDelete = "采购申请强制删除";

    public const string RfqItemDelete = "需求明细删除";
    /// <summary>与 <see cref="DeleteLogEntityNames.Rfq"/> +「删除」拼出的 ActionType 一致。</summary>
    public const string RfqHeaderDelete = "询价需求删除";
    public const string RfqHeaderForceDelete = "询价需求强制删除";
    public const string RfqRestore = "询价需求恢复";
    public const string QuoteItemDelete = "报价明细删除";

    public const string RfqTagApply = "需求打标签";
    public const string RfqTagRemove = "需求移除标签";

    /// <summary>付款单反核销（付款完成回滚至审核通过）。</summary>
    public const string FinancePaymentReverseVerification = "付款反核销";

    /// <summary>收款单确认（新建 → 确认）。</summary>
    public const string FinanceReceiptConfirm = "收款确认";

    /// <summary>收款单取消。</summary>
    public const string FinanceReceiptCancel = "收款取消";

    /// <summary>收款单反核销（撤销核销流水，主单状态不变）。</summary>
    public const string FinanceReceiptReverseVerification = "收款反核销";

    /// <summary>进项发票反核销（软删核销流水，开票/认证状态不变）。</summary>
    public const string FinancePurchaseInvoiceReverseVerification = "进项发票反核销";
}
