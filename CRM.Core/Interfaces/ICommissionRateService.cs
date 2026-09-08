using CRM.Core.Utilities;

namespace CRM.Core.Interfaces;

public sealed class CommissionRateDto
{
    public string Id { get; set; } = string.Empty;
    public string VersionId { get; set; } = string.Empty;
    public short RoleType { get; set; }
    public short UserLevel { get; set; }
    public decimal?[] Thresholds { get; set; } = new decimal?[10];
    public decimal?[] RatePoints { get; set; } = new decimal?[10];
    public string? Remark { get; set; }
}

public sealed class CommissionLadderWriteDto
{
    public decimal? ThresholdAmount { get; set; }
    public decimal? RatePoints { get; set; }
}

public sealed class CommissionRateVersionDto
{
    public string Id { get; set; } = string.Empty;
    public short RoleType { get; set; }
    public int VersionNo { get; set; }
    public string? Remark { get; set; }
    public short Status { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CommissionRateSettingsDto
{
    public List<CommissionRateVersionDto> SalesVersions { get; set; } = new();
    public List<CommissionRateVersionDto> PurchaseVersions { get; set; } = new();
    public string? SalesActiveVersionId { get; set; }
    public string? PurchaseActiveVersionId { get; set; }
    public int ReceiptWriteoffDelayDays { get; set; }
}

public interface ICommissionRateService
{
    Task<CommissionRateSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommissionRateVersionDto>> ListVersionsAsync(
        short roleType,
        CancellationToken cancellationToken = default);

    Task<CommissionRateVersionDto> CreateVersionAsync(
        short roleType,
        string? remark,
        string? copyFromVersionId,
        string? operatorUserId,
        CancellationToken cancellationToken = default);

    Task<CommissionRateVersionDto> UpdateVersionAsync(
        string id,
        string? remark,
        string? operatorUserId,
        CancellationToken cancellationToken = default);

    Task SetActiveVersionsAsync(
        string salesVersionId,
        string purchaseVersionId,
        int receiptWriteoffDelayDays,
        string? operatorUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommissionRateDto>> ListAsync(
        short roleType,
        string? versionId = null,
        CancellationToken cancellationToken = default);

    Task<CommissionRateDto?> GetAsync(string id, CancellationToken cancellationToken = default);

    Task<CommissionRateDto> UpdateAsync(
        string id,
        IReadOnlyList<CommissionLadderWriteDto> ladders,
        string? remark,
        string? operatorUserId,
        CancellationToken cancellationToken = default);
}
