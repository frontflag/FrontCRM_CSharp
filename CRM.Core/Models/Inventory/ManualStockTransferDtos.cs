namespace CRM.Core.Models.Inventory;

public sealed class ManualStockTransferPreviewDto
{
    public string SourceStockItemId { get; set; } = string.Empty;
    public string? StockItemCode { get; set; }

    /// <summary>物料型号展示：优先在库行采购型号，否则物料主数据规格型号。</summary>
    public string? MaterialModel { get; set; }

    /// <summary>品牌（在库行采购品牌快照）。</summary>
    public string? MaterialBrand { get; set; }

    /// <summary>地域类型（与 <see cref="StockItem.RegionType"/> 一致：10 境内 / 20 境外）。</summary>
    public short RegionType { get; set; }

    public string FromWarehouseId { get; set; } = string.Empty;
    public string? FromWarehouseName { get; set; }
    public string? SourceLocationId { get; set; }
    public int QtyRepertory { get; set; }
    public int QtyRepertoryAvailable { get; set; }
    public int PlannedMoveQty { get; set; }
    public bool CanExecute { get; set; }
    public IReadOnlyList<string> BlockReasons { get; set; } = Array.Empty<string>();
}

public sealed class ManualStockTransferExecuteRequest
{
    public string SourceStockItemId { get; set; } = string.Empty;
    public string ToWarehouseId { get; set; } = string.Empty;
    public string? ToLocationId { get; set; }
    public string? Remark { get; set; }
}

public sealed class ManualStockTransferExecuteResultDto
{
    public string StockTransferManualId { get; set; } = string.Empty;
    public string TransferCode { get; set; } = string.Empty;
    public int MoveQty { get; set; }
    public string TargetStockItemId { get; set; } = string.Empty;
    public string TargetStockAggregateId { get; set; } = string.Empty;
}
