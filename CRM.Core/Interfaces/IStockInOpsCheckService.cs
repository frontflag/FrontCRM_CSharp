namespace CRM.Core.Interfaces;

/// <summary>入库链路运维检查（只读，全量；仅采购入库）。</summary>
public interface IStockInOpsCheckService
{
    Task<StockInOpsCheckResultDto> RunAsync(CancellationToken cancellationToken = default);
}

public sealed class StockInOpsCheckResultDto
{
    public DateTime RanAtUtc { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public int FindingCount { get; set; }
    public bool Truncated { get; set; }
    public IReadOnlyList<StockInOpsCheckFindingDto> Findings { get; set; } =
        Array.Empty<StockInOpsCheckFindingDto>();
}

public sealed class StockInOpsCheckFindingDto
{
    public string Severity { get; set; } = "error";
    public string Category { get; set; } = "";
    public string DocType { get; set; } = "";
    public string? DocId { get; set; }
    public string? DocCode { get; set; }
    public string? RouteName { get; set; }
    public Dictionary<string, string>? RouteParams { get; set; }
    public Dictionary<string, string>? RouteQuery { get; set; }
    public string? RelatedDocType { get; set; }
    public string? RelatedDocId { get; set; }
    public string? RelatedDocCode { get; set; }
    public string? RelatedRouteName { get; set; }
    public Dictionary<string, string>? RelatedRouteParams { get; set; }
    public Dictionary<string, string>? RelatedRouteQuery { get; set; }
    public string Reason { get; set; } = "";
    public string Suggestion { get; set; } = "";
}
