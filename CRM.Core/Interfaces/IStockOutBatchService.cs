using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces;

public class StockOutBatchImportRowRequest
{
    public string? GlobalBatchNo { get; set; }
    public int OutQty { get; set; }
}

public class StockOutBatchImportRequest
{
    public string PackingId { get; set; } = string.Empty;
    public List<StockOutBatchImportRowRequest> Rows { get; set; } = new();
}

public class StockOutBatchImportResultDto
{
    public int ImportedCount { get; set; }
}

public class StockOutBatchUpdateRequest
{
    public int OutQty { get; set; }
}

public class StockOutBatchOperationContext
{
    public string? OperatorUserId { get; set; }
    public string? OperatorUserName { get; set; }
}

public class StockOutBatchBulkDeleteSkippedDto
{
    public string GlobalBatchNo { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class StockOutBatchBulkDeleteResultDto
{
    public int DeletedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> DeletedGlobalBatchNos { get; set; } = new();
    public List<StockOutBatchBulkDeleteSkippedDto> Skipped { get; set; } = new();
}

public interface IStockOutBatchService
{
    Task<StockOutBatch?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<StockOutBatchImportResultDto> ImportAsync(
        StockOutBatchImportRequest request,
        StockOutBatchOperationContext? context = null,
        CancellationToken cancellationToken = default);

    Task<StockOutBatch> UpdateAsync(
        string id,
        StockOutBatchUpdateRequest request,
        StockOutBatchOperationContext? context = null,
        CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(
        string id,
        string reason,
        StockOutBatchOperationContext? context = null,
        CancellationToken cancellationToken = default);

    Task<StockOutBatchBulkDeleteResultDto> BulkDeleteByPackingAsync(
        string packingId,
        string reason,
        StockOutBatchOperationContext? context = null,
        CancellationToken cancellationToken = default);

    Task LogExportAsync(
        string packingId,
        int exportedCount,
        StockOutBatchOperationContext? context = null,
        CancellationToken cancellationToken = default);
}
