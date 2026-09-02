using CRM.Core.Interfaces;

namespace CRM.Core.Models.Purchase;

/// <summary>
/// 采购单明细扩展刷新结果（用于前端提示是否有更新数据）。
/// </summary>
public class PurchaseOrderItemExtendRefreshResult
{
    /// <summary><c>status</c> / <c>vendor</c> / <c>pn</c> / <c>brand</c> / <c>qty</c> / <c>price</c></summary>
    public string Facet { get; set; } = "status";
    public string PurchaseOrderId { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int ChangedItems { get; set; }
    public int ChangedFieldsCount { get; set; }
    public int SyncedPurchaseRequisitionStatusCount { get; set; }
    public int SyncedArrivalNoticeStatusCount { get; set; }
    public DateTime RefreshedAt { get; set; } = DateTime.UtcNow;
    public List<PurchaseOrderItemExtendChangeDto> Changes { get; set; } = new();

    public int ArrivalNoticesUpdated { get; set; }
    public int StockInItemsUpdated { get; set; }
    public int StockInHeadersUpdated { get; set; }
    public int StockInItemExtendsUpdated { get; set; }
    public int StockItemsUpdated { get; set; }
    public int StockOutItemExtendsUpdated { get; set; }
    public List<PurchaseOrderPurchasePriceLineChangeDto> PurchasePriceLineChanges { get; set; } = new();
    public List<PurchaseOrderInvoiceMatchWarningDto> InvoiceMatchWarnings { get; set; } = new();
    public List<PurchaseOrderPaymentOverWarningDto> PaymentOverWarnings { get; set; } = new();

    public int PackingItemsUpdated { get; set; }
    public int CustomsDeclarationItemsUpdated { get; set; }
    public int StockItemsMoved { get; set; }
    public int StockAggregatesCreated { get; set; }
    public int StockAggregatesRemoved { get; set; }
    public List<PurchaseOrderIdentitySnapshotChangeDto> IdentityChanges { get; set; } = new();
    public string? OldVendorName { get; set; }
    public string? NewVendorName { get; set; }
}

public class PurchaseOrderPurchasePriceLineChangeDto
{
    public string PurchaseOrderItemId { get; set; } = string.Empty;
    public string? PurchaseOrderItemCode { get; set; }
    public decimal OldCost { get; set; }
    public decimal NewCost { get; set; }
    public short OldCurrency { get; set; }
    public short NewCurrency { get; set; }
    public decimal OldConvertPrice { get; set; }
    public decimal NewConvertPrice { get; set; }
}

public class PurchaseOrderInvoiceMatchWarningDto
{
    public string StockInItemId { get; set; } = string.Empty;
    public string? StockInItemCode { get; set; }
    public string PurchaseOrderItemId { get; set; } = string.Empty;
    public string? PurchaseOrderItemCode { get; set; }
    public decimal Amount { get; set; }
    public decimal InvoiceMatchDone { get; set; }
    public decimal InvoiceMatchToBe { get; set; }
}

public class PurchaseOrderPaymentOverWarningDto
{
    public string PurchaseOrderItemId { get; set; } = string.Empty;
    public string? PurchaseOrderItemCode { get; set; }
    public decimal LineAmount { get; set; }
    public decimal PaymentDone { get; set; }
}

public class PurchaseOrderItemExtendChangeDto
{
    public string PurchaseOrderItemId { get; set; } = string.Empty;
    public string? PurchaseOrderItemCode { get; set; }
    public List<PurchaseOrderItemExtendFieldChangeDto> Fields { get; set; } = new();
}

public class PurchaseOrderItemExtendFieldChangeDto
{
    public string Field { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Before { get; set; } = string.Empty;
    public string After { get; set; } = string.Empty;
}

/// <summary>
/// 批量重算到货通知 <c>stockin_notify.Status</c>（与扩展重算内 <c>RecalculateArrivalNoticeStatusesForPoLineAsync</c> 同源）。
/// </summary>
public class ArrivalNoticeStatusBatchRecalculateResult
{
    public int TotalNotices { get; set; }
    public int ChangedCount { get; set; }
    /// <summary>其中修正为已入库(100) 的条数（含自 30 升级等）。</summary>
    public int ToStockedInCount { get; set; }
    public List<string> ChangedNoticeCodes { get; set; } = new();
}

