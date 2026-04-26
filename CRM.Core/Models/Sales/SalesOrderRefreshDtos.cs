namespace CRM.Core.Models.Sales;

/// <summary>
/// 销售单明细扩展刷新结果（用于前端提示是否有更新数据）。
/// </summary>
public class SalesOrderItemExtendRefreshResult
{
    public string SalesOrderId { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int ChangedItems { get; set; }
    public int ChangedFieldsCount { get; set; }
    public int SyncedStockOutNotifyStatusCount { get; set; }
    public DateTime RefreshedAt { get; set; } = DateTime.UtcNow;
    public List<SalesOrderItemExtendChangeDto> Changes { get; set; } = new();
}

public class SalesOrderItemExtendChangeDto
{
    public string SellOrderItemId { get; set; } = string.Empty;
    public string? SellOrderItemCode { get; set; }
    public List<SalesOrderItemExtendFieldChangeDto> Fields { get; set; } = new();
}

public class SalesOrderItemExtendFieldChangeDto
{
    public string Field { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Before { get; set; } = string.Empty;
    public string After { get; set; } = string.Empty;
}

