namespace CRM.Core.Interfaces;

/// <summary>采购订单明细行列表：数据库分页（与 <c>GET /api/v1/purchase-orders/items</c> 对应）。</summary>
public interface IPurchaseOrderItemListQuery
{
    Task<PagedResult<PurchaseOrderItemListLineRaw>> GetPagedAsync(
        PurchaseOrderItemListQueryRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>采购订单明细列表查询条件。</summary>
public sealed class PurchaseOrderItemListQueryRequest
{
    public string? CurrentUserId { get; set; }

    /// <summary>主单创建时间起（含）。</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>主单创建时间止（与主单列表一致：含当日时按 +1 天边界在查询层处理）。</summary>
    public DateTime? EndDate { get; set; }

    public string? PurchaseOrderCode { get; set; }
    public string? VendorName { get; set; }
    public string? PurchaseUserName { get; set; }
    public string? Pn { get; set; }
    public short? OrderType { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>明细列表一行：库内原始字段，供 API 层做权限掩码与创建人补全。</summary>
public sealed class PurchaseOrderItemListLineRaw
{
    public string PurchaseOrderItemId { get; set; } = string.Empty;
    public string PurchaseOrderId { get; set; } = string.Empty;
    public string PurchaseOrderItemCode { get; set; } = string.Empty;
    public string PurchaseOrderCode { get; set; } = string.Empty;
    public short PurchaseOrderType { get; set; }
    public short OrderStatus { get; set; }
    public DateTime? OrderCreateTime { get; set; }
    public string? PurchaseUserName { get; set; }
    public string? CreateByUserId { get; set; }
    public string VendorId { get; set; } = string.Empty;
    public string? VendorName { get; set; }
    public string? Pn { get; set; }
    public string? Brand { get; set; }
    public short ItemStatus { get; set; }
    public short FinancePaymentStatus { get; set; }
    public short PurchaseProgressStatus { get; set; }
    public short StockInProgressStatus { get; set; }
    public short PaymentProgressStatus { get; set; }
    public short InvoiceProgressStatus { get; set; }
    public decimal PaymentAmountRequested { get; set; }
    public decimal Qty { get; set; }
    public decimal Cost { get; set; }
    public short Currency { get; set; }
    public DateTime? DeliveryDate { get; set; }
}
