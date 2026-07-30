namespace CRM.Core.Constants;

/// <summary>需求明细分析数据集（列表看板 vs 报表范围）。</summary>
public static class RfqItemAnalyticsDatasets
{
    /// <summary>与明细列表搜索栏筛选结果一致（无额外硬过滤）。</summary>
    public const string ListFilter = "listFilter";

    /// <summary>报表范围 + 排除主单已取消。</summary>
    public const string ReportScope = "reportScope";

    public static bool IsReportScope(string? dataset) =>
        string.Equals(dataset, ReportScope, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string? dataset) =>
        IsReportScope(dataset) ? ReportScope : ListFilter;
}
