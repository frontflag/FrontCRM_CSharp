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

