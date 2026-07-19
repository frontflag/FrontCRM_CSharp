using CRM.Core.Models.Sales;

namespace CRM.Core.Interfaces;

/// <summary>销售订单下游客户信息同步（MVP：未出库完成链路；已完结财务/出库单据阻断）。</summary>
public interface ISalesOrderCustomerDownstreamSyncService
{
    Task<SalesOrderCustomerDownstreamSyncPreviewResult> PreviewAsync(
        string salesOrderId,
        CancellationToken cancellationToken = default);

    Task<SalesOrderCustomerDownstreamSyncApplyResult> ApplyAsync(
        SellOrder order,
        string? actingUserId = null,
        CancellationToken cancellationToken = default);
}

public class SalesOrderCustomerDownstreamSyncPreviewResult
{
    public string SalesOrderId { get; set; } = string.Empty;
    public string? SellOrderCode { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public bool CanSync { get; set; }
    public bool NoOp { get; set; }
    public string? BlockReason { get; set; }
    public List<string> BlockingDocuments { get; set; } = new();
    public int StockOutNotifiesToSync { get; set; }
    public int PackingsToSync { get; set; }
    public int PackingItemExtendsToSync { get; set; }
    public int StockOutsToSync { get; set; }
    public List<SalesOrderCustomerDownstreamSyncPreviewItem> SyncItems { get; set; } = new();
}

public class SalesOrderCustomerDownstreamSyncPreviewItem
{
    /// <summary>stockOutNotify | packing | packingItemExtend | stockOut</summary>
    public string Category { get; set; } = string.Empty;
    public string DocumentCode { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public bool IsMismatch { get; set; }
}

public class SalesOrderCustomerDownstreamSyncApplyResult
{
    public SalesOrderCustomerDownstreamSyncPreviewResult Preview { get; set; } = new();
    public bool Applied { get; set; }
}
