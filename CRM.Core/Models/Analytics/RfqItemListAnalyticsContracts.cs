namespace CRM.Core.Models.Analytics;

/// <summary>需求明细列表看板排行（扩展主列表看板）。</summary>
public sealed class RfqItemListAnalyticsRankingsDto
{
    public IReadOnlyList<SalesAnalyticsRankingRowDto> CustomerByLineCount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();

    public IReadOnlyList<SalesAnalyticsRankingRowDto> SalesUserByLineCount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();

    public IReadOnlyList<SalesAnalyticsRankingRowDto> MpnByLineCount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();

    public IReadOnlyList<SalesAnalyticsRankingRowDto> MpnByQty { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();

    public IReadOnlyList<SalesAnalyticsRankingRowDto> BrandByLineCount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();

    public IReadOnlyList<SalesAnalyticsRankingRowDto> BrandByQty { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
}
