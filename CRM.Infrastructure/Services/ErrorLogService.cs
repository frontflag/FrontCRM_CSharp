using CRM.Core.Interfaces;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Services
{
    /// <summary>
    /// 错误日志服务实现
    /// </summary>
    public class ErrorLogService : IErrorLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ErrorLogService> _logger;

        public ErrorLogService(ApplicationDbContext context, ILogger<ErrorLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task LogAsync(
            string moduleName,
            string errorMessage,
            Exception? exception = null,
            string? operationType = null,
            string? documentNo = null,
            string? dataId = null,
            string? userId = null,
            string? userName = null,
            string? requestPath = null,
            string? requestBody = null)
        {
            try
            {
                string? errorDetail = null;
                if (exception != null)
                {
                    errorDetail = BuildErrorDetail(exception);
                }

                var log = new SysErrorLog
                {
                    OccurredAt = DateTime.UtcNow,
                    ModuleName = moduleName,
                    OperationType = operationType,
                    ErrorMessage = errorMessage.Length > 500 ? errorMessage[..500] : errorMessage,
                    ErrorDetail = errorDetail,
                    DocumentNo = documentNo,
                    DataId = dataId,
                    UserId = userId,
                    UserName = userName,
                    RequestPath = requestPath,
                    RequestBody = requestBody
                };

                _context.ErrorLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // 错误日志本身不能抛异常，只记录到系统日志
                _logger.LogError(ex, "写入错误日志失败");
            }
        }

        /// <inheritdoc/>
        public async Task<(IEnumerable<SysErrorLog> Items, int Total)> GetPagedAsync(
            int page,
            int pageSize,
            string? moduleName = null,
            string? keyword = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _context.ErrorLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(moduleName))
                query = query.Where(e => e.ModuleName == moduleName);

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(e =>
                    e.ErrorMessage.Contains(keyword) ||
                    (e.DocumentNo != null && e.DocumentNo.Contains(keyword)) ||
                    (e.DataId != null && e.DataId.Contains(keyword)));

            if (startDate.HasValue)
                query = query.Where(e => e.OccurredAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.OccurredAt <= endDate.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(e => e.OccurredAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        /// <inheritdoc/>
        public async Task ResolveAsync(long id, string remark)
        {
            var log = await _context.ErrorLogs.FindAsync(id);
            if (log == null) throw new InvalidOperationException($"错误日志 ID={id} 不存在");

            log.IsResolved = true;
            log.ResolveRemark = remark;
            _context.ErrorLogs.Update(log);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 构建完整错误详情字符串
        /// </summary>
        private static string BuildErrorDetail(Exception ex)
        {
            var lines = new List<string>();
            var current = ex;
            var depth = 0;

            while (current != null && depth < 5)
            {
                var prefix = depth == 0 ? "Exception" : $"InnerException[{depth}]";
                lines.Add($"[{prefix}] {current.GetType().FullName}: {current.Message}");
                if (!string.IsNullOrEmpty(current.StackTrace))
                {
                    var stackLines = current.StackTrace.Split('\n').Take(10);
                    lines.AddRange(stackLines.Select(l => "  " + l.Trim()));
                }
                current = current.InnerException;
                depth++;
            }

            return string.Join("\n", lines);
        }
    }
}
