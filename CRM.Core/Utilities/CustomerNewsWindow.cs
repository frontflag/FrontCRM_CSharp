namespace CRM.Core.Utilities;

/// <summary>客户新闻动态监测窗口（公司日历 <c>Asia/Shanghai</c>）。</summary>
public static class CustomerNewsWindow
{
    public const int FirstWindowMonths = 3;

    /// <summary>
    /// 首次：近 3 个月，结束日=今天。再次：上次成功简报结束日之后至今天；同日再抓时窗口可退化为当天。
    /// </summary>
    public static (DateOnly Start, DateOnly End) ForFetch(
        DateOnly today,
        DateOnly? lastSuccessPeriodEnd)
    {
        var end = today;
        if (lastSuccessPeriodEnd is null)
            return (today.AddMonths(-FirstWindowMonths), end);

        var start = lastSuccessPeriodEnd.Value.AddDays(1);
        if (start > end)
            start = end;
        return (start, end);
    }
}
