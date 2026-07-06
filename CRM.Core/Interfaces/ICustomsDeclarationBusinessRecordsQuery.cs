namespace CRM.Core.Interfaces;

using CRM.Core.Models.Inventory;

/// <summary>报关单详情「业务记录」面板：沿报关明细聚合上下游单据。</summary>
public interface ICustomsDeclarationBusinessRecordsQuery
{
    Task<CustomsDeclarationBusinessRecordsDto?> LoadAsync(string declarationId, CancellationToken cancellationToken = default);
}

public sealed class CustomsDeclarationBusinessRecordRowDto
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public short? Status { get; set; }
    public DateTime? OccurredAt { get; set; }
    /// <summary>所属主单 Id（如销售订单 Id、采购订单 Id），供前端跳转明细面板。</summary>
    public string? ParentId { get; set; }
}

public sealed class CustomsDeclarationBusinessRecordsDto
{
    public List<CustomsDeclarationBusinessRecordRowDto> SalesOrders { get; set; } = new();
    /// <summary>销售订单明细完整列表行（与销售订单详情页明细列表字段一致）。</summary>
    public List<SellOrderItemLineDto> SalesOrderItems { get; set; } = new();
    public List<CustomsDeclarationBusinessRecordRowDto> PurchaseOrders { get; set; } = new();
    /// <summary>采购订单明细完整列表行（与采购订单明细列表页字段一致）。</summary>
    public List<PurchaseOrderItemListLineDto> PurchaseOrderItems { get; set; } = new();
    public List<CustomsDeclarationBusinessRecordRowDto> StockOutNotifies { get; set; } = new();
    /// <summary>出库通知完整列表行（与出库通知列表页字段一致）。</summary>
    public List<StockOutRequestListItemDto> StockOutNotifyItems { get; set; } = new();
    public List<CustomsDeclarationBusinessRecordRowDto> CustomsStockOutNotifies { get; set; } = new();
    /// <summary>报关出库通知完整列表行（与出库通知列表页字段一致）。</summary>
    public List<StockOutRequestListItemDto> CustomsStockOutNotifyItems { get; set; } = new();
    public List<CustomsDeclarationBusinessRecordRowDto> CustomsPackings { get; set; } = new();
    /// <summary>报关装箱单完整列表行（与装箱单列表页字段一致）。</summary>
    public List<PackingListItemDto> CustomsPackingItems { get; set; } = new();
    public List<CustomsDeclarationBusinessRecordRowDto> CustomsStockOuts { get; set; } = new();
    /// <summary>报关出库完整列表行（与出库单列表页字段一致）。</summary>
    public List<StockOutListItemDto> CustomsStockOutItems { get; set; } = new();
    public List<CustomsDeclarationBusinessRecordRowDto> CustomsArrivalNotifies { get; set; } = new();
    /// <summary>报关到货通知完整列表行（与到货通知列表页字段一致）。</summary>
    public List<StockInNotify> CustomsArrivalNotifyItems { get; set; } = new();
    public List<CustomsDeclarationBusinessRecordRowDto> CustomsStockIns { get; set; } = new();
    public List<CustomsDeclarationBusinessRecordRowDto> Packings { get; set; } = new();
    /// <summary>装箱单完整列表行（与装箱单列表页字段一致）。</summary>
    public List<PackingListItemDto> PackingItems { get; set; } = new();
    public List<CustomsDeclarationBusinessRecordRowDto> StockOuts { get; set; } = new();
    /// <summary>出库单完整列表行（与出库单列表页字段一致）。</summary>
    public List<StockOutListItemDto> StockOutItems { get; set; } = new();
}
