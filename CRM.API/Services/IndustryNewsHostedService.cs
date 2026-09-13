using CRM.Core.Interfaces;
using CRM.Core.Utilities;

namespace CRM.API.Services;

/// <summary>每天上海 08:00 生成行业新闻简报；失败保留上一份成功简报并重试。</summary>
public sealed class IndustryNewsHostedService : BackgroundService
{
    readonly IServiceScopeFactory _scopeFactory;
    readonly ILogger<IndustryNewsHostedService> _logger;

    public IndustryNewsHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<IndustryNewsHostedService> logger)
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
                _logger.LogWarning(ex, "Industry news daily job failed");
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
        var shanghaiNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CommissionShanghai.Zone);
        if (!IndustryNewsWindow.CanRunAt(shanghaiNow))
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IIndustryNewsService>();
        var result = await service.RunForTodayAsync(force: false, ct);
        if (result.Ran)
        {
            _logger.LogInformation(
                "Industry news daily job done success={Success} message={Message}",
                result.Success, result.Message);
        }
    }

    async Task LogErrorAsync(Exception ex)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var errors = scope.ServiceProvider.GetRequiredService<IErrorLogService>();
            await errors.LogAsync("industry-news", ex.Message, ex, "daily-job");
        }
        catch (Exception logEx)
        {
            _logger.LogWarning(logEx, "Failed to write industry news job error log");
        }
    }
}
