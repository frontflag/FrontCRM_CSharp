namespace CRM.Core.Interfaces;

public interface ICustomerNewsMonitorService
{
    Task<CustomerNewsListDto> ListAsync(
        string customerId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<CustomerNewsDetailDto?> GetByIdAsync(
        string customerId,
        string briefingId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<CustomerNewsLatestRunDto> GetLatestRunAsync(
        string customerId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<CustomerNewsRunResultDto> RunAsync(
        string customerId,
        string userId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string customerId,
        string briefingId,
        string userId,
        CancellationToken cancellationToken = default);
}

public sealed class CustomerNewsListItemDto
{
    public string Id { get; set; } = string.Empty;
    public DateOnly BriefingDate { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public sealed class CustomerNewsListDto
{
    public bool CanFetch { get; set; }
    public bool CanDelete { get; set; }
    public bool IsRunning { get; set; }
    public List<CustomerNewsListItemDto> Items { get; set; } = new();
}

public sealed class CustomerNewsDetailDto
{
    public string Id { get; set; } = string.Empty;
    public DateOnly BriefingDate { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string Markdown { get; set; } = string.Empty;
}

public sealed class CustomerNewsLatestRunDto
{
    public string? Id { get; set; }
    public string? Status { get; set; }
    public string? Message { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public bool IsRunning { get; set; }
}

public sealed class CustomerNewsRunResultDto
{
    public bool Ran { get; set; }
    public bool Success { get; set; }
    public bool Pending { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime AcceptedAt { get; set; }
    public CustomerNewsDetailDto? Latest { get; set; }
}
