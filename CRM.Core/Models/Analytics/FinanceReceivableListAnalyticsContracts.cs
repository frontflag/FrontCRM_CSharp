namespace CRM.Core.Models.Analytics;

/// <summary>应收款列表看板上下文（有 finance-receipt.read 即可看金额，不脱敏）。</summary>
public sealed class FinanceReceivableListAnalyticsContextDto
{
    public bool MaskAmounts { get; set; }
    /// <summary>美元折算说明。</summary>
    public string? ExchangeRateHint { get; set; }
}

public sealed class FinanceReceivableListAnalyticsCurrencyLineDto
{
    public string CurrencyKey { get; set; } = string.Empty;
    public string CurrencyLabel { get; set; } = string.Empty;
    public decimal? OriginalAmount { get; set; }
    public decimal? UsdAmount { get; set; }
}

public sealed class FinanceReceivableListAnalyticsSnapshotDto
{
    public int CustomerCount { get; set; }
    public int LineCount { get; set; }

    public decimal? PendingAmountUsd { get; set; }
    public IReadOnlyList<FinanceReceivableListAnalyticsCurrencyLineDto> PendingCurrencyLines { get; set; } =
        Array.Empty<FinanceReceivableListAnalyticsCurrencyLineDto>();

    public decimal? TotalAmountUsd { get; set; }
    public IReadOnlyList<FinanceReceivableListAnalyticsCurrencyLineDto> TotalCurrencyLines { get; set; } =
        Array.Empty<FinanceReceivableListAnalyticsCurrencyLineDto>();

    public int? MaxReceivableAgeDays { get; set; }
}

public sealed class FinanceReceivableListAnalyticsDashboardDto
{
    public FinanceReceivableListAnalyticsContextDto Context { get; set; } = new();
    public FinanceReceivableListAnalyticsSnapshotDto Snapshot { get; set; } = new();
}

public sealed class FinanceReceivableListAnalyticsTrendPointDto
{
    public string Period { get; set; } = string.Empty;
    public int CustomerCount { get; set; }
    public int LineCount { get; set; }
    public decimal? PendingAmountUsd { get; set; }
    public decimal? TotalAmountUsd { get; set; }
}

/// <summary>分布项：待核销与总额双值；账期组仅 Pending 有意义。</summary>
public sealed class FinanceReceivableListAnalyticsBreakdownItemDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal PendingValue { get; set; }
    public decimal TotalValue { get; set; }
    public decimal PendingRatio { get; set; }
    public decimal TotalRatio { get; set; }
}

public sealed class FinanceReceivableListAnalyticsBreakdownGroupDto
{
    public string GroupKey { get; set; } = string.Empty;
    public string GroupLabel { get; set; } = string.Empty;
    /// <summary>账期组为 true：前端金额模式切换时仍用 PendingValue。</summary>
    public bool AgingPendingOnly { get; set; }
    public IReadOnlyList<FinanceReceivableListAnalyticsBreakdownItemDto> Items { get; set; } =
        Array.Empty<FinanceReceivableListAnalyticsBreakdownItemDto>();
}

public sealed class FinanceReceivableListAnalyticsRankingRowDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? PendingAmountUsd { get; set; }
    public decimal? TotalAmountUsd { get; set; }
    public int OrderCount { get; set; }
    /// <summary>单笔排行时标注核销状态 0/1/2。</summary>
    public short? VerificationStatus { get; set; }
}

public sealed class FinanceReceivableListAnalyticsRankingsDto
{
    public IReadOnlyList<FinanceReceivableListAnalyticsRankingRowDto> ReceivableByTotalAmount { get; set; } =
        Array.Empty<FinanceReceivableListAnalyticsRankingRowDto>();
    public IReadOnlyList<FinanceReceivableListAnalyticsRankingRowDto> CustomerByAmount { get; set; } =
        Array.Empty<FinanceReceivableListAnalyticsRankingRowDto>();
    public IReadOnlyList<FinanceReceivableListAnalyticsRankingRowDto> SalesUserByAmount { get; set; } =
        Array.Empty<FinanceReceivableListAnalyticsRankingRowDto>();
}
