using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces;

public interface IManualStockTransferService
{
    Task<ManualStockTransferPreviewDto> PreviewAsync(string sourceStockItemId, CancellationToken cancellationToken = default);

    Task<ManualStockTransferExecuteResultDto> ExecuteAsync(
        ManualStockTransferExecuteRequest request,
        string? actingUserId,
        CancellationToken cancellationToken = default);
}
