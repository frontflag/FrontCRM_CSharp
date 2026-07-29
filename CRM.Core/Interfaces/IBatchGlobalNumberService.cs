namespace CRM.Core.Interfaces;

/// <summary>
/// 入库/出库批次全局唯一编号：<c>PC-</c> + 8 位十进制流水（<see cref="ModuleCodes.InventoryBatch"/>）。
/// </summary>
public interface IBatchGlobalNumberService
{
    Task<string> GenerateNextAsync(CancellationToken cancellationToken = default);

    /// <summary>一次事务预留连续 <paramref name="count"/> 个编号（大批量导入用，避免逐条事务）。</summary>
    Task<IReadOnlyList<string>> GenerateNextBlockAsync(int count, CancellationToken cancellationToken = default);
}
