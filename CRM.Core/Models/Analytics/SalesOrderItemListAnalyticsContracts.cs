namespace CRM.Core.Models.Analytics;

/// <summary>销售订单明细列表看板上下文。</summary>
public sealed class SalesOrderItemListAnalyticsContextDto
{
    public bool MaskAmounts { get; set; }
}

public sealed class SalesOrderItemListAnalyticsCurrencyLineDto
{
    public string CurrencyKey { get; set; } = string.Empty;
    public string CurrencyLabel { get; set; } = string.Empty;
    public decimal? OriginalAmount { get; set; }
    public decimal? UsdAmount { get; set; }
}

/// <summary>明细列表看板 KPI（成单口径：主单 status≥审核通过 且明细 status=0）。</summary>
public sealed class SalesOrderItemListAnalyticsSnapshotDto
{
    public int ApprovedCustomerCount { get; set; }
    public int ApprovedOrderCount { get; set; }
    public int ApprovedLineCount { get; set; }
    public decimal? ApprovedAmountUsd { get; set; }
    public IReadOnlyList<SalesOrderItemListAnalyticsCurrencyLineDto> CurrencyLines { get; set; } =
        Array.Empty<SalesOrderItemListAnalyticsCurrencyLineDto>();

    public decimal? PurchaseProfitUsd { get; set; }
    public decimal? OutboundProfitUsd { get; set; }

    public int InStockCustomerCount { get; set; }
    public int InStockLineCount { get; set; }
    public decimal? InStockAmountUsd { get; set; }
    public int? MaxStockAgeDays { get; set; }

    public int ReceivableCustomerCount { get; set; }
    public int ReceivableLineCount { get; set; }
    public decimal? ReceivableAmountUsd { get; set; }
    public IReadOnlyList<SalesOrderItemListAnalyticsCurrencyLineDto> ReceivableCurrencyLines { get; set; } =
        Array.Empty<SalesOrderItemListAnalyticsCurrencyLineDto>();
    public int? MaxReceivableAgeDays { get; set; }
}

public sealed class SalesOrderItemListAnalyticsDashboardDto
{
    public SalesOrderItemListAnalyticsContextDto Context { get; set; } = new();
    public SalesOrderItemListAnalyticsSnapshotDto Snapshot { get; set; } = new();
}

public sealed class SalesOrderItemListAnalyticsTrendPointDto
{
    public string Period { get; set; } = string.Empty;
    public int ApprovedOrderCount { get; set; }
    public int ApprovedLineCount { get; set; }
    public decimal? ApprovedLineAmountUsd { get; set; }
}

public sealed class SalesOrderItemListAnalyticsRankingsDto
{
    public IReadOnlyList<SalesAnalyticsRankingRowDto> CustomerByAmount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
    public IReadOnlyList<SalesAnalyticsRankingRowDto> PnByAmount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
    public IReadOnlyList<SalesAnalyticsRankingRowDto> PnByQty { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
    public IReadOnlyList<SalesAnalyticsRankingRowDto> BrandByAmount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
    public IReadOnlyList<SalesAnalyticsRankingRowDto> BrandByQty { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
    public IReadOnlyList<SalesAnalyticsRankingRowDto> SalesUserByAmount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
}
