namespace CRM.Core.Constants;

/// <summary>采购订单明细列表左栏快捷检索（<c>quickFilter</c> / URL <c>preset</c> 业务项）。时间类 preset 由前端展开为 <c>startDate/endDate</c>。</summary>
public static class PurchaseOrderItemListQuickFilterCodes
{
    public const string PendingSubmitAudit = "pending_submit_audit";
    public const string PendingVendorConfirm = "pending_vendor_confirm";
    public const string PendingSubmitPaymentRequest = "pending_submit_payment_request";
    public const string PendingSubmitArrivalNotify = "pending_submit_arrival_notify";
    public const string PayLater = "pay_later";
    public const string ConfirmedUnpaid = "confirmed_unpaid";
    public const string StockedInUnpaid = "stocked_in_unpaid";
    public const string PaymentPartial = "payment_partial";
    public const string PaymentComplete = "payment_complete";
    public const string ConfirmedPendingStockIn = "confirmed_pending_stock_in";
    public const string PaidPendingStockIn = "paid_pending_stock_in";
    public const string StockedIn = "stocked_in";

    public static bool IsKnown(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var c = code.Trim();
        return c is PendingSubmitAudit
            or PendingVendorConfirm
            or PendingSubmitPaymentRequest
            or PendingSubmitArrivalNotify
            or PayLater
            or ConfirmedUnpaid
            or StockedInUnpaid
            or PaymentPartial
            or PaymentComplete
            or ConfirmedPendingStockIn
            or PaidPendingStockIn
            or StockedIn;
    }
}
