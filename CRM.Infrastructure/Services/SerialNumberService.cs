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
            var codes = await ReserveNextAsync(moduleCode, 1);
            return codes[0];
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<string>> ReserveNextAsync(
            string moduleCode,
            int count,
            CancellationToken cancellationToken = default)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "预占数量必须大于 0。");

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var serial = await _context.SerialNumbers
                    .FromSqlRaw(
                        "SELECT * FROM sys_serial_number WHERE \"ModuleCode\" = {0} FOR UPDATE",
                        moduleCode)
                    .FirstOrDefaultAsync(cancellationToken);

                if (serial == null)
                    throw new InvalidOperationException($"未找到业务模块 '{moduleCode}' 的流水号配置，请先初始化。");

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

                var codes = new string[count];
                for (var i = 0; i < count; i++)
                {
                    serial.CurrentSequence += 1;
                    codes[i] = FormatBusinessCode(serial.Prefix, serial.CurrentSequence);
                }

                serial.UpdateTime = DateTime.UtcNow;
                _context.SerialNumbers.Update(serial);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                _logger.LogDebug("预占流水号：{ModuleCode} x {Count} -> {First}", moduleCode, count, codes[0]);
                return codes;
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
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
            if (normalizedPrefix.Length < 2 || normalizedPrefix.Length > 16)
            {
                throw new InvalidOperationException(
                    $"流水号前缀 '{normalizedPrefix}' 非法：长度须在 2～16 之间。");
            }

            foreach (var c in normalizedPrefix)
            {
                if (c is (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_')
                    continue;
                throw new InvalidOperationException(
                    $"流水号前缀 '{normalizedPrefix}' 非法：仅允许字母、数字与下划线。");
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
