namespace CRM.Core.Models.Analytics;

/// <summary>报价列表看板上下文。</summary>
public sealed class QuoteListAnalyticsContextDto
{
    public bool MaskCustomerNames { get; set; }
    public bool MaskVendorNames { get; set; }
}

public sealed class QuoteListAnalyticsSnapshotDto
{
    public int QuoteVendorCount { get; set; }
    public int ValidQuoteCount { get; set; }
    public int NoQuoteFoundItemCount { get; set; }
    public decimal? RfqQuoteRate { get; set; }
    public decimal? AvgResponseMinutes { get; set; }
    public decimal? AvgQuotesPerRfqItem { get; set; }
    public int ConvertedLineCount { get; set; }
    public decimal? QuoteConversionRate { get; set; }
}

public sealed class QuoteListAnalyticsDashboardDto
{
    public QuoteListAnalyticsContextDto Context { get; set; } = new();
    public QuoteListAnalyticsSnapshotDto Snapshot { get; set; } = new();
}

public sealed class QuoteListAnalyticsTrendPointDto
{
    public string Period { get; set; } = string.Empty;
    public int QuoteVendorCount { get; set; }
    public int RfqItemCount { get; set; }
    public int TotalDemandCount { get; set; }
    public int ValidQuoteCount { get; set; }
}

public sealed class QuoteListAnalyticsRankingsDto
{
    public IReadOnlyList<SalesAnalyticsRankingRowDto> VendorByRfqItemCount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();

    public IReadOnlyList<SalesAnalyticsRankingRowDto> PurchaserByQuoteCount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();

    public IReadOnlyList<SalesAnalyticsRankingRowDto> PurchaserByQuoteRate { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();

    public IReadOnlyList<SalesAnalyticsRankingRowDto> MpnByQuoteCount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();

    public IReadOnlyList<SalesAnalyticsRankingRowDto> MpnByQty { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();

    public IReadOnlyList<SalesAnalyticsRankingRowDto> BrandByQuoteCount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();

    public IReadOnlyList<SalesAnalyticsRankingRowDto> BrandByQty { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
}
