namespace CRM.Core.Models.Analytics;

/// <summary>销售订单列表看板上下文（权限脱敏等）。</summary>
public sealed class SalesOrderListAnalyticsContextDto
{
    public bool MaskAmounts { get; set; }
}

/// <summary>列表看板 KPI 快照（成单口径：status >= 审核通过）。</summary>
public sealed class SalesOrderListAnalyticsSnapshotDto
{
    /// <summary>成单客户数（去重）。</summary>
    public int ApprovedCustomerCount { get; set; }

    /// <summary>复购客户数（成单数 ≥ 2 的客户）。</summary>
    public int RepeatCustomerCount { get; set; }

    /// <summary>成单订单数。</summary>
    public int ApprovedOrderCount { get; set; }

    /// <summary>复购订单数：Σ max(0, 该客户成单数 - 1)。</summary>
    public int RepeatOrderCount { get; set; }

    /// <summary>成单金额（USD，convert_total 合计）。</summary>
    public decimal? ApprovedAmountUsd { get; set; }

    /// <summary>成单原币金额分档（按订单头 currency + total）。</summary>
    public IReadOnlyList<SalesOrderListAnalyticsCurrencyLineDto> CurrencyLines { get; set; } =
        Array.Empty<SalesOrderListAnalyticsCurrencyLineDto>();
}

public sealed class SalesOrderListAnalyticsCurrencyLineDto
{
    public string CurrencyKey { get; set; } = string.Empty;
    public string CurrencyLabel { get; set; } = string.Empty;
    public decimal? OriginalAmount { get; set; }
    public decimal? UsdAmount { get; set; }
}

public sealed class SalesOrderListAnalyticsDashboardDto
{
    public SalesOrderListAnalyticsContextDto Context { get; set; } = new();
    public SalesOrderListAnalyticsSnapshotDto Snapshot { get; set; } = new();
}

public sealed class SalesOrderListAnalyticsTrendPointDto
{
    public string Period { get; set; } = string.Empty;
    public int ApprovedOrderCount { get; set; }
    public decimal? ApprovedAmountUsd { get; set; }
}

public sealed class SalesOrderListAnalyticsRankingsDto
{
    public IReadOnlyList<SalesAnalyticsRankingRowDto> CustomerByAmount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();

    public IReadOnlyList<SalesAnalyticsRankingRowDto> CustomerByOrderCount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();

    public IReadOnlyList<SalesAnalyticsRankingRowDto> CustomerByRepeatOrderCount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();

    public IReadOnlyList<SalesAnalyticsRankingRowDto> SalesUserByAmount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
}
