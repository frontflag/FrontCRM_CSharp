namespace CRM.Core.Interfaces;

public sealed class CustomerIntelInvestigateRequest
{
    public string? CustomerId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? CreditCode { get; set; }
    public string? Region { get; set; }
    public bool ForceRefresh { get; set; }
}

public class CustomerIntelReportSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? CreditCode { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool IsLatest { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CustomerIntelReportDetailDto : CustomerIntelReportSummaryDto
{
    public object? Report { get; set; }
    public string SchemaVersion { get; set; } = "1.0";
    public string? InvocationLogId { get; set; }
    public bool FromCache { get; set; }
}

public sealed class CustomerIntelInvestigateResultDto
{
    public CustomerIntelReportDetailDto Report { get; set; } = new();
    public bool FromCache { get; set; }
    public string InvocationId { get; set; } = string.Empty;
}

public interface ICustomerIntelReportService
{
    Task<CustomerIntelInvestigateResultDto> InvestigateAsync(
        CustomerIntelInvestigateRequest request,
        string? userId,
        CancellationToken cancellationToken = default);

    Task<CustomerIntelReportDetailDto?> GetLatestByCustomerIdAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerIntelReportSummaryDto>> ListByCustomerIdAsync(
        string customerId,
        int take = 20,
        CancellationToken cancellationToken = default);

    Task<CustomerIntelReportDetailDto?> GetByIdAsync(
        string reportId,
        CancellationToken cancellationToken = default);

    Task<CustomerIntelReportDetailDto?> GetLatestByQueryAsync(
        string companyName,
        string? creditCode,
        CancellationToken cancellationToken = default);
}
