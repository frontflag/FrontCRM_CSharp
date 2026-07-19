namespace CRM.Core.Constants;

/// <summary>到货通知列表左栏 preset（URL <c>preset</c> / API 解析）。</summary>
public static class ArrivalNoticeListQuickFilterCodes
{
    public const string OverdueAll = "overdue_all";
    public const string Overdue1Day = "overdue_1_day";
    public const string Overdue3Days = "overdue_3_days";
    public const string Overdue1Week = "overdue_1_week";

    public const string ExpectedToday = "expected_today";
    public const string ExpectedTomorrow = "expected_tomorrow";
    public const string ExpectedWithin3Days = "expected_within_3_days";
    public const string ExpectedWithin7Days = "expected_within_7_days";

    public const string NotArrived = "not_arrived";
    public const string ArrivedToday = "arrived_today";
    public const string ArrivedTodayYesterday = "arrived_today_yesterday";
    public const string ArrivedWithin3Days = "arrived_within_3_days";
    public const string ArrivedWithin7Days = "arrived_within_7_days";
    public const string ArrivedWithin30Days = "arrived_within_30_days";

    public const string TypePurchase = "type_purchase";
    public const string TypeCustoms = "type_customs";

    public const string TodoPendingQc = "todo_pending_qc";
    public const string TodoPendingStockIn = "todo_pending_stock_in";

    public const string StatusQcDone = "status_qc_done";
    public const string StatusStockedIn = "status_stocked_in";

    public static bool IsKnown(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var c = code.Trim();
        return c is OverdueAll or Overdue1Day or Overdue3Days or Overdue1Week
            or ExpectedToday or ExpectedTomorrow or ExpectedWithin3Days or ExpectedWithin7Days
            or NotArrived or ArrivedToday or ArrivedTodayYesterday or ArrivedWithin3Days
            or ArrivedWithin7Days or ArrivedWithin30Days
            or TypePurchase or TypeCustoms
            or TodoPendingQc or TodoPendingStockIn
            or StatusQcDone or StatusStockedIn;
    }
}
