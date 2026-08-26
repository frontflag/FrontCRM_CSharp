using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Interfaces.RfqAssignment;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services.RfqAssignment;

/// <summary>策略：指定采购（assign_method=4）。整单明细只写所选一人到槽位 1，忽略报价人数 N。</summary>
public sealed class DesignatedPurchaserAssignStrategy : IRfqPurchaserAssignStrategy
{
    private readonly IPurchaseQuoterPoolService _purchaseQuoterPoolService;
    private readonly ILogger<DesignatedPurchaserAssignStrategy> _logger;

    public DesignatedPurchaserAssignStrategy(
        IPurchaseQuoterPoolService purchaseQuoterPoolService,
        ILogger<DesignatedPurchaserAssignStrategy> logger)
    {
        _purchaseQuoterPoolService = purchaseQuoterPoolService;
        _logger = logger;
    }

    public short AssignMethodCode => RfqAssignMethodCodes.DesignatedPurchaser;
    public string DisplayName => "指定采购";

    public async Task<RfqPurchaserAssignmentOutcome> AssignAsync(
        RfqAssignmentContext context,
        CancellationToken cancellationToken = default)
    {
        var userId = context.DesignatedPurchaserUserId?.Trim();
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("请选择分配采购");

        if (!context.AllowDesignatedPurchaserOutsidePool)
        {
            var pool = await _purchaseQuoterPoolService.GetOrderedActivePoolUserIdsAsync(cancellationToken);
            var inPool = pool.Any(id => string.Equals(id, userId, StringComparison.OrdinalIgnoreCase));
            if (!inPool)
                throw new ArgumentException("分配采购须为报价员池中已勾选且在职的账号");
        }

        var assignments = context.Items.Select(input => new RfqItemAssigneePair
        {
            ItemKey = input.ItemKey,
            LineNo = input.LineNo,
            PurchaserUserId1 = userId,
            PurchaserUserId2 = null
        }).ToList();

        _logger.LogInformation(
            "【需求-指定采购】已分配：RfqCode={RfqCode} Trigger={Trigger} ItemCount={ItemCount} UserId={UserId} AllowOutsidePool={AllowOutside}",
            context.RfqCode ?? context.RfqId,
            context.Trigger,
            context.Items.Count,
            userId,
            context.AllowDesignatedPurchaserOutsidePool);

        return new RfqPurchaserAssignmentOutcome
        {
            AssignMethodCode = AssignMethodCode,
            Assignments = assignments
        };
    }
}
