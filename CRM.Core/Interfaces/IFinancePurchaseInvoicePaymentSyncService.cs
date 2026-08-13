namespace CRM.Core.Interfaces;

/// <summary>
/// 进项发票付款缓存：按核销流水落到 PO 明细的已付金额回写 PaymentDone/PaymentToBe/PaymentStatus。
/// 状态相对 <c>VerifiedDone</c>，不是发票总额。
/// </summary>
public interface IFinancePurchaseInvoicePaymentSyncService
{
    Task RecalculateForInvoiceAsync(string financePurchaseInvoiceId, CancellationToken cancellationToken = default);

    Task RecalculateForPurchaseOrderItemAsync(string purchaseOrderItemId, CancellationToken cancellationToken = default);
}
