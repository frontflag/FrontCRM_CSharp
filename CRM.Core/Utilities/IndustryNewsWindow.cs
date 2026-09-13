namespace CRM.Core.Utilities;

/// <summary>行业新闻简报日期窗口（公司日历 <c>Asia/Shanghai</c>）。</summary>
public static class IndustryNewsWindow
{
    public const int JobHourShanghai = 8;

    /// <summary>结束日 = 昨天；开始日 = 昨天往前 1 天（共两个自然日）。</summary>
    public static (DateOnly Start, DateOnly End) ForBriefingDate(DateOnly briefingDate)
    {
        var end = briefingDate.AddDays(-1);
        var start = end.AddDays(-1);
        return (start, end);
    }

    public static bool CanRunAt(DateTime shanghaiLocal) => shanghaiLocal.Hour >= JobHourShanghai;
}
