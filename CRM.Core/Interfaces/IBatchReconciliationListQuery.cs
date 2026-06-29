namespace CRM.Core.Interfaces;

public class BatchReconciliationQueryRequest
{
    /// <summary>当前用户 Id（服务端注入，用于采购数据范围过滤）。</summary>
    public string? CurrentUserId { get; set; }

    public string? GlobalBatchNo { get; set; }
    public string? PurchaseOrderCode { get; set; }
    public string? StockInCode { get; set; }
    public string? PackingCode { get; set; }
    public string? MaterialModel { get; set; }
    public string? Lot { get; set; }
    public string? SerialNumber { get; set; }
    public string? VendorName { get; set; }
    public string? CustomerName { get; set; }
    public string? Remark { get; set; }
}

public class BatchReconciliationRowDto
{
    public string StockInBatchId { get; set; } = string.Empty;
    public string? StockOutBatchId { get; set; }
    public string GlobalBatchNo { get; set; } = string.Empty;

    public string? WarehouseName { get; set; }
    public DateTime StockInDate { get; set; }
    public string StockInCode { get; set; } = string.Empty;
    public string? PurchaseOrderCode { get; set; }
    public string? FreightForwarderOrderNo { get; set; }
    public string? VendorId { get; set; }
    public string? VendorName { get; set; }
    public string? VendorEnglishName { get; set; }
    public string? MaterialModel { get; set; }
    public string? MaterialBrand { get; set; }
    public int StockInItemQuantity { get; set; }

    public string? BatchDimension { get; set; }
    public string? BatchUnit { get; set; }
    public string? UnitNo { get; set; }
    public int BatchQty { get; set; }
    public string? Dc { get; set; }
    public string? PackageOrigin { get; set; }
    public string? WaferOrigin { get; set; }
    public string? Lot { get; set; }
    public string? SerialNumber { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? PartCode { get; set; }
    public string? BatchRemark { get; set; }

    public string? PackingCode { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public DateTime? StockOutDate { get; set; }
    public int? OutQty { get; set; }

    /// <summary>该全局编号已出库合计。</summary>
    public int TotalOutQty { get; set; }
    /// <summary>剩余可出 = BatchQty - TotalOutQty。</summary>
    public int RemainingQty { get; set; }
}

public class BatchReconciliationConsumptionRowDto
{
    public string StockOutBatchId { get; set; } = string.Empty;
    public string PackingCode { get; set; } = string.Empty;
    public int OutQty { get; set; }
    public DateTime? StockOutDate { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
}

public interface IBatchReconciliationListQuery
{
    Task<PagedResult<BatchReconciliationRowDto>> GetPagedAsync(
        BatchReconciliationQueryRequest? request,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BatchReconciliationConsumptionRowDto>> GetConsumptionByGlobalBatchNoAsync(
        string globalBatchNo,
        string? currentUserId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BatchReconciliationRowDto>> ListForInBatchExportAsync(
        BatchReconciliationQueryRequest? request,
        int maxRows,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BatchOutExportRowDto>> ListForOutBatchExportAsync(
        BatchReconciliationQueryRequest? request,
        int maxRows,
        CancellationToken cancellationToken = default);
}

public class BatchOutExportRowDto
{
    public string GlobalBatchNo { get; set; } = string.Empty;
    public int OutQty { get; set; }
    public string PackingCode { get; set; } = string.Empty;
    public DateTime? StockOutDate { get; set; }
    public string? MaterialModel { get; set; }
    public string? Lot { get; set; }
}
