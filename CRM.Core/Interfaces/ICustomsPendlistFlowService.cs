namespace CRM.Core.Interfaces;

/// <summary>待报关列表右侧「流程」页签聚合（地铁轴上下游）。</summary>
public interface ICustomsPendlistFlowService
{
    Task<CustomsPendlistFlowAggregatesDto> GetFlowAggregatesAsync(
        string pendlistId,
        CancellationToken cancellationToken = default);
}

public sealed class CustomsPendlistFlowAggregatesDto
{
    public string PendlistId { get; set; } = string.Empty;
    public CustomsPendlistFlowDocDto? SellOrderItem { get; set; }
    public CustomsPendlistFlowDocDto? SalesStockOutNotify { get; set; }
    public CustomsPendlistFlowDocDto Pendlist { get; set; } = new();
    public List<CustomsPendlistFlowDocDto> CustomsStockOutNotifies { get; set; } = new();
    public List<CustomsPendlistFlowDocDto> Packings { get; set; } = new();
    public List<CustomsPendlistFlowDocDto> Pickings { get; set; } = new();
    public List<CustomsPendlistFlowDocDto> StockOuts { get; set; } = new();
    public List<CustomsPendlistFlowDocDto> Declarations { get; set; } = new();
    public List<CustomsPendlistFlowDocDto> Arrivals { get; set; } = new();
    public List<CustomsPendlistFlowDocDto> Qcs { get; set; } = new();
    public List<CustomsPendlistFlowDocDto> StockIns { get; set; } = new();
}

public sealed class CustomsPendlistFlowDocDto
{
    public string Id { get; set; } = string.Empty;
    public string? DocCode { get; set; }
    public short? Status { get; set; }
    public DateTime? CreateTime { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerCode { get; set; }
    public string? PersonName { get; set; }
    public decimal? UnitPrice { get; set; }
    public short? Currency { get; set; }
    public decimal? Qty { get; set; }
    public bool IsDeleted { get; set; }
    /// <summary>仅报关出库通知小卡：关联待报关 Id（溯源，非强制删除确认入口）。</summary>
    public string? PendlistId { get; set; }
    public string? SalesOrderId { get; set; }
}
