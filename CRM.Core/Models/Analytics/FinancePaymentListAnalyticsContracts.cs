namespace CRM.Core.Models.Analytics;

/// <summary>付款记录列表看板上下文（511 时脱敏金额与供应商名）。</summary>
public sealed class FinancePaymentListAnalyticsContextDto
{
    public bool MaskAmounts { get; set; }
}

public sealed class FinancePaymentListAnalyticsCurrencyLineDto
{
    public string CurrencyKey { get; set; } = string.Empty;
    public string CurrencyLabel { get; set; } = string.Empty;
    public decimal? OriginalAmount { get; set; }
}

public sealed class FinancePaymentListAnalyticsSnapshotDto
{
    public int VendorCount { get; set; }
    public int HeaderCount { get; set; }
    public IReadOnlyList<FinancePaymentListAnalyticsCurrencyLineDto> CurrencyLines { get; set; } =
        Array.Empty<FinancePaymentListAnalyticsCurrencyLineDto>();
}

public sealed class FinancePaymentListAnalyticsDashboardDto
{
    public FinancePaymentListAnalyticsContextDto Context { get; set; } = new();
    public FinancePaymentListAnalyticsSnapshotDto Snapshot { get; set; } = new();
}

public sealed class FinancePaymentListAnalyticsTrendCurrencyAmountDto
{
    public string CurrencyKey { get; set; } = string.Empty;
    public string CurrencyLabel { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
}

public sealed class FinancePaymentListAnalyticsTrendPointDto
{
    public string Period { get; set; } = string.Empty;
    public int HeaderCount { get; set; }
    public IReadOnlyList<FinancePaymentListAnalyticsTrendCurrencyAmountDto> AmountsByCurrency { get; set; } =
        Array.Empty<FinancePaymentListAnalyticsTrendCurrencyAmountDto>();
}

/// <summary>分布组：核销状态无币别；采购员按原币分面。</summary>
public sealed class FinancePaymentListAnalyticsBreakdownGroupDto
{
    public string GroupKey { get; set; } = string.Empty;
    public string GroupLabel { get; set; } = string.Empty;
    public string? CurrencyKey { get; set; }
    public string? CurrencyLabel { get; set; }
    public IReadOnlyList<SalesAnalyticsBreakdownItemDto> Items { get; set; } =
        Array.Empty<SalesAnalyticsBreakdownItemDto>();
}

public sealed class FinancePaymentListAnalyticsRankingFacetDto
{
    public string CurrencyKey { get; set; } = string.Empty;
    public string CurrencyLabel { get; set; } = string.Empty;
    public IReadOnlyList<SalesAnalyticsRankingRowDto> Rows { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
}

public sealed class FinancePaymentListAnalyticsRankingsDto
{
    public IReadOnlyList<FinancePaymentListAnalyticsRankingFacetDto> VendorByAmount { get; set; } =
        Array.Empty<FinancePaymentListAnalyticsRankingFacetDto>();
    public IReadOnlyList<FinancePaymentListAnalyticsRankingFacetDto> PurchaseUserByAmount { get; set; } =
        Array.Empty<FinancePaymentListAnalyticsRankingFacetDto>();
}
