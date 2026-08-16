using CRM.Core.Models.Purchase;

namespace CRM.Core.Interfaces;

/// <summary>
/// 以采购订单行当前单价 / 币别 / 折算美金为准，无状态门控覆盖下游采购价快照。
/// </summary>
public interface IPurchaseOrderPurchasePriceDownstreamSyncService
{
    Task<PurchaseOrderPurchasePriceDownstreamSyncResult> ApplyAsync(
        IReadOnlyList<PurchaseOrderItem> items,
        CancellationToken cancellationToken = default);
}

public class PurchaseOrderPurchasePriceDownstreamSyncResult
{
    public int ArrivalNoticesUpdated { get; set; }
    public int StockInItemsUpdated { get; set; }
    public int StockInHeadersUpdated { get; set; }
    public int StockInItemExtendsUpdated { get; set; }
    public int StockItemsUpdated { get; set; }
    public int StockOutItemExtendsUpdated { get; set; }
    public List<PurchaseOrderPurchasePriceLineChangeDto> LineChanges { get; set; } = new();
    public List<PurchaseOrderInvoiceMatchWarningDto> InvoiceMatchWarnings { get; set; } = new();
    public List<PurchaseOrderPaymentOverWarningDto> PaymentOverWarnings { get; set; } = new();

    public bool HasUpdates =>
        ArrivalNoticesUpdated > 0
        || StockInItemsUpdated > 0
        || StockInHeadersUpdated > 0
        || StockInItemExtendsUpdated > 0
        || StockItemsUpdated > 0
        || StockOutItemExtendsUpdated > 0
        || LineChanges.Count > 0
        || InvoiceMatchWarnings.Count > 0
        || PaymentOverWarnings.Count > 0;
}
