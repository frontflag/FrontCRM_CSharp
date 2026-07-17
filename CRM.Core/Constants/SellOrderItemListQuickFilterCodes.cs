namespace CRM.Core.Constants;

/// <summary>销售订单明细列表左栏快捷检索（<c>quickFilter</c> / URL <c>preset</c> 业务项）。时间类 preset 由前端展开为 <c>orderCreateStart/End</c>。</summary>
public static class SellOrderItemListQuickFilterCodes
{
    public const string PendingSubmitAudit = "pending_submit_audit";
    public const string PendingSubmitPurchaseReq = "pending_submit_purchase_req";
    public const string PendingSubmitStockOutNotify = "pending_submit_stock_out_notify";

    public const string AppliedPendingPo = "applied_pending_po";
    public const string PurchasedPendingStockIn = "purchased_pending_stock_in";
    public const string NotifyPendingPacking = "notify_pending_packing";
    public const string PackedPendingStockOut = "packed_pending_stock_out";
    public const string InStockPendingOut = "in_stock_pending_out";
    public const string UsedStocking = "used_stocking";
    public const string StockOutPendingReceipt = "stock_out_pending_receipt";
    public const string ReceiptPartial = "receipt_partial";
    public const string ReceiptComplete = "receipt_complete";

    public static bool IsKnown(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var c = code.Trim();
        return c is PendingSubmitAudit
            or PendingSubmitPurchaseReq
            or PendingSubmitStockOutNotify
            or AppliedPendingPo
            or PurchasedPendingStockIn
            or NotifyPendingPacking
            or PackedPendingStockOut
            or InStockPendingOut
            or UsedStocking
            or StockOutPendingReceipt
            or ReceiptPartial
            or ReceiptComplete;
    }
}
