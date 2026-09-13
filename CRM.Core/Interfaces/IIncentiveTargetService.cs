namespace CRM.Core.Interfaces;

public interface IIncentiveTargetService
{
    Task<IncentiveTargetMineDto> GetMineAsync(string userId, CancellationToken cancellationToken = default);

    Task<IncentiveTargetMineDto> PutMineAsync(
        string userId,
        IncentiveTargetPutRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class IncentiveTargetMineDto
{
    public short RoleType { get; set; }
    public IncentiveTargetPeriodDto Term { get; set; } = new();
    public IncentiveTargetYearDto Year { get; set; } = new();
}

public sealed class IncentiveTargetPeriodDto
{
    public string PeriodKey { get; set; } = string.Empty;
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public string Label { get; set; } = string.Empty;
    public string MonthSpan { get; set; } = string.Empty;
    public bool HasTarget { get; set; }
    public decimal? TargetUsd { get; set; }
    public decimal? ActualUsd { get; set; }
    public decimal? CompletionPct { get; set; }
}

public sealed class IncentiveTargetYearDto
{
    public string PeriodKey { get; set; } = string.Empty;
    public bool HasTarget { get; set; }
    public decimal? TargetUsd { get; set; }
    public decimal? ActualUsd { get; set; }
    public decimal? CompletionPct { get; set; }
}

public sealed class IncentiveTargetPutRequest
{
    public decimal? TermTargetUsd { get; set; }
    public decimal? YearTargetUsd { get; set; }
}
