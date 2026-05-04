using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces;

/// <summary>入库批次记录列表：数据库侧 Count + Skip/Take。</summary>
public interface IStockInBatchListQuery
{
    Task<PagedResult<StockInBatch>> GetPagedAsync(
        string? stockInItemCode,
        string? lot,
        string? serialNumber,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
