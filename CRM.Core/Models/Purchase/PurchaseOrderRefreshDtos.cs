namespace CRM.Core.Models.Purchase;

/// <summary>
/// 采购单明细扩展刷新结果（用于前端提示是否有更新数据）。
/// </summary>
public class PurchaseOrderItemExtendRefreshResult
{
    public string PurchaseOrderId { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int ChangedItems { get; set; }
    public int ChangedFieldsCount { get; set; }
    public int SyncedPurchaseRequisitionStatusCount { get; set; }
    public int SyncedArrivalNoticeStatusCount { get; set; }
    public DateTime RefreshedAt { get; set; } = DateTime.UtcNow;
    public List<PurchaseOrderItemExtendChangeDto> Changes { get; set; } = new();
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

