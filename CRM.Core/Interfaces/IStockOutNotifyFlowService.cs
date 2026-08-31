namespace CRM.Core.Interfaces;

/// <summary>
/// 出库通知列表右侧「流程」聚合：销售明细 → 出库通知（当前）→ 本行强绑定库存明细 →
/// 备货库存（有在库才出现）→ 本通知装箱 → 本通知出库。
/// </summary>
public interface IStockOutNotifyFlowService
{
    Task<StockOutNotifyFlowAggregatesDto> GetFlowAggregatesAsync(
        string stockOutNotifyId,
        string? currentUserId,
        CancellationToken cancellationToken = default);
}

public sealed class StockOutNotifyFlowAggregatesDto
{
    public string StockOutNotifyId { get; set; } = string.Empty;
    public StockItemFlowDocDto? SellOrderItem { get; set; }
    public StockItemFlowDocDto StockOutNotify { get; set; } = new();
    public List<StockItemFlowDocDto> StockItems { get; set; } = new();
    public List<StockItemFlowDocDto> StockingStockItems { get; set; } = new();
    public List<StockItemFlowDocDto> Packings { get; set; } = new();
    public List<StockItemFlowDocDto> StockOuts { get; set; } = new();
}
