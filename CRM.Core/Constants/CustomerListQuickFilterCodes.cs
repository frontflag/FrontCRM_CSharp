namespace CRM.Core.Constants;

/// <summary>客户列表左栏快捷检索（<c>quickFilter</c> / URL <c>preset</c> 业务项）。时间/关注/待办类 preset 由前端展开 query 参数。</summary>
public static class CustomerListQuickFilterCodes
{
    public const string HasDemand = "has_demand";
    public const string DemandLast7Days = "demand_last_7_days";
    public const string DemandLast30Days = "demand_last_30_days";
    public const string DemandStale6m = "demand_stale_6m";
    public const string DemandStale1y = "demand_stale_1y";

    public const string HasDeal = "has_deal";
    public const string DealLast7Days = "deal_last_7_days";
    public const string DealLast30Days = "deal_last_30_days";
    public const string DealStale6m = "deal_stale_6m";
    public const string DealStale1y = "deal_stale_1y";

    public const string PendingShipment = "pending_shipment";
    public const string HasReceivable = "has_receivable";

    public static bool IsKnown(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var c = code.Trim();
        return c is HasDemand
            or DemandLast7Days
            or DemandLast30Days
            or DemandStale6m
            or DemandStale1y
            or HasDeal
            or DealLast7Days
            or DealLast30Days
            or DealStale6m
            or DealStale1y
            or PendingShipment
            or HasReceivable;
    }
}
