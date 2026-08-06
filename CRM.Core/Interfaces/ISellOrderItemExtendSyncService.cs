namespace CRM.Core.Interfaces;

/// <summary>
/// 销售订单明细扩展表进度重算（采购量、出库通知、实出、收款核销汇总）。
/// </summary>
public interface ISellOrderItemExtendSyncService
{
    /// <summary>
    /// 按销售明细 Id 重算扩展表及 <see cref="Models.Sales.SellOrderItem.PurchasedQty"/>。
    /// <paramref name="enforceLineQtyOutboundGuards"/>：销售行改数量时为 true（校验通知/实出不超过销售数量）；
    /// 出库强制删除等回写场景传 false，避免历史「行数量 &lt; 已实出」脏数据阻断删除。
    /// </summary>
    Task RecalculateAsync(
        string sellOrderItemId,
        CancellationToken cancellationToken = default,
        bool enforceLineQtyOutboundGuards = true);
}
