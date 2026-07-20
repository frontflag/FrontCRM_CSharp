using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Services
{
    /// <summary>
    /// 错误日志服务：始终使用独立 Scope/DbContext 写入，避免污染业务失败中的上下文。
    /// </summary>
    public class ErrorLogService : IErrorLogService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ErrorLogService> _logger;

        public ErrorLogService(IServiceScopeFactory scopeFactory, ILogger<ErrorLogService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<long?> LogAsync(
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
                string? errorDetail = exception != null ? BuildErrorDetail(exception) : null;
                var msg = (errorMessage ?? string.Empty).Trim();
                if (msg.Length == 0) msg = exception?.Message ?? "未知错误";
                if (msg.Length > 500) msg = msg[..500];

                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var log = new SysErrorLog
                {
                    OccurredAt = DateTime.UtcNow,
                    ModuleName = string.IsNullOrWhiteSpace(moduleName) ? "系统" : moduleName.Trim(),
                    OperationType = TrimOrNull(operationType, 50),
                    ErrorMessage = msg,
                    ErrorDetail = errorDetail,
                    DocumentNo = TrimOrNull(documentNo, 50),
                    DataId = TrimOrNull(dataId, 36),
                    UserId = TrimOrNull(userId, 36),
                    UserName = TrimOrNull(userName, 50),
                    RequestPath = TrimOrNull(requestPath, 200),
                    RequestBody = requestBody
                };

                db.ErrorLogs.Add(log);
                await db.SaveChangesAsync();
                return log.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "写入错误日志失败");
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<(IEnumerable<SysErrorLog> Items, int Total)> GetPagedAsync(
            int page,
            int pageSize,
            string? moduleName = null,
            string? keyword = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? status = null)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var query = db.ErrorLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(moduleName))
                query = query.Where(e => e.ModuleName == moduleName.Trim());

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim();
                query = query.Where(e =>
                    e.ErrorMessage.Contains(k) ||
                    (e.DocumentNo != null && e.DocumentNo.Contains(k)) ||
                    (e.DataId != null && e.DataId.Contains(k)) ||
                    (e.RequestPath != null && e.RequestPath.Contains(k)) ||
                    (e.UserName != null && e.UserName.Contains(k)) ||
                    (e.ErrorDetail != null && e.ErrorDetail.Contains(k)));
            }

            if (startDate.HasValue)
                query = query.Where(e => e.OccurredAt >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(e => e.OccurredAt <= endDate.Value);

            var normalizedStatus = (status ?? string.Empty).Trim().ToLowerInvariant();
            if (normalizedStatus == SysErrorLogFilterStatus.Open)
                query = query.Where(e => !e.IsResolved);
            else if (normalizedStatus == SysErrorLogFilterStatus.Resolved)
                query = query.Where(e =>
                    e.IsResolved &&
                    (e.ResolveRemark == null || e.ResolveRemark != SysErrorLogResolveRemarks.Ignore));
            else if (normalizedStatus == SysErrorLogFilterStatus.Ignored)
                query = query.Where(e =>
                    e.IsResolved && e.ResolveRemark == SysErrorLogResolveRemarks.Ignore);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(e => e.OccurredAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        /// <inheritdoc/>
        public async Task<SysErrorLog?> GetByIdAsync(long id)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await db.ErrorLogs.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        }

        /// <inheritdoc/>
        public async Task ResolveAsync(long id, string remark)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var log = await db.ErrorLogs.FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new InvalidOperationException($"错误日志 ID={id} 不存在");

            log.IsResolved = true;
            log.ResolveRemark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim();
            if (log.ResolveRemark != null && log.ResolveRemark.Length > 200)
                log.ResolveRemark = log.ResolveRemark[..200];
            await db.SaveChangesAsync();
        }

        private static string? TrimOrNull(string? s, int max)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var t = s.Trim();
            return t.Length <= max ? t : t[..max];
        }

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
