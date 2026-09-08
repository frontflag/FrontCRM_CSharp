using CRM.Core.Utilities;

namespace CRM.Core.Interfaces;

public sealed class CommissionRateDto
{
    public string Id { get; set; } = string.Empty;
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

public interface ICommissionRateService
{
    Task<IReadOnlyList<CommissionRateDto>> ListAsync(short roleType, CancellationToken cancellationToken = default);

    Task<CommissionRateDto?> GetAsync(string id, CancellationToken cancellationToken = default);

    Task<CommissionRateDto> UpdateAsync(
        string id,
        IReadOnlyList<CommissionLadderWriteDto> ladders,
        string? remark,
        string? operatorUserId,
        CancellationToken cancellationToken = default);
}
