using CRM.Core.Models.Document;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Document
{
    /// <summary>
    /// 存储文件名：yyMMdd + 6位当日序号 + _bizId_ + 原始文件名（过滤非法字符）
    /// </summary>
    public class FileNameGenerator
    {
        private readonly ApplicationDbContext _context;

        public FileNameGenerator(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateAsync(string bizType, string bizId, string originalFileName)
        {
            var datePart = DateTime.UtcNow.ToString("yyMMdd");
            var seq = await GetNextDailySequenceAsync();
            var safeBizId = SanitizeFileName(bizId, 32);
            var safeOriginal = SanitizeFileName(Path.GetFileName(originalFileName), 180);
            var ext = Path.GetExtension(originalFileName);
            if (string.IsNullOrEmpty(ext))
                ext = "";
            var nameWithoutExt = safeOriginal;
            if (nameWithoutExt.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                nameWithoutExt = nameWithoutExt[..^ext.Length];
            return $"{datePart}{seq:D6}_{safeBizId}_{nameWithoutExt}{ext}";
        }

        private async Task<int> GetNextDailySequenceAsync()
        {
            var today = DateTime.UtcNow.Date;
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var row = await _context.DocumentDailySequences
                    .FromSqlRaw("SELECT * FROM document_daily_sequence WHERE \"TheDate\" = {0} FOR UPDATE", today)
                    .FirstOrDefaultAsync();

                if (row == null)
                {
                    row = new DocumentDailySequence { TheDate = today, CurrentSequence = 0 };
                    await _context.DocumentDailySequences.AddAsync(row);
                    await _context.SaveChangesAsync();
                }

                row.CurrentSequence += 1;
                _context.DocumentDailySequences.Update(row);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return row.CurrentSequence;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static string SanitizeFileName(string value, int maxLen)
        {
            if (string.IsNullOrWhiteSpace(value)) return "file";
            var invalid = Path.GetInvalidFileNameChars();
            var s = new string(value.Trim().Where(c => !invalid.Contains(c)).ToArray());
            if (s.Length > maxLen) s = s[..maxLen];
            return string.IsNullOrEmpty(s) ? "file" : s;
        }
    }
}
