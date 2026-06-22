using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Interfaces.RfqAssignment;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services.RfqAssignment;

/// <summary>策略：相同品牌分配同一报价员（assign_method=3）。按品牌分组，每组从报价员池轮询取 N 人，同组明细共用。</summary>
public sealed class SameBrandPurchaserAssignStrategy : IRfqPurchaserAssignStrategy
{
    private readonly IPurchaseQuoterPoolService _purchaseQuoterPoolService;
    private readonly IRfqPurchaserRoundRobinCursorStore _cursorStore;
    private readonly ILogger<SameBrandPurchaserAssignStrategy> _logger;

    public SameBrandPurchaserAssignStrategy(
        IPurchaseQuoterPoolService purchaseQuoterPoolService,
        IRfqPurchaserRoundRobinCursorStore cursorStore,
        ILogger<SameBrandPurchaserAssignStrategy> logger)
    {
        _purchaseQuoterPoolService = purchaseQuoterPoolService;
        _cursorStore = cursorStore;
        _logger = logger;
    }

    public short AssignMethodCode => RfqAssignMethodCodes.SameBrandSamePurchaser;
    public string DisplayName => "品牌轮询";

    public async Task<RfqPurchaserAssignmentOutcome> AssignAsync(
        RfqAssignmentContext context,
        CancellationToken cancellationToken = default)
    {
        var brandAssignees = new Dictionary<string, (string? UserId1, string? UserId2)>(StringComparer.Ordinal);
        if (context.ExistingBrandAssignees != null)
        {
            foreach (var kv in context.ExistingBrandAssignees)
                brandAssignees[kv.Key] = kv.Value;
        }

        var assignments = new List<RfqItemAssigneePair>(context.Items.Count);
        foreach (var input in context.Items)
        {
            var brandKey = RfqAssignmentBrandKey.Resolve(input.BrandId, input.Brand);
            if (!brandAssignees.TryGetValue(brandKey, out var pair))
            {
                pair = await TakeNextRoundRobinPurchasersAsync(cancellationToken);
                brandAssignees[brandKey] = pair;
            }

            assignments.Add(new RfqItemAssigneePair
            {
                ItemKey = input.ItemKey,
                LineNo = input.LineNo,
                PurchaserUserId1 = pair.UserId1,
                PurchaserUserId2 = pair.UserId2
            });

            _logger.LogInformation(
                "【需求-同品牌分配】明细已分配：RfqCode={RfqCode} Trigger={Trigger} LineNo={LineNo} BrandKey={BrandKey} UserId1={UserId1} UserId2={UserId2}",
                context.RfqCode ?? context.RfqId,
                context.Trigger,
                input.LineNo,
                brandKey,
                pair.UserId1 ?? "(null)",
                pair.UserId2 ?? "(null)");
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
                "【需求-同品牌分配】报价员池为空，跳过分配。请在「采购参数 → 报价员池」中配置可参与轮询的采购员。");
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
            "【需求-同品牌分配】本组取值：池人数={PoolCount} 分配人数={AssignCount} CursorBefore={CursorBefore} " +
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
