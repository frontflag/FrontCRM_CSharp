namespace CRM.Core.Models.Analytics;

/// <summary>采购订单明细列表看板上下文。</summary>
public sealed class PurchaseOrderItemListAnalyticsContextDto
{
    public bool MaskAmounts { get; set; }
}

/// <summary>明细列表看板 KPI（成单口径：主单 status≥10 且明细 status≠-2）。</summary>
public sealed class PurchaseOrderItemListAnalyticsSnapshotDto
{
    public int ApprovedVendorCount { get; set; }
    public int ApprovedOrderCount { get; set; }
    public int ApprovedLineCount { get; set; }
    public decimal? ApprovedAmountUsd { get; set; }
    public IReadOnlyList<PurchaseOrderListAnalyticsCurrencyLineDto> CurrencyLines { get; set; } =
        Array.Empty<PurchaseOrderListAnalyticsCurrencyLineDto>();

    public int InStockVendorCount { get; set; }
    public int InStockLineCount { get; set; }
    public decimal? InStockAmountUsd { get; set; }
    public int? MaxStockAgeDays { get; set; }

    public int PayableVendorCount { get; set; }
    public int PayableLineCount { get; set; }
    public decimal? PayableAmountUsd { get; set; }
    public IReadOnlyList<PurchaseOrderListAnalyticsCurrencyLineDto> PayableCurrencyLines { get; set; } =
        Array.Empty<PurchaseOrderListAnalyticsCurrencyLineDto>();
}

public sealed class PurchaseOrderItemListAnalyticsDashboardDto
{
    public PurchaseOrderItemListAnalyticsContextDto Context { get; set; } = new();
    public PurchaseOrderItemListAnalyticsSnapshotDto Snapshot { get; set; } = new();
}

public sealed class PurchaseOrderItemListAnalyticsTrendPointDto
{
    public string Period { get; set; } = string.Empty;
    public int ApprovedOrderCount { get; set; }
    public int ApprovedLineCount { get; set; }
    public decimal? ApprovedLineAmountUsd { get; set; }
}

public sealed class PurchaseOrderItemListAnalyticsRankingsDto
{
    public IReadOnlyList<SalesAnalyticsRankingRowDto> VendorByAmount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
    public IReadOnlyList<SalesAnalyticsRankingRowDto> PnByAmount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
    public IReadOnlyList<SalesAnalyticsRankingRowDto> PnByQty { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
    public IReadOnlyList<SalesAnalyticsRankingRowDto> BrandByAmount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
    public IReadOnlyList<SalesAnalyticsRankingRowDto> BrandByQty { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
    public IReadOnlyList<SalesAnalyticsRankingRowDto> PurchaseUserByAmount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
}
