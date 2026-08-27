namespace CRM.Core.Models.Analytics;

/// <summary>入库单列表看板上下文。</summary>
public sealed class StockInListAnalyticsContextDto
{
    public bool MaskAmounts { get; set; }

    /// <summary>折算美金回落说明（有金额权限时返回）。</summary>
    public string? ExchangeRateHint { get; set; }
}

public sealed class StockInListAnalyticsCurrencyLineDto
{
    public string CurrencyKey { get; set; } = string.Empty;
    public string CurrencyLabel { get; set; } = string.Empty;
    public decimal? OriginalAmount { get; set; }
    public decimal? UsdAmount { get; set; }
}

public sealed class StockInListAnalyticsSnapshotDto
{
    public int VendorCount { get; set; }
    public int HeaderCount { get; set; }
    public decimal? AmountUsd { get; set; }
    public IReadOnlyList<StockInListAnalyticsCurrencyLineDto> CurrencyLines { get; set; } =
        Array.Empty<StockInListAnalyticsCurrencyLineDto>();
}

public sealed class StockInListAnalyticsDashboardDto
{
    public StockInListAnalyticsContextDto Context { get; set; } = new();
    public StockInListAnalyticsSnapshotDto Snapshot { get; set; } = new();
}

public sealed class StockInListAnalyticsTrendPointDto
{
    public string Period { get; set; } = string.Empty;
    public int HeaderCount { get; set; }
    public decimal? AmountUsd { get; set; }
}

public sealed class StockInListAnalyticsRankingsDto
{
    public IReadOnlyList<SalesAnalyticsRankingRowDto> VendorByAmount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
    public IReadOnlyList<SalesAnalyticsRankingRowDto> PurchaseUserByAmount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
}
