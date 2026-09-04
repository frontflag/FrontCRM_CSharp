namespace CRM.Core.Interfaces;

/// <summary>库存明细列表右侧「流程」页签聚合（7 站；下游仅本层 <c>stockItemId</c>）。</summary>
public interface IStockItemFlowService
{
    Task<StockItemFlowAggregatesDto> GetFlowAggregatesAsync(
        string stockItemId,
        string? currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>单条库存层下游（出库通知 / 装箱 / 出库），供入库单流程等多层聚合复用。</summary>
    Task<StockItemFlowDownstreamSliceDto> GetDownstreamSliceAsync(
        string stockItemId,
        InventoryStockItemListRowDto row,
        CancellationToken cancellationToken = default);
}

public sealed class StockItemFlowAggregatesDto
{
    public string StockItemId { get; set; } = string.Empty;
    public StockItemFlowDocDto? PurchaseOrderItem { get; set; }
    public StockItemFlowDocDto? Qc { get; set; }
    public StockItemFlowDocDto? StockIn { get; set; }
    public StockItemFlowDocDto StockItem { get; set; } = new();
    public List<StockItemFlowDocDto> StockOutNotifies { get; set; } = new();
    public List<StockItemFlowDocDto> Packings { get; set; } = new();
    public List<StockItemFlowDocDto> StockOuts { get; set; } = new();
}

public sealed class StockItemFlowDocDto
{
    public string Id { get; set; } = string.Empty;
    public string? DocCode { get; set; }
    public short? Status { get; set; }
    public DateTime? CreateTime { get; set; }
    /// <summary>库存明细站：入库日（与创建时间区分）。</summary>
    public DateTime? BizDate { get; set; }
    public string? VendorName { get; set; }
    public string? VendorCode { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerCode { get; set; }
    public string? PersonName { get; set; }
    public decimal? UnitPrice { get; set; }
    public short? Currency { get; set; }
    public decimal? SalesUnitPrice { get; set; }
    public short? SalesCurrency { get; set; }
    public decimal? Qty { get; set; }
    /// <summary>库存明细站：层已出库数量。</summary>
    public decimal? Qty2 { get; set; }
    public int? PassQty { get; set; }
    public int? RejectQty { get; set; }
    public short? StockInType { get; set; }
    /// <summary>出库通知 / 装箱 / 出库单：出库类型（<c>StockOutType</c>）。</summary>
    public short? StockOutType { get; set; }
    /// <summary>报关入库 / 报关出库关联报关单主键（有值时前端显示报关单图标）。</summary>
    public string? CustomsDeclarationId { get; set; }
    /// <summary>关联报关单号（图标 tooltip / 跳转展示）。</summary>
    public string? CustomsDeclarationCode { get; set; }
    public string? StockInNotifyId { get; set; }
    public string? PurchaseOrderId { get; set; }
    public string? PurchaseOrderItemId { get; set; }
    public string? StockAggregateId { get; set; }
    /// <summary>销售明细站：所属销售订单主键（跳转详情用）。</summary>
    public string? SellOrderId { get; set; }
    /// <summary>出库站：出库明细单号（与头单号并列）。</summary>
    public string? LineDocCode { get; set; }
    public bool IsDeleted { get; set; }
}
