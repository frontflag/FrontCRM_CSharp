namespace CRM.Core.Interfaces;

public interface IRiskAlertService
{
    Task<RiskAlertSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);

    Task<RiskAlertSettingsDto> PutSettingsAsync(
        RiskAlertSettingsPutRequest request,
        CancellationToken cancellationToken = default);

    Task<RiskAlertDashboardDto> GetDashboardAsync(string userId, CancellationToken cancellationToken = default);
}

public sealed class RiskAlertSettingsDto
{
    public decimal InventoryAmountUsdMax { get; set; }
    public int StockAgeDaysMax { get; set; }
    public decimal ReceivableAmountUsdMax { get; set; }
    public decimal CustomerReceivableUsdMax { get; set; }
    public int SoReceivableAgeDaysMax { get; set; }
}

public sealed class RiskAlertSettingsPutRequest
{
    public decimal? InventoryAmountUsdMax { get; set; }
    public int? StockAgeDaysMax { get; set; }
    public decimal? ReceivableAmountUsdMax { get; set; }
    public decimal? CustomerReceivableUsdMax { get; set; }
    public int? SoReceivableAgeDaysMax { get; set; }
}

public sealed class RiskAlertDashboardDto
{
    public int ImmediateCount { get; set; }
    public bool AnyEnabled { get; set; }
    public List<RiskAlertItemDto> Items { get; set; } = new();
}

public sealed class RiskAlertItemDto
{
    public string Code { get; set; } = string.Empty;
    public bool Triggered { get; set; }
    public bool Enabled { get; set; }
    public bool Masked { get; set; }
    public decimal? ActualUsd { get; set; }
    public decimal? ThresholdUsd { get; set; }
    public int? ActualDays { get; set; }
    public int? ThresholdDays { get; set; }
    public int? HitCount { get; set; }
    public string? SubjectId { get; set; }
    public string? SubjectCode { get; set; }
    public string? SubjectName { get; set; }
}
