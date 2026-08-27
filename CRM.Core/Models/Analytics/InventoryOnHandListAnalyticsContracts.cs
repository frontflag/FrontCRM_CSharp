namespace CRM.Core.Models.Analytics;

/// <summary>库存中心列表看板上下文。</summary>
public sealed class InventoryOnHandListAnalyticsContextDto
{
    public bool MaskAmounts { get; set; }
}

public sealed class InventoryOnHandListAnalyticsCurrencyLineDto
{
    public string CurrencyKey { get; set; } = string.Empty;
    public string CurrencyLabel { get; set; } = string.Empty;
    public decimal? OriginalAmount { get; set; }
}

public sealed class InventoryOnHandListAnalyticsSnapshotDto
{
    public int OnHandQty { get; set; }
    public IReadOnlyList<InventoryOnHandListAnalyticsCurrencyLineDto> CurrencyLines { get; set; } =
        Array.Empty<InventoryOnHandListAnalyticsCurrencyLineDto>();
    /// <summary>加权平均库龄（天）；无入库日层不参与。</summary>
    public decimal? WeightedAvgAgeDays { get; set; }
    public int StagnantLayerCount { get; set; }
}

public sealed class InventoryOnHandListAnalyticsDashboardDto
{
    public InventoryOnHandListAnalyticsContextDto Context { get; set; } = new();
    public InventoryOnHandListAnalyticsSnapshotDto Snapshot { get; set; } = new();
}

public sealed class InventoryOnHandListAnalyticsTrendCurrencyAmountDto
{
    public string CurrencyKey { get; set; } = string.Empty;
    public string CurrencyLabel { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
}

public sealed class InventoryOnHandListAnalyticsTrendPointDto
{
    public string Period { get; set; } = string.Empty;
    public int OnHandQty { get; set; }
    public IReadOnlyList<InventoryOnHandListAnalyticsTrendCurrencyAmountDto> AmountsByCurrency { get; set; } =
        Array.Empty<InventoryOnHandListAnalyticsTrendCurrencyAmountDto>();
}

/// <summary>分布组：<c>CurrencyKey</c> 为空表示按数量；否则为该原币金额。</summary>
public sealed class InventoryOnHandListAnalyticsBreakdownGroupDto
{
    public string GroupKey { get; set; } = string.Empty;
    public string GroupLabel { get; set; } = string.Empty;
    public string? CurrencyKey { get; set; }
    public string? CurrencyLabel { get; set; }
    public IReadOnlyList<SalesAnalyticsBreakdownItemDto> Items { get; set; } =
        Array.Empty<SalesAnalyticsBreakdownItemDto>();
}

public sealed class InventoryOnHandListAnalyticsRankingFacetDto
{
    public string CurrencyKey { get; set; } = string.Empty;
    public string CurrencyLabel { get; set; } = string.Empty;
    public IReadOnlyList<SalesAnalyticsRankingRowDto> Rows { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
}

public sealed class InventoryOnHandListAnalyticsRankingsDto
{
    public IReadOnlyList<SalesAnalyticsRankingRowDto> CustomerByQty { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
    public IReadOnlyList<SalesAnalyticsRankingRowDto> SalesUserByQty { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
    public IReadOnlyList<SalesAnalyticsRankingRowDto> MaterialByQty { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
    public IReadOnlyList<SalesAnalyticsRankingRowDto> BrandByQty { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
    public IReadOnlyList<InventoryOnHandListAnalyticsRankingFacetDto> CustomerByAmount { get; set; } =
        Array.Empty<InventoryOnHandListAnalyticsRankingFacetDto>();
    public IReadOnlyList<InventoryOnHandListAnalyticsRankingFacetDto> SalesUserByAmount { get; set; } =
        Array.Empty<InventoryOnHandListAnalyticsRankingFacetDto>();
    public IReadOnlyList<InventoryOnHandListAnalyticsRankingFacetDto> MaterialByAmount { get; set; } =
        Array.Empty<InventoryOnHandListAnalyticsRankingFacetDto>();
    public IReadOnlyList<InventoryOnHandListAnalyticsRankingFacetDto> BrandByAmount { get; set; } =
        Array.Empty<InventoryOnHandListAnalyticsRankingFacetDto>();
}
