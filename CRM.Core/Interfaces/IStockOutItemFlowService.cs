namespace CRM.Core.Interfaces;

/// <summary>
/// 出库明细列表右侧「流程」聚合：销售明细 → 出库通知 → 本行库存明细 → 本行装箱 → 本出库行（当前）。
/// </summary>
public interface IStockOutItemFlowService
{
    Task<StockOutItemFlowAggregatesDto> GetFlowAggregatesAsync(
        string stockOutItemId,
        string? currentUserId,
        CancellationToken cancellationToken = default);
}

public sealed class StockOutItemFlowAggregatesDto
{
    public string StockOutItemId { get; set; } = string.Empty;
    public StockItemFlowDocDto? SellOrderItem { get; set; }
    public StockItemFlowDocDto? StockOutNotify { get; set; }
    public List<StockItemFlowDocDto> StockItems { get; set; } = new();
    public List<StockItemFlowDocDto> Packings { get; set; } = new();
    public List<StockItemFlowDocDto> StockOuts { get; set; } = new();
}
