namespace CRM.Core.Models.Dtos;

/// <summary>分页查询 log_operation 的请求参数。</summary>
public sealed class OperationLogQuery
{
    public string? BizType { get; set; }
    public string? ActionType { get; set; }
    public string? RecordCode { get; set; }
    public string? OperatorUserName { get; set; }
    /// <summary>起始时间（含），UTC 或可解析的带时区字符串。</summary>
    public DateTime? OperationTimeFrom { get; set; }
    /// <summary>结束时间（含），UTC 或可解析的带时区字符串。</summary>
    public DateTime? OperationTimeTo { get; set; }
    public string? Reason { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>单条操作日志（与 log_operation 列对应）。</summary>
public sealed class OperationLogListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string BizType { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public string? RecordCode { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public DateTime OperationTime { get; set; }
    public string? OperatorUserId { get; set; }
    public string? OperatorUserName { get; set; }
    public string? Reason { get; set; }
    public string? OperationDesc { get; set; }
}

public sealed class OperationLogPagedResult
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public IReadOnlyList<OperationLogListItemDto> Items { get; set; } = Array.Empty<OperationLogListItemDto>();
}
