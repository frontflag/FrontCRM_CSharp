namespace CRM.Core.Utilities;

/// <summary>库龄分桶，与物流分析 / 库存中心看板一致：0–30 / 31–90 / 91–180 / 181–365 / 365+。</summary>
public static class InventoryAnalyticsAgeBucket
{
    public const string D0To30 = "0_30";
    public const string D31To90 = "31_90";
    public const string D91To180 = "91_180";
    public const string D181To365 = "181_365";
    public const string D365Plus = "365_plus";

    public static string Classify(int ageDays)
    {
        if (ageDays <= 30) return D0To30;
        if (ageDays <= 90) return D31To90;
        if (ageDays <= 180) return D91To180;
        if (ageDays <= 365) return D181To365;
        return D365Plus;
    }

    public static Dictionary<string, (string Label, decimal Qty)> CreateEmpty() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            [D0To30] = ("0–30天", 0m),
            [D31To90] = ("31–90天", 0m),
            [D91To180] = ("91–180天", 0m),
            [D181To365] = ("181–365天", 0m),
            [D365Plus] = ("365天以上", 0m)
        };
}
