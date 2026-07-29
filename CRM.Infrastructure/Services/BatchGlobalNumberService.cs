using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Services;

/// <inheritdoc cref="IBatchGlobalNumberService"/>
public sealed class BatchGlobalNumberService : IBatchGlobalNumberService
{
    public const string DisplayPrefix = "PC-";
    private const int SequenceLength = 8;
    private const int MaxSequence = 99_999_999;

    private readonly ApplicationDbContext _context;
    private readonly ILogger<BatchGlobalNumberService> _logger;

    public BatchGlobalNumberService(ApplicationDbContext context, ILogger<BatchGlobalNumberService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string> GenerateNextAsync(CancellationToken cancellationToken = default)
    {
        var block = await GenerateNextBlockAsync(1, cancellationToken);
        return block[0];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GenerateNextBlockAsync(int count, CancellationToken cancellationToken = default)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), "预留编号数量须至少为 1。");

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var serial = await _context.SerialNumbers
                .FromSqlRaw(
                    "SELECT * FROM sys_serial_number WHERE \"ModuleCode\" = {0} FOR UPDATE",
                    ModuleCodes.InventoryBatch)
                .FirstOrDefaultAsync(cancellationToken);

            if (serial == null)
            {
                throw new InvalidOperationException(
                    $"未找到业务模块 '{ModuleCodes.InventoryBatch}' 的流水号配置，请先初始化。");
            }

            var start = serial.CurrentSequence;
            var end = start + count;
            if (end > MaxSequence)
            {
                throw new InvalidOperationException(
                    $"批次全局编号已超出 {SequenceLength} 位十进制可表示范围（最大 {MaxSequence}）。");
            }

            serial.CurrentSequence = end;
            serial.UpdateTime = DateTime.UtcNow;
            _context.SerialNumbers.Update(serial);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var result = new string[count];
            for (var i = 0; i < count; i++)
                result[i] = $"{DisplayPrefix}{(start + i + 1):D8}";

            _logger.LogDebug("批量生成批次全局编号 {Count} 条：{First} … {Last}", count, result[0], result[^1]);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
