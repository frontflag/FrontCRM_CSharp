namespace CRM.Core.Models.Purchase;

/// <summary>采购申请列表分页查询参数（与 <c>GET /api/v1/purchase-requisitions</c> Query 对齐）。</summary>
public sealed class PurchaseRequisitionListQueryRequest
{
    public string? Keyword { get; set; }
    public string? SellOrderId { get; set; }
    public short? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>采购申请列表一行（与列表 API 的 <c>items</c> 项字段一致，JSON 驼峰由序列化器处理）。</summary>
public sealed class PurchaseRequisitionListPageRow
{
    public string Id { get; set; } = string.Empty;
    public string BillCode { get; set; } = string.Empty;
    public string SellOrderId { get; set; } = string.Empty;
    public string SellOrderItemId { get; set; } = string.Empty;
    public string? SellOrderCode { get; set; }
    public string? PN { get; set; }
    public string? Brand { get; set; }
    public decimal Qty { get; set; }
    public DateTime ExpectedPurchaseTime { get; set; }
    public short Status { get; set; }
    public short Type { get; set; }
    public string? PurchaseUserId { get; set; }
    public string? PurchaseUserAccount { get; set; }
    public string? QuoteVendorId { get; set; }
    public decimal QuoteCost { get; set; }
    public string? Remark { get; set; }
    public DateTime CreateTime { get; set; }
    public string? CreateUserAccount { get; set; }
}
