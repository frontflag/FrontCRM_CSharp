using CRM.Core.Models.Purchase;

namespace CRM.Core.Interfaces;

/// <summary>采购订单更换供应商（主表 + 明细 + 未完结下游）。</summary>
public interface IPurchaseOrderVendorChangeService
{
    /// <summary>预检：阻断原因或未完结下游同步计数。</summary>
    Task<PurchaseOrderVendorChangePreviewResult> PreviewAsync(
        string purchaseOrderId,
        string newVendorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行更换：更新主表/明细供应商，并同步未完结下游。
    /// 调用前须已校验 <see cref="PurchaseOrderVendorChangeAccessRules.CanChangeVendor"/> 与数据权限。
    /// </summary>
    Task<PurchaseOrderVendorChangeApplyResult> ApplyAsync(
        PurchaseOrder order,
        string newVendorId,
        string? actingUserId = null,
        CancellationToken cancellationToken = default);
}

public class PurchaseOrderVendorChangePreviewResult
{
    public string PurchaseOrderId { get; set; } = string.Empty;
    public string? PurchaseOrderCode { get; set; }
    public string? OldVendorId { get; set; }
    public string? OldVendorName { get; set; }
    public string NewVendorId { get; set; } = string.Empty;
    public string? NewVendorName { get; set; }
    public bool CanChange { get; set; }
    public bool NoOp { get; set; }
    public string? BlockReason { get; set; }
    public List<string> BlockingDocuments { get; set; } = new();
    public int PoItemsToSync { get; set; }
    public int ArrivalNoticesToSync { get; set; }
    public int StockInsToSync { get; set; }
    public int PaymentsToSync { get; set; }
    public int PurchaseInvoicesToSync { get; set; }
}

public class PurchaseOrderVendorChangeApplyResult
{
    public PurchaseOrderVendorChangePreviewResult Preview { get; set; } = new();
    public bool Applied { get; set; }
}
