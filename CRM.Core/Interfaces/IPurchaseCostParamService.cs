namespace CRM.Core.Interfaces;

public sealed class PurchaseCostParamDto
{
    public string Id { get; set; } = string.Empty;
    public decimal Ratio { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public string? Remark { get; set; }
    public DateTime CreateTimeUtc { get; set; }
    public string? CreateByUserId { get; set; }
    public bool IsEffectiveNow { get; set; }
}

public sealed class PurchaseCostParamChangeLogDto
{
    public string Id { get; set; } = string.Empty;
    public string? PurchaseCostParamId { get; set; }
    public decimal Ratio { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime ChangeTimeUtc { get; set; }
    public string? ChangeUserId { get; set; }
    public string? ChangeUserName { get; set; }
    public string? ChangeSummary { get; set; }
}

public interface IPurchaseCostParamService
{
    Task<PurchaseCostParamDto> GetEffectiveAsync(DateTime? asOfUtc = null, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PurchaseCostParamDto> Items, int TotalCount)> ListPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PurchaseCostParamDto> CreateAsync(
        decimal ratio,
        DateTime startTimeUtc,
        string? remark,
        string? userId,
        string? userName,
        CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(string id, string? userId, string? userName, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PurchaseCostParamChangeLogDto> Items, int TotalCount)> GetChangeLogPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
