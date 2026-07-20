using CRM.Core.Interfaces;

namespace CRM.API.Services;

/// <summary>
/// 定期清理埋点明细（90 天）与日汇总（400 天）。
/// </summary>
public sealed class TelemetryCleanupHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelemetryCleanupHostedService> _logger;

    public TelemetryCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<TelemetryCleanupHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 启动后稍候，避免与启动尖峰重叠
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var telemetry = scope.ServiceProvider.GetRequiredService<ITelemetryService>();
                await telemetry.CleanupExpiredAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Telemetry cleanup failed");
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
