using CRM.Core.Interfaces;
using CRM.Core.Interfaces.RfqAssignment;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services.RfqAssignment;

/// <summary>报价员池轮询取人（条目轮询与采报优先兜底共用）。</summary>
public sealed class RfqPurchaserRoundRobinPicker
{
    private readonly IPurchaseQuoterPoolService _purchaseQuoterPoolService;
    private readonly IRfqPurchaserRoundRobinCursorStore _cursorStore;
    private readonly ILogger<RfqPurchaserRoundRobinPicker> _logger;

    public RfqPurchaserRoundRobinPicker(
        IPurchaseQuoterPoolService purchaseQuoterPoolService,
        IRfqPurchaserRoundRobinCursorStore cursorStore,
        ILogger<RfqPurchaserRoundRobinPicker> logger)
    {
        _purchaseQuoterPoolService = purchaseQuoterPoolService;
        _cursorStore = cursorStore;
        _logger = logger;
    }

    public async Task<(string? UserId1, string? UserId2)> TakeNextPairAsync(CancellationToken cancellationToken = default)
    {
        var pool = await _purchaseQuoterPoolService.GetOrderedActivePoolUserIdsAsync(cancellationToken);
        var n = pool.Count;
        if (n == 0)
        {
            _logger.LogWarning("【需求-轮询取人】报价员池为空，跳过分配。");
            return (null, null);
        }

        var assignCount = await ResolveAssigneeCountAsync(cancellationToken);
        var cursor = await _cursorStore.GetCursorAsync(cancellationToken);
        var ids = new List<string>(assignCount);
        for (var i = 0; i < assignCount; i++)
            ids.Add(pool[(cursor + i) % n]);

        await _cursorStore.SaveCursorAsync(cursor + assignCount, cancellationToken);

        var a1 = ids[0];
        var a2 = assignCount >= 2 ? ids[1] : null;
        return (a1, a2);
    }

    public async Task<string?> TakeNextSingleExcludingAsync(
        string? excludeUserId,
        CancellationToken cancellationToken = default)
    {
        var pool = await _purchaseQuoterPoolService.GetOrderedActivePoolUserIdsAsync(cancellationToken);
        var n = pool.Count;
        if (n == 0)
        {
            _logger.LogWarning("【需求-轮询取人】报价员池为空，跳过分配。");
            return null;
        }

        var cursor = await _cursorStore.GetCursorAsync(cancellationToken);
        string? picked = null;
        var advance = 0;

        for (var i = 0; i < n; i++)
        {
            var candidate = pool[(cursor + i) % n];
            advance = i + 1;
            if (!string.IsNullOrWhiteSpace(excludeUserId)
                && string.Equals(candidate, excludeUserId.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            picked = candidate;
            break;
        }

        if (picked == null)
        {
            picked = pool[cursor % n];
            advance = 1;
        }

        await _cursorStore.SaveCursorAsync(cursor + advance, cancellationToken);
        return picked;
    }

    private async Task<int> ResolveAssigneeCountAsync(CancellationToken cancellationToken)
    {
        var assignCount = await _purchaseQuoterPoolService.GetAssigneeCountAsync(cancellationToken);
        return assignCount is 1 or 2 ? assignCount : 2;
    }
}
