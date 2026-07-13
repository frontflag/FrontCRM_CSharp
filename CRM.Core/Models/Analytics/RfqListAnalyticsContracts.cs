namespace CRM.Core.Models.Analytics;

/// <summary>需求列表看板上下文。</summary>
public sealed class RfqListAnalyticsContextDto
{
    public bool MaskCustomerNames { get; set; }
}

/// <summary>需求列表看板 KPI 快照。</summary>
public sealed class RfqListAnalyticsSnapshotDto
{
    /// <summary>发布需求客户数。</summary>
    public int PublishedCustomerCount { get; set; }

    /// <summary>复询需求客户数（主单数 ≥ 2）。</summary>
    public int RepeatInquiryCustomerCount { get; set; }

    /// <summary>复询需求数：Σ max(0, 该客户主单数 - 1)。</summary>
    public int RepeatInquiryRfqCount { get; set; }

    /// <summary>需求数（主单）。</summary>
    public int RfqCount { get; set; }

    /// <summary>需求明细数。</summary>
    public int RfqItemCount { get; set; }

    /// <summary>成单明细数。</summary>
    public int ConvertedLineCount { get; set; }

    /// <summary>需求成单率（%）；分母为排除查无报价后的明细数。</summary>
    public decimal? ConversionRate { get; set; }
}

public sealed class RfqListAnalyticsDashboardDto
{
    public RfqListAnalyticsContextDto Context { get; set; } = new();
    public RfqListAnalyticsSnapshotDto Snapshot { get; set; } = new();
}

public sealed class RfqListAnalyticsTrendPointDto
{
    public string Period { get; set; } = string.Empty;
    public int CustomerCount { get; set; }
    public int RfqCount { get; set; }
    public int RfqItemCount { get; set; }
}

public sealed class RfqListAnalyticsRankingsDto
{
    public IReadOnlyList<SalesAnalyticsRankingRowDto> CustomerByLineCount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();

    public IReadOnlyList<SalesAnalyticsRankingRowDto> SalesUserByLineCount { get; set; } =
        Array.Empty<SalesAnalyticsRankingRowDto>();
}
