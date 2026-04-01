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
        private const string Base32Alphabet = "0123456789ABCDEFGHKLMNPRSTUVWXYZ";
        private const int EncodedSequenceLength = 5;

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

                // 新业务编号规则：3/4位业务标识 + 5位32进制流水号
                var result = FormatBusinessCode(serial.Prefix, serial.CurrentSequence);
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
            return FormatBusinessCode(serial.Prefix, nextSeq);
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

        private static string FormatBusinessCode(string prefix, int sequence)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new InvalidOperationException("流水号前缀不能为空。");
            }

            var normalizedPrefix = prefix.Trim().ToUpperInvariant();
            if (normalizedPrefix.Length < 3 || normalizedPrefix.Length > 4)
            {
                throw new InvalidOperationException(
                    $"流水号前缀 '{normalizedPrefix}' 非法：仅支持3位主业务前缀或4位辅助业务前缀。");
            }

            var seqPart = EncodeBase32(sequence, EncodedSequenceLength);
            return $"{normalizedPrefix}{seqPart}";
        }

        private static string EncodeBase32(int value, int minLength)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "流水号值不能为负数。");
            }

            var baseSize = Base32Alphabet.Length;
            var buffer = new char[Math.Max(minLength, 1)];
            var index = buffer.Length - 1;
            var current = value;

            do
            {
                var remainder = current % baseSize;
                buffer[index--] = Base32Alphabet[remainder];
                current /= baseSize;
            } while (current > 0 && index >= 0);

            if (current > 0)
            {
                throw new InvalidOperationException($"流水号超出{minLength}位32进制可表示范围。");
            }

            while (index >= 0)
            {
                buffer[index--] = '0';
            }

            return new string(buffer);
        }
    }
}
