namespace CRM.Core.Models.Analytics;

/// <summary>采购订单列表看板上下文（权限脱敏等）。</summary>
public sealed class PurchaseOrderListAnalyticsContextDto
{
    public bool MaskAmounts { get; set; }
}

/// <summary>列表看板 KPI 快照（成单口径：status >= 审核通过）。</summary>
public sealed class PurchaseOrderListAnalyticsSnapshotDto
{
    /// <summary>成单供应商数（去重）。</summary>
    public int ApprovedVendorCount { get; set; }

    /// <summary>复采供应商数（成单数 ≥ 2 的供应商）。</summary>
    public int RepeatVendorCount { get; set; }

    /// <summary>成单订单数。</summary>
    public int ApprovedOrderCount { get; set; }

    /// <summary>复采订单数：Σ max(0, 该供应商成单数 - 1)。</summary>
    public int RepeatOrderCount { get; set; }

    /// <summary>成单金额（USD，convert_total 合计）。</summary>
    public decimal? ApprovedAmountUsd { get; set; }

    /// <summary>成单原币金额分档（按订单头 currency + total）。</summary>
    public IReadOnlyList<PurchaseOrderListAnalyticsCurrencyLineDto> CurrencyLines { get; set; } =
        Array.Empty<PurchaseOrderListAnalyticsCurrencyLineDto>();
}

public sealed class PurchaseOrderListAnalyticsCurrencyLineDto
{
    public string CurrencyKey { get; set; } = string.Empty;
    public string CurrencyLabel { get; set; } = string.Empty;
    public decimal? OriginalAmount { get; set; }
    public decimal? UsdAmount { get; set; }
}

public sealed class PurchaseOrderListAnalyticsDashboardDto
{
    public PurchaseOrderListAnalyticsContextDto Context { get; set; } = new();
    public PurchaseOrderListAnalyticsSnapshotDto Snapshot { get; set; } = new();
}

public sealed class PurchaseOrderListAnalyticsTrendPointDto
{
    public string Period { get; set; } = string.Empty;
    public int ApprovedOrderCount { get; set; }
    public decimal? ApprovedAmountUsd { get; set; }
}

public sealed class PurchaseOrderListAnalyticsRankingsDto
{
    public IReadOnlyList<SalesAnalyticsRankingRowDto> VendorByAmount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();

    public IReadOnlyList<SalesAnalyticsRankingRowDto> VendorByOrderCount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();

    public IReadOnlyList<SalesAnalyticsRankingRowDto> VendorByRepeatOrderCount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();

    public IReadOnlyList<SalesAnalyticsRankingRowDto> PurchaseUserByAmount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
}
