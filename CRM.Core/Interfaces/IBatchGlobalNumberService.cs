namespace CRM.Core.Interfaces;

/// <summary>
/// 入库/出库批次全局唯一编号：<c>PC-</c> + 8 位十进制流水（<see cref="ModuleCodes.InventoryBatch"/>）。
/// </summary>
public interface IBatchGlobalNumberService
{
    Task<string> GenerateNextAsync(CancellationToken cancellationToken = default);
}
