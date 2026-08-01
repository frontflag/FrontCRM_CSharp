namespace CRM.Core.Constants;

/// <summary>报价列表分析数据集（列表看板 vs 报表范围）。</summary>
public static class QuoteAnalyticsDatasets
{
    /// <summary>与报价列表搜索栏筛选结果一致。</summary>
    public const string ListFilter = "listFilter";

    /// <summary>报表范围（透镜 + quote.create_time；不排除主状态）。</summary>
    public const string ReportScope = "reportScope";

    public static bool IsReportScope(string? dataset) =>
        string.Equals(dataset, ReportScope, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string? dataset) =>
        IsReportScope(dataset) ? ReportScope : ListFilter;
}
