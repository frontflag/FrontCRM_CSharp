namespace CRM.Core.Interfaces;

/// <summary>报价主表状态与 SO / RFQ 生命周期联动。</summary>
public interface IQuoteStatusSyncService
{
    /// <summary>销售订单创建成功后，将关联报价标为成单（跳过已关闭）。</summary>
    Task MarkQuotesWonAsync(IEnumerable<string?> quoteIds, CancellationToken cancellationToken = default);

    /// <summary>销售订单删除/取消/明细变更后，按有效 SO 引用回写成单或新建。</summary>
    Task ReconcileQuotesAfterSalesOrderChangeAsync(
        IEnumerable<string?> quoteIds,
        CancellationToken cancellationToken = default);

    /// <summary>需求主单关闭或取消后，将该 RFQ 下仍为新建的报价标为关闭。</summary>
    Task CloseNewQuotesForRfqAsync(string rfqId, CancellationToken cancellationToken = default);
}
