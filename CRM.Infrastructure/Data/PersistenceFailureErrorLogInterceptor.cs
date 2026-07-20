using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.System;
using CRM.Core.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Data;

/// <summary>
/// SaveChanges 失败且为 DbUpdate* 时写入 sys_error_log（覆盖 Controller catch 后包装返回的场景）。
/// </summary>
public sealed class PersistenceFailureErrorLogInterceptor : SaveChangesInterceptor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PersistenceFailureErrorLogInterceptor> _logger;
    private static int _reentry;

    public PersistenceFailureErrorLogInterceptor(
        IServiceScopeFactory scopeFactory,
        ILogger<PersistenceFailureErrorLogInterceptor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public override async Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await TryLogAsync(eventData).ConfigureAwait(false);
        await base.SaveChangesFailedAsync(eventData, cancellationToken).ConfigureAwait(false);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        _ = TryLogAsync(eventData);
        base.SaveChangesFailed(eventData);
    }

    private async Task TryLogAsync(DbContextErrorEventData eventData)
    {
        var ex = eventData.Exception;
        if (ex is not DbUpdateException and not DbUpdateConcurrencyException)
            return;

        if (eventData.Context?.ChangeTracker.Entries().Any(e => e.Entity is SysErrorLog) == true)
            return;

        if (Interlocked.CompareExchange(ref _reentry, 1, 0) != 0)
            return;

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var errorLog = scope.ServiceProvider.GetRequiredService<IErrorLogService>();
            var id = await errorLog.LogAsync(
                moduleName: "数据保存",
                errorMessage: RootMessage(ex),
                exception: ex,
                operationType: "SaveChanges",
                userId: SysErrorRequestContext.UserId,
                userName: SysErrorRequestContext.UserName,
                requestPath: SysErrorRequestContext.RequestPath);

            if (id.HasValue)
                SysErrorRequestContext.ErrorId = SysErrorLogIdFormat.Format(id.Value);
        }
        catch (Exception logEx)
        {
            _logger.LogError(logEx, "PersistenceFailureErrorLogInterceptor 写错误日志失败");
        }
        finally
        {
            Interlocked.Exchange(ref _reentry, 0);
        }
    }

    private static string RootMessage(Exception ex)
    {
        var cur = ex;
        while (cur.InnerException != null)
            cur = cur.InnerException;
        return string.IsNullOrWhiteSpace(cur.Message) ? ex.Message : cur.Message;
    }
}
