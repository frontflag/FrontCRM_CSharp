using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Interfaces.RfqAssignment;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services.RfqAssignment;

/// <summary>策略：采报优先（assign_method=5）。按 MPN 采购史 → 报价史 → 条目轮询。</summary>
public sealed class PurchaseQuotePriorityPurchaserAssignStrategy : IRfqPurchaserAssignStrategy
{
    private readonly IPurchaseQuoterPoolService _purchaseQuoterPoolService;
    private readonly IRfqMpnPurchaserAffinityLookup _mpnAffinityLookup;
    private readonly RfqPurchaserRoundRobinPicker _roundRobinPicker;
    private readonly ILogger<PurchaseQuotePriorityPurchaserAssignStrategy> _logger;

    public PurchaseQuotePriorityPurchaserAssignStrategy(
        IPurchaseQuoterPoolService purchaseQuoterPoolService,
        IRfqMpnPurchaserAffinityLookup mpnAffinityLookup,
        RfqPurchaserRoundRobinPicker roundRobinPicker,
        ILogger<PurchaseQuotePriorityPurchaserAssignStrategy> logger)
    {
        _purchaseQuoterPoolService = purchaseQuoterPoolService;
        _mpnAffinityLookup = mpnAffinityLookup;
        _roundRobinPicker = roundRobinPicker;
        _logger = logger;
    }

    public short AssignMethodCode => RfqAssignMethodCodes.PurchaseQuotePriority;
    public string DisplayName => "采报优先";

    public async Task<RfqPurchaserAssignmentOutcome> AssignAsync(
        RfqAssignmentContext context,
        CancellationToken cancellationToken = default)
    {
        var assignCount = await ResolveAssigneeCountAsync(cancellationToken);
        var pool = await _purchaseQuoterPoolService.GetOrderedActivePoolUserIdsAsync(cancellationToken);
        var poolSet = pool.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var assignments = new List<RfqItemAssigneePair>(context.Items.Count);
        foreach (var input in context.Items)
        {
            var (userId1, userId2) = await AssignLineAsync(
                input,
                assignCount,
                poolSet,
                cancellationToken);

            assignments.Add(new RfqItemAssigneePair
            {
                ItemKey = input.ItemKey,
                LineNo = input.LineNo,
                PurchaserUserId1 = userId1,
                PurchaserUserId2 = userId2
            });

            _logger.LogInformation(
                "【需求-采报优先】明细已分配：RfqCode={RfqCode} Trigger={Trigger} LineNo={LineNo} Mpn={Mpn} UserId1={UserId1} UserId2={UserId2}",
                context.RfqCode ?? context.RfqId,
                context.Trigger,
                input.LineNo,
                input.Mpn ?? "(empty)",
                userId1 ?? "(null)",
                userId2 ?? "(null)");
        }

        return new RfqPurchaserAssignmentOutcome
        {
            AssignMethodCode = AssignMethodCode,
            Assignments = assignments
        };
    }

    private async Task<(string? UserId1, string? UserId2)> AssignLineAsync(
        RfqItemAssignmentInput input,
        int assignCount,
        IReadOnlySet<string> poolSet,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.Mpn))
            return await TakeRoundRobinAsync(assignCount, cancellationToken);

        var fromPurchase = await _mpnAffinityLookup.GetPurchasersFromPurchaseHistoryAsync(
            input.Mpn,
            poolSet,
            assignCount,
            cancellationToken);
        if (fromPurchase.Count > 0)
            return await FillFromHistoryAsync(fromPurchase, assignCount, cancellationToken);

        var fromQuote = await _mpnAffinityLookup.GetPurchasersFromQuoteHistoryAsync(
            input.Mpn,
            poolSet,
            assignCount,
            cancellationToken);
        if (fromQuote.Count > 0)
            return await FillFromHistoryAsync(fromQuote, assignCount, cancellationToken);

        return await TakeRoundRobinAsync(assignCount, cancellationToken);
    }

    private async Task<(string? UserId1, string? UserId2)> FillFromHistoryAsync(
        IReadOnlyList<string> orderedPurchasers,
        int assignCount,
        CancellationToken cancellationToken)
    {
        var userId1 = orderedPurchasers[0];
        if (assignCount == 1)
            return (userId1, null);

        if (orderedPurchasers.Count >= 2)
            return (userId1, orderedPurchasers[1]);

        var userId2 = await _roundRobinPicker.TakeNextSingleExcludingAsync(userId1, cancellationToken);
        return (userId1, userId2);
    }

    private async Task<(string? UserId1, string? UserId2)> TakeRoundRobinAsync(
        int assignCount,
        CancellationToken cancellationToken)
    {
        if (assignCount == 1)
        {
            var single = await _roundRobinPicker.TakeNextSingleExcludingAsync(null, cancellationToken);
            return (single, null);
        }

        return await _roundRobinPicker.TakeNextPairAsync(cancellationToken);
    }

    private async Task<int> ResolveAssigneeCountAsync(CancellationToken cancellationToken)
    {
        var assignCount = await _purchaseQuoterPoolService.GetAssigneeCountAsync(cancellationToken);
        return assignCount is 1 or 2 ? assignCount : 2;
    }
}
