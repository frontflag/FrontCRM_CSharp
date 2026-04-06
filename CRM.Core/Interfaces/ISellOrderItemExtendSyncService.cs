namespace CRM.Core.Interfaces;

/// <summary>
/// 销售订单明细扩展表进度重算（采购量、出库通知、实出、收款核销汇总）。
/// </summary>
public interface ISellOrderItemExtendSyncService
{
    /// <summary>按销售明细 Id 重算扩展表及 <see cref="Models.Sales.SellOrderItem.PurchasedQty"/>。</summary>
    Task RecalculateAsync(string sellOrderItemId, CancellationToken cancellationToken = default);
}
