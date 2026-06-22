using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Interfaces.RfqAssignment;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services.RfqAssignment;

/// <summary>策略：条目轮询（assign_method=2）。每条明细从报价员池取连续 N 人，全局游标每条 +N。</summary>
public sealed class ItemRoundRobinPurchaserAssignStrategy : IRfqPurchaserAssignStrategy
{
    private readonly IPurchaseQuoterPoolService _purchaseQuoterPoolService;
    private readonly IRfqPurchaserRoundRobinCursorStore _cursorStore;
    private readonly ILogger<ItemRoundRobinPurchaserAssignStrategy> _logger;

    public ItemRoundRobinPurchaserAssignStrategy(
        IPurchaseQuoterPoolService purchaseQuoterPoolService,
        IRfqPurchaserRoundRobinCursorStore cursorStore,
        ILogger<ItemRoundRobinPurchaserAssignStrategy> logger)
    {
        _purchaseQuoterPoolService = purchaseQuoterPoolService;
        _cursorStore = cursorStore;
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
            var (userId1, userId2) = await TakeNextRoundRobinPurchasersAsync(cancellationToken);
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

    private async Task<(string? UserId1, string? UserId2)> TakeNextRoundRobinPurchasersAsync(
        CancellationToken cancellationToken)
    {
        var pool = await _purchaseQuoterPoolService.GetOrderedActivePoolUserIdsAsync(cancellationToken);
        var n = pool.Count;
        if (n == 0)
        {
            _logger.LogWarning(
                "【需求-条目轮询】报价员池为空，跳过分配。请在「采购参数 → 报价员池」中配置可参与轮询的采购员。");
            return (null, null);
        }

        var assignCount = await _purchaseQuoterPoolService.GetAssigneeCountAsync(cancellationToken);
        if (assignCount is not (1 or 2))
            assignCount = 2;

        var cursor = await _cursorStore.GetCursorAsync(cancellationToken);
        var ids = new List<string>(assignCount);
        for (var i = 0; i < assignCount; i++)
            ids.Add(pool[(cursor + i) % n]);

        await _cursorStore.SaveCursorAsync(cursor + assignCount, cancellationToken);

        var a1 = ids[0];
        var a2 = assignCount >= 2 ? ids[1] : null;
        _logger.LogInformation(
            "【需求-条目轮询】本笔取值：池人数={PoolCount} 分配人数={AssignCount} CursorBefore={CursorBefore} " +
            "UserId1={UserId1} UserId2={UserId2} CursorAfter={CursorAfter}",
            n,
            assignCount,
            cursor,
            a1,
            a2 ?? "(null)",
            cursor + assignCount);

        return (a1, a2);
    }
}
