namespace CRM.Core.Interfaces;

public sealed class VendorIntelInvestigateRequest
{
    public string? VendorId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? CreditCode { get; set; }
    public string? Region { get; set; }
    public bool ForceRefresh { get; set; }
}

public class VendorIntelReportSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string? VendorId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? CreditCode { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool IsLatest { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class VendorIntelReportDetailDto : VendorIntelReportSummaryDto
{
    public object? Report { get; set; }
    public string SchemaVersion { get; set; } = "1.0";
    public string? InvocationLogId { get; set; }
    public bool FromCache { get; set; }
}

public sealed class VendorIntelInvestigateResultDto
{
    public VendorIntelReportDetailDto Report { get; set; } = new();
    public bool FromCache { get; set; }
    public string InvocationId { get; set; } = string.Empty;
}

public interface IVendorIntelReportService
{
    Task<VendorIntelInvestigateResultDto> InvestigateAsync(
        VendorIntelInvestigateRequest request,
        string? userId,
        CancellationToken cancellationToken = default);

    Task<VendorIntelReportDetailDto?> GetLatestByVendorIdAsync(
        string vendorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VendorIntelReportSummaryDto>> ListByVendorIdAsync(
        string vendorId,
        int take = 20,
        CancellationToken cancellationToken = default);

    Task<VendorIntelReportDetailDto?> GetByIdAsync(
        string reportId,
        CancellationToken cancellationToken = default);

    Task<VendorIntelReportDetailDto?> GetLatestByQueryAsync(
        string companyName,
        string? creditCode,
        CancellationToken cancellationToken = default);
}
