namespace CRM.Core.Interfaces;

/// <summary>
/// 采购订单明细扩展表重算：采购/入库/付款/进项开票进度及数量、金额口径（与 purchaseorderitem 一对一）。
/// </summary>
public interface IPurchaseOrderItemExtendSyncService
{
    /// <summary>按采购明细 Id 重算扩展表（不存在时按明细创建扩展行）。</summary>
    Task RecalculateAsync(string purchaseOrderItemId, CancellationToken cancellationToken = default);

    /// <summary>进项发票变更后，按发票明细关联的入库单解析波及的采购明细并重算。</summary>
    Task RecalculateForFinancePurchaseInvoiceAsync(string financePurchaseInvoiceId, CancellationToken cancellationToken = default);

    /// <summary>解析进项发票波及的采购明细 Id（删除发票前调用，删除后对这些 Id 再 <see cref="RecalculateAsync"/>）。</summary>
    Task<IReadOnlyList<string>> ResolvePurchaseOrderItemIdsForFinancePurchaseInvoiceAsync(string financePurchaseInvoiceId,
        CancellationToken cancellationToken = default);
}
