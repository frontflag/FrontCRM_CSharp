namespace CRM.Core.Interfaces.RfqAssignment;

/// <summary>条目/整单轮询策略共用的全局游标持久化。</summary>
public interface IRfqPurchaserRoundRobinCursorStore
{
    Task<int> GetCursorAsync(CancellationToken cancellationToken = default);
    Task SaveCursorAsync(int cursor, CancellationToken cancellationToken = default);
}
