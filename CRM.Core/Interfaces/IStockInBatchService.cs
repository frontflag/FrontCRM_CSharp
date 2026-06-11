using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces
{
    public class StockInBatchListQuery
    {
        public string? GlobalBatchNo { get; set; }
        public string? Lot { get; set; }
        public string? SerialNumber { get; set; }
    }

    public class StockInBatchUpdateRequest
    {
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
        public string? Remark { get; set; }
    }

    /// <summary>Excel 导入：单行批次数据（与模板列对应，不含全局编号）。</summary>
    public class StockInBatchImportRowRequest
    {
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
        public string? Remark { get; set; }
    }

    public class StockInBatchImportRequest
    {
        public string StockInId { get; set; } = string.Empty;
        public string StockInItemId { get; set; } = string.Empty;
        public List<StockInBatchImportRowRequest> Rows { get; set; } = new();
    }

    public class StockInBatchImportResultDto
    {
        public int ImportedCount { get; set; }
        public List<string> GlobalBatchNos { get; set; } = new();
    }

    public interface IStockInBatchService
    {
        Task<IReadOnlyList<StockInBatch>> ListAsync(StockInBatchListQuery? query, CancellationToken cancellationToken = default);
        Task<StockInBatch?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<StockInBatch> UpdateAsync(string id, StockInBatchUpdateRequest request, CancellationToken cancellationToken = default);
        Task<StockInBatchImportResultDto> ImportAsync(StockInBatchImportRequest request, CancellationToken cancellationToken = default);
        Task SoftDeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}
