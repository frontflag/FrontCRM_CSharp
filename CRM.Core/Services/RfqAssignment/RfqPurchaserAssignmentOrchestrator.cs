using CRM.Core.Interfaces.RfqAssignment;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services.RfqAssignment;

public sealed class RfqPurchaserAssignmentOrchestrator : IRfqPurchaserAssignmentOrchestrator
{
    private readonly IReadOnlyDictionary<short, IRfqPurchaserAssignStrategy> _strategies;
    private readonly ILogger<RfqPurchaserAssignmentOrchestrator> _logger;

    public RfqPurchaserAssignmentOrchestrator(
        IEnumerable<IRfqPurchaserAssignStrategy> strategies,
        ILogger<RfqPurchaserAssignmentOrchestrator> logger)
    {
        _strategies = strategies.ToDictionary(s => s.AssignMethodCode);
        _logger = logger;
    }

    public Task<RfqPurchaserAssignmentOutcome> AssignAsync(
        short assignMethod,
        RfqAssignmentContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_strategies.TryGetValue(assignMethod, out var strategy))
        {
            _logger.LogWarning(
                "【需求-采购员分配】未注册策略 AssignMethod={AssignMethod}，RfqId={RfqId}",
                assignMethod,
                context.RfqId);
            throw new InvalidOperationException($"未支持的分配方式: {assignMethod}");
        }

        _logger.LogInformation(
            "【需求-采购员分配】使用策略 {Strategy}({Code})，RfqId={RfqId} Trigger={Trigger} ItemCount={ItemCount}",
            strategy.DisplayName,
            strategy.AssignMethodCode,
            context.RfqId,
            context.Trigger,
            context.Items.Count);

        return strategy.AssignAsync(context, cancellationToken);
    }
}
