namespace CRM.Core.Interfaces;

public interface IIndustryNewsService
{
    Task<IndustryNewsLatestDto> GetLatestAsync(CancellationToken cancellationToken = default);

    Task<IndustryNewsRunResultDto> RunForTodayAsync(
        bool force,
        CancellationToken cancellationToken = default);
}

public sealed class IndustryNewsLatestDto
{
    public bool HasBriefing { get; set; }
    public bool IsStale { get; set; }
    public DateOnly? BriefingDate { get; set; }
    public DateOnly? PeriodStart { get; set; }
    public DateOnly? PeriodEnd { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public List<IndustryNewsItemDto> Items { get; set; } = new();
    public string Markdown { get; set; } = string.Empty;
}

public sealed class IndustryNewsItemDto
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string OccurredOn { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int Importance { get; set; }
    public bool IsBackground { get; set; }
    public bool Unconfirmed { get; set; }
}

public sealed class IndustryNewsRunResultDto
{
    public bool Ran { get; set; }
    public bool Success { get; set; }
    /// <summary>已受理、模型仍在后台生成；调用方应轮询 latest。</summary>
    public bool Pending { get; set; }
    public string Message { get; set; } = string.Empty;
    public IndustryNewsLatestDto? Latest { get; set; }
}
