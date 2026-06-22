namespace CRM.Core.Interfaces.RfqAssignment;

/// <summary>按 assign_method 路由到具体分配策略。</summary>
public interface IRfqPurchaserAssignmentOrchestrator
{
    Task<RfqPurchaserAssignmentOutcome> AssignAsync(
        short assignMethod,
        RfqAssignmentContext context,
        CancellationToken cancellationToken = default);
}
