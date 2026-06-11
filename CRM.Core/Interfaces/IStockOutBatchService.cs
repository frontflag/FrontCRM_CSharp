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

public interface IStockOutBatchService
{
    Task<StockOutBatchImportResultDto> ImportAsync(
        StockOutBatchImportRequest request,
        CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(string id, CancellationToken cancellationToken = default);
}
