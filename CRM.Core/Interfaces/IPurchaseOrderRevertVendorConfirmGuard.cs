namespace CRM.Core.Interfaces;

/// <summary>
/// 采购订单「取消确认」：已确认(30) 退回待确认(20) 前的下游单据校验。
/// </summary>
public interface IPurchaseOrderRevertVendorConfirmGuard
{
    /// <summary>存在有效付款单 / 到货通知 / 已过账采购入库 / 进项发票时抛 <see cref="InvalidOperationException"/>。</summary>
    Task EnsureCanRevertAsync(string purchaseOrderId, string? purchaseOrderCode);
}
