namespace CRM.Core.Constants;

/// <summary>供应商列表左栏快捷检索（<c>quickFilter</c> / URL <c>preset</c> 业务项）。时间/关注/待办类 preset 由前端展开 query 参数。</summary>
public static class VendorListQuickFilterCodes
{
    public const string HasQuote = "has_quote";
    public const string QuoteLast7Days = "quote_last_7_days";
    public const string QuoteLast30Days = "quote_last_30_days";
    public const string QuoteStale6m = "quote_stale_6m";
    public const string QuoteStale1y = "quote_stale_1y";

    public const string HasPurchase = "has_purchase";
    public const string PurchaseLast7Days = "purchase_last_7_days";
    public const string PurchaseLast30Days = "purchase_last_30_days";
    public const string PurchaseStale6m = "purchase_stale_6m";
    public const string PurchaseStale1y = "purchase_stale_1y";

    public const string PendingInbound = "pending_inbound";
    public const string HasPayable = "has_payable";

    public static bool IsKnown(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var c = code.Trim();
        return c is HasQuote
            or QuoteLast7Days
            or QuoteLast30Days
            or QuoteStale6m
            or QuoteStale1y
            or HasPurchase
            or PurchaseLast7Days
            or PurchaseLast30Days
            or PurchaseStale6m
            or PurchaseStale1y
            or PendingInbound
            or HasPayable;
    }
}
