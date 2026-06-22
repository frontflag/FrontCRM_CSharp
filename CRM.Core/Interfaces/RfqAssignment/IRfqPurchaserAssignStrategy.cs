namespace CRM.Core.Interfaces.RfqAssignment;

/// <summary>需求明细询价采购员/报价员分配策略。</summary>
public interface IRfqPurchaserAssignStrategy
{
    short AssignMethodCode { get; }
    string DisplayName { get; }

    Task<RfqPurchaserAssignmentOutcome> AssignAsync(
        RfqAssignmentContext context,
        CancellationToken cancellationToken = default);
}
