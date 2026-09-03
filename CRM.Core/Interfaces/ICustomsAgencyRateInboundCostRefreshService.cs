namespace CRM.Core.Interfaces;

/// <summary>
/// Debug：按输入代理费率局部重算指定报关公司历史报关单费用，并回写报关入库成本。
/// 不改报关公司主数据；跳过手工费率单。
/// </summary>
public interface ICustomsAgencyRateInboundCostRefreshService
{
    Task<CustomsAgencyRateInboundCostRefreshResult> RefreshAsync(
        string customsBrokerId,
        decimal agencyRate,
        string? actingUserId,
        CancellationToken cancellationToken = default);
}

public sealed class CustomsAgencyRateInboundCostRefreshResult
{
    public int TotalDeclarations { get; set; }
    public int SkippedVoided { get; set; }
    public int SkippedManual { get; set; }
    public int SkippedNoFees { get; set; }
    public int RefreshedDeclarations { get; set; }
    public int FeesChangedDeclarations { get; set; }
    public int ArrivalNoticesUpdated { get; set; }
    public int StockInItemsUpdated { get; set; }
    public int StockItemLayersUpdated { get; set; }
    public int FailedCount { get; set; }
    public List<string> RefreshedDeclarationCodes { get; set; } = new();
    public List<string> FailedMessages { get; set; } = new();
}
