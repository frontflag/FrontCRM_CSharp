namespace CRM.Core.Models.Ai;

public static class AiDataQueryIntents
{
    public const string Performance = "performance";
    public const string SalesTrend = "salesTrend";
    public const string InventoryAmount = "inventoryAmount";
    public const string Backlog = "backlog";
    public const string ReceivableTotal = "receivableTotal";
    public const string ReceivableCustomers = "receivableCustomers";
    public const string TopBrands = "topBrands";
    public const string OrderTrend = "orderTrend";
}

public static class AiDataQuerySlotNames
{
    public const string Metric = "metric";
    public const string Period = "period";
    public const string Measure = "measure";
    public const string Basis = "basis";
    public const string AgeDays = "ageDays";
    public const string Dimension = "dimension";
    public const string TopN = "topN";
}

public static class AiDataQueryKinds
{
    public const string Clarify = "clarify";
    public const string Result = "result";
    public const string Unsupported = "unsupported";
}

/// <summary>本轮数据查询已确认的槽位。缺必填槽时不查数。</summary>
public sealed class AiDataQueryState
{
    public string? Intent { get; set; }
    /// <summary>stockOutAmount / orderAmount / profit / orderCount。</summary>
    public string? Metric { get; set; }
    /// <summary>amount / qty。</summary>
    public string? Measure { get; set; }
    /// <summary>stockOut / order。</summary>
    public string? Basis { get; set; }
    public int? AgeDays { get; set; }
    /// <summary>brand / customer / both。</summary>
    public string? Dimension { get; set; }
    public int? TopN { get; set; }
    /// <summary>today / thisMonth / lastMonth / last3Months / last6Months。</summary>
    public string? Period { get; set; }
    public string? PendingSlot { get; set; }
    public int MissCount { get; set; }
}

public sealed class AiDataQueryOptionDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public sealed class AiDataQueryPointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal? Value { get; set; }
}

public sealed class AiDataQueryChartDto
{
    /// <summary>number / line / bar。</summary>
    public string Type { get; set; } = "number";
    public string Unit { get; set; } = string.Empty;
    public IReadOnlyList<AiDataQueryPointDto> Points { get; set; } = Array.Empty<AiDataQueryPointDto>();
}

public sealed class AiDataQueryRowDto
{
    public string Label { get; set; } = string.Empty;
    public string? ValueText { get; set; }
    public string? RouteName { get; set; }
    public string? RouteQueryKey { get; set; }
    public string? RouteQueryValue { get; set; }
}

public sealed class AiDataQueryLinkDto
{
    public string Label { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public string? QueryKey { get; set; }
    public string? QueryValue { get; set; }
}

public sealed class AiDataQueryRequest
{
    public string? Question { get; set; }
    public AiDataQueryState? State { get; set; }
}

public sealed class AiDataQueryResponse
{
    public string Kind { get; set; } = AiDataQueryKinds.Unsupported;
    public string Summary { get; set; } = string.Empty;
    public string? BasisNote { get; set; }
    public AiDataQueryState State { get; set; } = new();
    public IReadOnlyList<AiDataQueryOptionDto> Options { get; set; } = Array.Empty<AiDataQueryOptionDto>();
    public AiDataQueryChartDto? Chart { get; set; }
    public IReadOnlyList<AiDataQueryRowDto> Rows { get; set; } = Array.Empty<AiDataQueryRowDto>();
    public AiDataQueryLinkDto? Link { get; set; }
}

public sealed class AiDataQueryPlan
{
    public string Kind { get; set; } = AiDataQueryKinds.Unsupported;
    public string Summary { get; set; } = string.Empty;
    public AiDataQueryState State { get; set; } = new();
    public IReadOnlyList<AiDataQueryOptionDto> Options { get; set; } = Array.Empty<AiDataQueryOptionDto>();
}
