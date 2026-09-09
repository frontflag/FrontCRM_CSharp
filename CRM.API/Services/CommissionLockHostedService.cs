using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.API.Services;

/// <summary>偶数月 2 日上海 00:00 锁定上一双月正式提成。</summary>
public sealed class CommissionLockHostedService : BackgroundService
{
    readonly IServiceScopeFactory _scopeFactory;
    readonly ILogger<CommissionLockHostedService> _logger;

    public CommissionLockHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<CommissionLockHostedService> logger)
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
                await TryRunAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Commission lock job failed");
                await LogErrorAsync(ex);
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

    async Task TryRunAsync(CancellationToken ct)
    {
        var today = CommissionShanghai.Today();
        if (!CommissionTerm.IsLockDate(today))
            return;

        var term = CommissionTerm.GetWindow(today).Term;
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var watermark = await db.CommissionJobWatermarks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == CommissionJobWatermarkCode.SeedId, ct);
        if (string.Equals(watermark?.LockTerm?.Trim(), term, StringComparison.Ordinal))
            return;

        _logger.LogInformation("Commission lock job start lockDate={LockDate} term={Term}", today, term);
        var locker = scope.ServiceProvider.GetRequiredService<ICommissionLockService>();
        var result = await locker.LockAsync(today, ct);
        _logger.LogInformation(
            "Commission lock job done term={Term} inserted={Inserted} skipped={Skipped}",
            result.Term, result.Inserted, result.SkippedExisting);
    }

    async Task LogErrorAsync(Exception ex)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var errors = scope.ServiceProvider.GetRequiredService<IErrorLogService>();
            await errors.LogAsync("commission", ex.Message, ex, "lock-job");
        }
        catch (Exception logEx)
        {
            _logger.LogWarning(logEx, "Failed to write commission lock job error log");
        }
    }
}
