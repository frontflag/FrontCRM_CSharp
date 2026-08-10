using CRM.API.Services.Implementations;
using CRM.API.Services.Interfaces;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.API.Services;

/// <summary>
/// 每日 08:30（Asia/Shanghai）自动同步所有已验证 IMAP 邮箱未读邮件。
/// </summary>
public sealed class MailSyncHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MailSyncHostedService> _logger;

    public MailSyncHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<MailSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TryRunDailyAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Mail sync daily job failed");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task TryRunDailyAsync(CancellationToken ct)
    {
        var utcNow = DateTime.UtcNow;
        if (!UserMailSyncService.IsPastShanghaiDailySlot(utcNow))
            return;

        var dateKey = UserMailSyncService.ShanghaiDateKey(utcNow);
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // 多实例：插入当日行成功者执行；已存在则跳过
        var existing = await db.MailSyncDailyRuns.AsNoTracking()
            .FirstOrDefaultAsync(x => x.RunDate == dateKey, ct);
        if (existing != null)
            return;

        var run = new MailSyncDailyRun
        {
            RunDate = dateKey,
            StartedAt = DateTime.UtcNow
        };
        try
        {
            db.MailSyncDailyRuns.Add(run);
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // 并发插入失败：另一实例已占位
            return;
        }

        _logger.LogInformation("Mail sync daily job start runDate={RunDate}", dateKey);
        var sync = scope.ServiceProvider.GetRequiredService<IUserMailSyncService>();
        var result = await sync.SyncAllUsersAsync(ct);

        run.FinishedAt = DateTime.UtcNow;
        run.OkCount = result.OkCount;
        run.FailCount = result.FailCount;
        db.MailSyncDailyRuns.Update(run);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "Mail sync daily job done runDate={RunDate} users={Users} ok={Ok} fail={Fail}",
            dateKey, result.UserCount, result.OkCount, result.FailCount);
    }
}
