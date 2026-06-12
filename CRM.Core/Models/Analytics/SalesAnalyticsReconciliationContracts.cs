namespace CRM.Core.Models.Analytics;

public sealed class SalesAnalyticsReconciliationMetricDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal? DashboardValue { get; set; }
    public decimal? BaselineValue { get; set; }
    public decimal? Delta { get; set; }
    public bool Matched { get; set; }
}

public sealed class SalesAnalyticsReconciliationReportDto
{
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public short SaleDataScope { get; set; }
    public string ViewLevel { get; set; } = string.Empty;
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public bool AllMatched { get; set; }
    /// <summary>看板 vs 列表路径（仅个人层且未指定其他业务员时附加）。</summary>
    public bool? ListPathMatched { get; set; }
    public IReadOnlyList<SalesAnalyticsReconciliationMetricDto> Metrics { get; set; } = Array.Empty<SalesAnalyticsReconciliationMetricDto>();
}
