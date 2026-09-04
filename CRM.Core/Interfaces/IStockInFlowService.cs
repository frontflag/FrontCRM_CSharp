namespace CRM.Core.Interfaces;

/// <summary>入库单列表右侧「流程」页签聚合（7 站；锚点为入库单头）。</summary>
public interface IStockInFlowService
{
    Task<StockInFlowAggregatesDto> GetFlowAggregatesAsync(
        string stockInId,
        string? currentUserId,
        CancellationToken cancellationToken = default);
}

public sealed class StockInFlowAggregatesDto
{
    public string StockInId { get; set; } = string.Empty;
    /// <summary>当前站：入库单头。</summary>
    public StockItemFlowDocDto StockIn { get; set; } = new();
    public List<StockItemFlowDocDto> PurchaseOrderItems { get; set; } = new();
    public List<StockItemFlowDocDto> Qcs { get; set; } = new();
    public List<StockItemFlowDocDto> StockItems { get; set; } = new();
    public List<StockItemFlowDocDto> StockOutNotifies { get; set; } = new();
    public List<StockItemFlowDocDto> Packings { get; set; } = new();
    public List<StockItemFlowDocDto> StockOuts { get; set; } = new();
}

/// <summary>单条库存层下游三站（出库通知 / 装箱 / 出库）。</summary>
public sealed class StockItemFlowDownstreamSliceDto
{
    public List<StockItemFlowDocDto> StockOutNotifies { get; set; } = new();
    public List<StockItemFlowDocDto> Packings { get; set; } = new();
    public List<StockItemFlowDocDto> StockOuts { get; set; } = new();
}
