namespace CRM.Core.Models.Analytics;

/// <summary>收款记录列表看板上下文（521 时脱敏金额与客户名）。</summary>
public sealed class FinanceReceiptListAnalyticsContextDto
{
    public bool MaskAmounts { get; set; }
}

public sealed class FinanceReceiptListAnalyticsCurrencyLineDto
{
    public string CurrencyKey { get; set; } = string.Empty;
    public string CurrencyLabel { get; set; } = string.Empty;
    public decimal? OriginalAmount { get; set; }
}

public sealed class FinanceReceiptListAnalyticsSnapshotDto
{
    public int CustomerCount { get; set; }
    public int HeaderCount { get; set; }
    public IReadOnlyList<FinanceReceiptListAnalyticsCurrencyLineDto> CurrencyLines { get; set; } =
        Array.Empty<FinanceReceiptListAnalyticsCurrencyLineDto>();
}

public sealed class FinanceReceiptListAnalyticsDashboardDto
{
    public FinanceReceiptListAnalyticsContextDto Context { get; set; } = new();
    public FinanceReceiptListAnalyticsSnapshotDto Snapshot { get; set; } = new();
}

public sealed class FinanceReceiptListAnalyticsTrendCurrencyAmountDto
{
    public string CurrencyKey { get; set; } = string.Empty;
    public string CurrencyLabel { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
}

public sealed class FinanceReceiptListAnalyticsTrendPointDto
{
    public string Period { get; set; } = string.Empty;
    public int HeaderCount { get; set; }
    public IReadOnlyList<FinanceReceiptListAnalyticsTrendCurrencyAmountDto> AmountsByCurrency { get; set; } =
        Array.Empty<FinanceReceiptListAnalyticsTrendCurrencyAmountDto>();
}

/// <summary>分布组：核销状态无币别；业务员按原币分面。</summary>
public sealed class FinanceReceiptListAnalyticsBreakdownGroupDto
{
    public string GroupKey { get; set; } = string.Empty;
    public string GroupLabel { get; set; } = string.Empty;
    public string? CurrencyKey { get; set; }
    public string? CurrencyLabel { get; set; }
    public IReadOnlyList<SalesAnalyticsBreakdownItemDto> Items { get; set; } =
        Array.Empty<SalesAnalyticsBreakdownItemDto>();
}

public sealed class FinanceReceiptListAnalyticsRankingFacetDto
{
    public string CurrencyKey { get; set; } = string.Empty;
    public string CurrencyLabel { get; set; } = string.Empty;
    public IReadOnlyList<SalesAnalyticsRankingRowDto> Rows { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
}

public sealed class FinanceReceiptListAnalyticsRankingsDto
{
    public IReadOnlyList<FinanceReceiptListAnalyticsRankingFacetDto> CustomerByAmount { get; set; } =
        Array.Empty<FinanceReceiptListAnalyticsRankingFacetDto>();
    public IReadOnlyList<FinanceReceiptListAnalyticsRankingFacetDto> SalesUserByAmount { get; set; } =
        Array.Empty<FinanceReceiptListAnalyticsRankingFacetDto>();
}
