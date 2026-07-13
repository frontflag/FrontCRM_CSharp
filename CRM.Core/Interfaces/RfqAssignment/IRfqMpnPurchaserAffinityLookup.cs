namespace CRM.Core.Interfaces.RfqAssignment;

/// <summary>按物料型号查询历史采购员/报价员（池内、有效单据、精确 MPN）。</summary>
public interface IRfqMpnPurchaserAffinityLookup
{
    /// <summary>采购单历史：按最近采购时间降序返回 distinct 采购员（最多 <paramref name="maxCount"/> 人）。</summary>
    Task<IReadOnlyList<string>> GetPurchasersFromPurchaseHistoryAsync(
        string mpn,
        IReadOnlySet<string> poolUserIds,
        int maxCount,
        CancellationToken cancellationToken = default);

    /// <summary>报价单历史：按最近报价时间降序返回 distinct 报价采购员（最多 <paramref name="maxCount"/> 人）。</summary>
    Task<IReadOnlyList<string>> GetPurchasersFromQuoteHistoryAsync(
        string mpn,
        IReadOnlySet<string> poolUserIds,
        int maxCount,
        CancellationToken cancellationToken = default);
}
