using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces
{
    public class StockInBatchListQuery
    {
        public string? StockInItemCode { get; set; }
        public string? Lot { get; set; }
        public string? SerialNumber { get; set; }
    }

    public class StockInBatchUpdateRequest
    {
        public string? MaterialModel { get; set; }
        public string? Dc { get; set; }
        public string? PackageOrigin { get; set; }
        public string? WaferOrigin { get; set; }
        public string? Lot { get; set; }
        public int LotQtyIn { get; set; }
        public int LotQtyOut { get; set; }
        public string? Origin { get; set; }
        public string? SerialNumber { get; set; }
        public int SnQtyIn { get; set; }
        public int SnQtyOut { get; set; }
        public string? FirmwareVersion { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>Excel 导入：单行批次数据（与模板列对应）。</summary>
    public class StockInBatchImportRowRequest
    {
        public string? MaterialModel { get; set; }
        public string? Dc { get; set; }
        public string? PackageOrigin { get; set; }
        public string? WaferOrigin { get; set; }
        public string? Lot { get; set; }
        public int LotQtyIn { get; set; }
        public string? Origin { get; set; }
        public string? SerialNumber { get; set; }
        public int SnQtyIn { get; set; }
        public string? FirmwareVersion { get; set; }
        public string? Remark { get; set; }
    }

    public class StockInBatchImportRequest
    {
        public string StockInId { get; set; } = string.Empty;
        public string StockInItemId { get; set; } = string.Empty;
        public string? StockInItemCode { get; set; }
        public List<StockInBatchImportRowRequest> Rows { get; set; } = new();
    }

    /// <summary>Excel 核销：单行（LOT / SN 核销数量）。</summary>
    public class StockInBatchWriteOffRowRequest
    {
        public string? Lot { get; set; }
        public int LotWriteOffQty { get; set; }
        public string? SerialNumber { get; set; }
        public int SnWriteOffQty { get; set; }
    }

    public class StockInBatchWriteOffRequest
    {
        public List<StockInBatchWriteOffRowRequest> Rows { get; set; } = new();
    }

    /// <summary>核销结果：先校验全部行，<see cref="ValidationPassed"/> 为 false 时不写库。</summary>
    public class StockInBatchWriteOffResultDto
    {
        public bool ValidationPassed { get; set; }
        /// <summary>实际更新的 <c>stock_in_batch</c> 行数（按主键去重）。</summary>
        public int UpdatedRowCount { get; set; }
        public List<string> InvalidLots { get; set; } = new();
        public List<string> InvalidSerialNumbers { get; set; } = new();
    }

    public interface IStockInBatchService
    {
        Task<IReadOnlyList<StockInBatch>> ListAsync(StockInBatchListQuery? query, CancellationToken cancellationToken = default);
        Task<StockInBatch?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<StockInBatch> UpdateAsync(string id, StockInBatchUpdateRequest request, CancellationToken cancellationToken = default);
        /// <summary>按入库单与明细写入多条 <c>stock_in_batch</c>，返回新增条数。</summary>
        Task<int> ImportAsync(StockInBatchImportRequest request, CancellationToken cancellationToken = default);
        /// <summary>按 LOT / SN 累加 <c>lot_qty_out</c>、<c>sn_qty_out</c>；校验失败不写库。</summary>
        Task<StockInBatchWriteOffResultDto> ApplyWriteOffAsync(StockInBatchWriteOffRequest request, CancellationToken cancellationToken = default);
    }
}
