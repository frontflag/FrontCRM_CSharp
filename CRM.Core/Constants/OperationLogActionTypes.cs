namespace CRM.Core.Constants;

/// <summary>log_operation.ActionType 常用文案（全系统删除/操作溯源口径一致）。</summary>
public static class OperationLogActionTypes
{
    /// <summary>客户/供应商等主体删除（历史口径）。</summary>
    public const string GenericDelete = "删除";

    public const string SellOrderDelete = "销售订单整单删除";
    public const string SellOrderItemDelete = "销售明细删除";
    public const string SellOrderItemDeleteWithOrder = "销售明细整单删除";

    public const string PurchaseOrderDelete = "采购订单整单删除";
    public const string PurchaseOrderItemDelete = "采购明细删除";
    public const string PurchaseOrderItemDeleteWithOrder = "采购明细整单删除";

    public const string PurchaseRequisitionSoftDelete = "采购申请普通删除";
    public const string PurchaseRequisitionForceDelete = "采购申请强制删除";

    public const string RfqItemDelete = "需求明细删除";
    public const string QuoteItemDelete = "报价明细删除";

    public const string RfqTagApply = "需求打标签";
    public const string RfqTagRemove = "需求移除标签";
}
