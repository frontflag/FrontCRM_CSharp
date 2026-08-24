using CRM.Core.Models.Sales;

namespace CRM.Core.Interfaces;

/// <summary>
/// 销售订单客户信息同步：以订单 CustomerId（或拟换客户）为准刷新头名称快照，并同步未完结下游 customer_id。
/// </summary>
public interface ISalesOrderCustomerDownstreamSyncService
{
    /// <param name="proposedCustomerId">拟换客户 ID；为空则按订单已落库 CustomerId 预检（详情「刷新客户」）。</param>
    Task<SalesOrderCustomerDownstreamSyncPreviewResult> PreviewAsync(
        string salesOrderId,
        string? proposedCustomerId = null,
        CancellationToken cancellationToken = default);

    /// <param name="proposedCustomerId">拟换客户 ID；为空则按订单当前 CustomerId 同步。</param>
    /// <param name="saveChanges">为 false 时仅改实体（由调用方统一 SaveChanges，如编辑保存一步提交）。</param>
    Task<SalesOrderCustomerDownstreamSyncApplyResult> ApplyAsync(
        SellOrder order,
        string? actingUserId = null,
        string? proposedCustomerId = null,
        bool saveChanges = true,
        CancellationToken cancellationToken = default);
}

public class SalesOrderCustomerDownstreamSyncPreviewResult
{
    public string SalesOrderId { get; set; } = string.Empty;
    public string? SellOrderCode { get; set; }
    /// <summary>目标客户 ID（拟换或已落库）。</summary>
    public string? CustomerId { get; set; }
    /// <summary>目标客户名称（主数据）。</summary>
    public string? CustomerName { get; set; }
    public string? OldCustomerId { get; set; }
    public string? OldCustomerName { get; set; }
    public bool CanSync { get; set; }
    public bool NoOp { get; set; }
    public string? BlockReason { get; set; }
    public List<string> BlockingDocuments { get; set; } = new();
    /// <summary>销售订单头客户（ID/名称）是否需按目标客户刷新（0/1）。</summary>
    public int SellOrderCustomerNameToSync { get; set; }
    public int StockOutNotifiesToSync { get; set; }
    public int PackingsToSync { get; set; }
    public int PackingItemExtendsToSync { get; set; }
    public int StockOutsToSync { get; set; }
    public int ReceivablesToSync { get; set; }
    public List<SalesOrderCustomerDownstreamSyncPreviewItem> SyncItems { get; set; } = new();
}

public class SalesOrderCustomerDownstreamSyncPreviewItem
{
    /// <summary>sellOrder | stockOutNotify | packing | packingItemExtend | stockOut | receivable</summary>
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
