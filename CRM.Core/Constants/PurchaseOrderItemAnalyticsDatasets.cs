namespace CRM.Core.Constants;

/// <summary>采购订单明细分析数据集（列表看板 vs 报表成单）。</summary>
public static class PurchaseOrderItemAnalyticsDatasets
{
    /// <summary>与明细列表搜索栏筛选结果一致（不成单约束）。</summary>
    public const string ListFilter = "listFilter";

    /// <summary>报表范围 + 成单（主单 status≥10 且明细未取消）。</summary>
    public const string ReportApproved = "reportApproved";

    public static bool IsReportApproved(string? dataset) =>
        string.Equals(dataset, ReportApproved, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string? dataset) =>
        IsReportApproved(dataset) ? ReportApproved : ListFilter;
}
