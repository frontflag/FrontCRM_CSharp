using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Interfaces.RfqAssignment;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services.RfqAssignment;

/// <summary>策略：条目轮询（assign_method=2）。每条明细从报价员池取连续 N 人，全局游标每条 +N。</summary>
public sealed class ItemRoundRobinPurchaserAssignStrategy : IRfqPurchaserAssignStrategy
{
    private readonly RfqPurchaserRoundRobinPicker _roundRobinPicker;
    private readonly ILogger<ItemRoundRobinPurchaserAssignStrategy> _logger;

    public ItemRoundRobinPurchaserAssignStrategy(
        RfqPurchaserRoundRobinPicker roundRobinPicker,
        ILogger<ItemRoundRobinPurchaserAssignStrategy> logger)
    {
        _roundRobinPicker = roundRobinPicker;
        _logger = logger;
    }

    public short AssignMethodCode => RfqAssignMethodCodes.ItemRoundRobin;
    public string DisplayName => "条目轮询";

    public async Task<RfqPurchaserAssignmentOutcome> AssignAsync(
        RfqAssignmentContext context,
        CancellationToken cancellationToken = default)
    {
        var assignments = new List<RfqItemAssigneePair>(context.Items.Count);
        foreach (var input in context.Items)
        {
            var (userId1, userId2) = await _roundRobinPicker.TakeNextPairAsync(cancellationToken);
            assignments.Add(new RfqItemAssigneePair
            {
                ItemKey = input.ItemKey,
                LineNo = input.LineNo,
                PurchaserUserId1 = userId1,
                PurchaserUserId2 = userId2
            });

            _logger.LogInformation(
                "【需求-条目轮询】明细已分配：RfqCode={RfqCode} Trigger={Trigger} LineNo={LineNo} UserId1={UserId1} UserId2={UserId2}",
                context.RfqCode ?? context.RfqId,
                context.Trigger,
                input.LineNo,
                userId1 ?? "(null)",
                userId2 ?? "(null)");
        }

        return new RfqPurchaserAssignmentOutcome
        {
            AssignMethodCode = AssignMethodCode,
            Assignments = assignments
        };
    }
}
