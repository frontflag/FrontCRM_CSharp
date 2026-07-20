namespace CRM.Core.Interfaces;

public sealed class TelemetryIngestItem
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string? SessionId { get; set; }
    public string? PageKey { get; set; }
    public string? RoutePath { get; set; }
    public string? Browser { get; set; }
    public string? Os { get; set; }
    public string? DeviceType { get; set; }
    public int? ScreenW { get; set; }
    public int? ScreenH { get; set; }
    public string? UserAgent { get; set; }
    public string? PayloadJson { get; set; }
}

public sealed class TelemetryIngestResult
{
    public int Accepted { get; set; }
    public int Duplicate { get; set; }
    public int Rejected { get; set; }
}

public sealed class TelemetryPageRankRow
{
    public string PageKey { get; set; } = string.Empty;
    /// <summary>业务解释；未登记时为 null，前端显示「—」。</summary>
    public string? Description { get; set; }
    public long ViewCount { get; set; }
    public long VisibleMsSum { get; set; }
    public long ActiveMsSum { get; set; }
}

public sealed class TelemetryActionRankRow
{
    public string PageKey { get; set; } = string.Empty;
    public string ActionId { get; set; } = string.Empty;
    /// <summary>业务解释；未登记时为 null，前端显示「—」。</summary>
    public string? Description { get; set; }
    public long ClickCount { get; set; }
}

public sealed class TelemetryApiRankRow
{
    public string Method { get; set; } = string.Empty;
    public string PathTemplate { get; set; } = string.Empty;
    /// <summary>业务解释；未登记时为 null，前端显示「—」。</summary>
    public string? Description { get; set; }
    public long CallCount { get; set; }
    public long FailCount { get; set; }
    public double AvgDurationMs { get; set; }
    public int MaxDurationMs { get; set; }
}

public interface ITelemetryService
{
    Task<TelemetryIngestResult> IngestAsync(
        IReadOnlyList<TelemetryIngestItem> items,
        string? userId,
        string? userName,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelemetryPageRankRow>> GetTopPagesAsync(
        DateOnly start, DateOnly end, int take = 50, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelemetryActionRankRow>> GetTopActionsAsync(
        DateOnly start, DateOnly end, int take = 50, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelemetryApiRankRow>> GetTopApisAsync(
        DateOnly start, DateOnly end, int take = 50, CancellationToken cancellationToken = default);

    Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default);
}
