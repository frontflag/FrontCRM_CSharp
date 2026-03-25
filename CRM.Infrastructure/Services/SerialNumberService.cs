using CRM.Core.Interfaces;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Services
{
    /// <summary>
    /// 系统流水号服务实现
    /// 使用数据库行锁（FOR UPDATE）确保并发安全
    /// </summary>
    public class SerialNumberService : ISerialNumberService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SerialNumberService> _logger;

        public SerialNumberService(ApplicationDbContext context, ILogger<SerialNumberService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<string> GenerateNextAsync(string moduleCode)
        {
            // 使用事务 + 行锁确保并发安全
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // PostgreSQL: FOR UPDATE 行锁，防止并发重复
                var serial = await _context.SerialNumbers
                    .FromSqlRaw(
                        "SELECT * FROM sys_serial_number WHERE \"ModuleCode\" = {0} FOR UPDATE",
                        moduleCode)
                    .FirstOrDefaultAsync();

                if (serial == null)
                {
                    throw new InvalidOperationException($"未找到业务模块 '{moduleCode}' 的流水号配置，请先初始化。");
                }

                // 检查是否需要按年/月重置
                var now = DateTime.UtcNow;
                if (serial.ResetByYear && serial.LastResetYear != now.Year)
                {
                    serial.CurrentSequence = 0;
                    serial.LastResetYear = now.Year;
                    serial.LastResetMonth = now.Month;
                }
                else if (serial.ResetByMonth && (serial.LastResetYear != now.Year || serial.LastResetMonth != now.Month))
                {
                    serial.CurrentSequence = 0;
                    serial.LastResetYear = now.Year;
                    serial.LastResetMonth = now.Month;
                }

                // 递增流水号
                serial.CurrentSequence += 1;
                serial.UpdateTime = DateTime.UtcNow;

                _context.SerialNumbers.Update(serial);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 统一业务编号格式：前缀 + YYMMDD + 4位流水号
                var result = FormatBusinessCode(serial.Prefix, serial.CurrentSequence, now);
                _logger.LogDebug("生成流水号：{ModuleCode} -> {SerialNo}", moduleCode, result);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> PreviewNextAsync(string moduleCode)
        {
            var serial = await _context.SerialNumbers
                .FirstOrDefaultAsync(s => s.ModuleCode == moduleCode);

            if (serial == null)
                throw new InvalidOperationException($"未找到业务模块 '{moduleCode}' 的流水号配置。");

            var nextSeq = serial.CurrentSequence + 1;
            return FormatBusinessCode(serial.Prefix, nextSeq, DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public async Task<int> GetCurrentSequenceAsync(string moduleCode)
        {
            var serial = await _context.SerialNumbers
                .FirstOrDefaultAsync(s => s.ModuleCode == moduleCode);
            return serial?.CurrentSequence ?? 0;
        }

        /// <inheritdoc/>
        public async Task ResetSequenceAsync(string moduleCode, int startFrom = 0)
        {
            var serial = await _context.SerialNumbers
                .FirstOrDefaultAsync(s => s.ModuleCode == moduleCode);

            if (serial == null)
                throw new InvalidOperationException($"未找到业务模块 '{moduleCode}' 的流水号配置。");

            serial.CurrentSequence = startFrom;
            serial.UpdateTime = DateTime.UtcNow;
            _context.SerialNumbers.Update(serial);
            await _context.SaveChangesAsync();
            _logger.LogWarning("流水号已重置：模块={ModuleCode}，起始值={StartFrom}", moduleCode, startFrom);
        }

        private static string FormatBusinessCode(string prefix, int sequence, DateTime nowUtc)
        {
            var datePart = nowUtc.ToString("yyMMdd");
            var seqPart = sequence.ToString("D4");
            return $"{prefix}{datePart}{seqPart}";
        }
    }
}
