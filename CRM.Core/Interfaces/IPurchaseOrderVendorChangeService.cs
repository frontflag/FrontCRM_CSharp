using CRM.Core.Models.Purchase;

namespace CRM.Core.Interfaces;

/// <summary>
/// 采购订单供应商同步：更换供应商，或按 VendorId 刷新头名称快照并同步未完结下游（对齐销售订单「刷新客户」）。
/// </summary>
public interface IPurchaseOrderVendorChangeService
{
    /// <summary>预检：拟换/目标供应商下的同步计数与阻断原因。</summary>
    Task<PurchaseOrderVendorChangePreviewResult> PreviewAsync(
        string purchaseOrderId,
        string newVendorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行：更新主表/明细供应商（含同 ID 刷新名称），并同步未完结下游。
    /// 调用前须已校验权限与数据范围（换 ID 时须 <see cref="PurchaseOrderVendorChangeAccessRules.CanChangeVendor"/>）。
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
    /// <summary>目标与当前是否为同一供应商 ID（仅刷名称/下游快照）。</summary>
    public bool SameVendorId { get; set; }
    public string? BlockReason { get; set; }
    public List<string> BlockingDocuments { get; set; } = new();
    /// <summary>采购订单头供应商名称快照是否需按主数据刷新（0/1）。</summary>
    public int PoVendorNameToSync { get; set; }
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
