namespace CRM.Core.Models.Analytics;

/// <summary>出库明细列表看板上下文。</summary>
public sealed class StockOutItemListAnalyticsContextDto
{
    public bool MaskAmounts { get; set; }

    /// <summary>折算美金回落说明（有金额权限时返回）。</summary>
    public string? ExchangeRateHint { get; set; }
}

public sealed class StockOutItemListAnalyticsCurrencyLineDto
{
    public string CurrencyKey { get; set; } = string.Empty;
    public string CurrencyLabel { get; set; } = string.Empty;
    public decimal? OriginalAmount { get; set; }
    public decimal? UsdAmount { get; set; }
}

public sealed class StockOutItemListAnalyticsSnapshotDto
{
    public int CustomerCount { get; set; }
    public int LineCount { get; set; }
    public decimal? AmountUsd { get; set; }
    public IReadOnlyList<StockOutItemListAnalyticsCurrencyLineDto> CurrencyLines { get; set; } =
        Array.Empty<StockOutItemListAnalyticsCurrencyLineDto>();
}

public sealed class StockOutItemListAnalyticsDashboardDto
{
    public StockOutItemListAnalyticsContextDto Context { get; set; } = new();
    public StockOutItemListAnalyticsSnapshotDto Snapshot { get; set; } = new();
}

public sealed class StockOutItemListAnalyticsTrendPointDto
{
    public string Period { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public decimal? AmountUsd { get; set; }
}

public sealed class StockOutItemListAnalyticsRankingsDto
{
    public IReadOnlyList<SalesAnalyticsRankingRowDto> CustomerByAmount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
    public IReadOnlyList<SalesAnalyticsRankingRowDto> SalesUserByAmount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
}
