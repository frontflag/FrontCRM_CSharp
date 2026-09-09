using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.API.Services;

/// <summary>每天上海 00:00 重算预计提成。</summary>
public sealed class CommissionDynamicHostedService : BackgroundService
{
    readonly IServiceScopeFactory _scopeFactory;
    readonly ILogger<CommissionDynamicHostedService> _logger;

    public CommissionDynamicHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<CommissionDynamicHostedService> logger)
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
                _logger.LogWarning(ex, "Commission dynamic daily job failed");
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
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var watermark = await db.CommissionJobWatermarks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == CommissionJobWatermarkCode.SeedId, ct);
        if (watermark?.DynamicJobDate == today)
            return;

        _logger.LogInformation("Commission dynamic daily job start calcDate={CalcDate}", today);
        var calc = scope.ServiceProvider.GetRequiredService<ICommissionDynamicCalculator>();
        var result = await calc.RecalcAsync(today, ct);
        _logger.LogInformation(
            "Commission dynamic daily job done sales={Sales} purchase={Purchase}",
            result.SalesCount, result.PurchaseCount);
    }

    async Task LogErrorAsync(Exception ex)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var errors = scope.ServiceProvider.GetRequiredService<IErrorLogService>();
            await errors.LogAsync("commission", ex.Message, ex, "dynamic-job");
        }
        catch (Exception logEx)
        {
            _logger.LogWarning(logEx, "Failed to write commission dynamic job error log");
        }
    }
}
