using CRM.Core.Models.System;

namespace CRM.Core.Interfaces;

/// <summary>订单旅程日志写入与查询（表 <c>log_orderjourney</c>）。</summary>
public interface IOrderJourneyLogService
{
    /// <summary>追加一条旅程记录；失败时不抛出，避免影响主业务。</summary>
    Task AppendAsync(OrderJourneyLog entry, CancellationToken cancellationToken = default);

    /// <summary>按销售订单 Id 查询主单及下属明细相关旅程，按事件时间升序。</summary>
    Task<IReadOnlyList<OrderJourneyLog>> GetBySellOrderIdAsync(string sellOrderId, CancellationToken cancellationToken = default);

    /// <summary>按采购订单 Id 查询主单及下属明细相关旅程，按事件时间升序。</summary>
    Task<IReadOnlyList<OrderJourneyLog>> GetByPurchaseOrderIdAsync(string purchaseOrderId, CancellationToken cancellationToken = default);
}
