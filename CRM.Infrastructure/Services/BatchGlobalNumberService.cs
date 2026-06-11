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

            serial.CurrentSequence += 1;
            if (serial.CurrentSequence > MaxSequence)
            {
                throw new InvalidOperationException(
                    $"批次全局编号已超出 {SequenceLength} 位十进制可表示范围（最大 {MaxSequence}）。");
            }

            serial.UpdateTime = DateTime.UtcNow;
            _context.SerialNumbers.Update(serial);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var result = $"{DisplayPrefix}{serial.CurrentSequence:D8}";
            _logger.LogDebug("生成批次全局编号：{GlobalBatchNo}", result);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
