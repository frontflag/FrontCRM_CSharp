namespace CRM.Core.Interfaces;

/// <summary>采购订单明细行列表：数据库分页（与 <c>GET /api/v1/purchase-orders/items</c> 对应）。</summary>
public interface IPurchaseOrderItemListQuery
{
    Task<PagedResult<PurchaseOrderItemListLineRaw>> GetPagedAsync(
        PurchaseOrderItemListQueryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>按明细主键批量加载列表行（与 <see cref="GetPagedAsync"/> 投影一致）。</summary>
    Task<List<PurchaseOrderItemListLineRaw>> GetByIdsAsync(
        IReadOnlyList<string> purchaseOrderItemIds,
        string? currentUserId = null,
        bool applyDataScope = true,
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
    public string? FreightForwarderOrderNo { get; set; }
    public string? VendorName { get; set; }
    public string? PurchaseUserName { get; set; }
    public string? Pn { get; set; }
    public short? OrderType { get; set; }
    /// <summary>交易币别筛选：rmb=人民币，foreign=外币（非人民币）。</summary>
    public string? TransactionCurrency { get; set; }

    /// <summary>付款进度 0/1/2（扩展表）。</summary>
    public short? PaymentProgressStatus { get; set; }

    /// <summary>采购进度 0/1/2（扩展表）。</summary>
    public short? PurchaseProgressStatus { get; set; }

    /// <summary>入库进度 0/1/2（扩展表）。</summary>
    public short? StockInProgressStatus { get; set; }

    /// <summary>开票进度 0/1/2（扩展表）。</summary>
    public short? InvoiceProgressStatus { get; set; }

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
    public string? FreightForwarderOrderNo { get; set; }
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
    public decimal PaymentAmount { get; set; }
    public decimal PaymentAmountRequested { get; set; }
    public decimal Qty { get; set; }
    public decimal Cost { get; set; }
    public short Currency { get; set; }
    public DateTime? DeliveryDate { get; set; }
}

/// <summary>采购订单明细列表行（与 <c>GET /api/v1/purchase-orders/items</c> 响应字段一致，供嵌入场景复用）。</summary>
public sealed class PurchaseOrderItemListLineDto
{
    public string PurchaseOrderItemId { get; set; } = string.Empty;
    public string PurchaseOrderId { get; set; } = string.Empty;
    public string PurchaseOrderItemCode { get; set; } = string.Empty;
    public string PurchaseOrderCode { get; set; } = string.Empty;
    public string? FreightForwarderOrderNo { get; set; }
    public short PurchaseOrderType { get; set; }
    public string VendorId { get; set; } = string.Empty;
    public string? VendorName { get; set; }
    public string? VendorEnglishName { get; set; }
    public short ItemStatus { get; set; }
    public short PurchaseProgressStatus { get; set; }
    public short StockInProgressStatus { get; set; }
    public short PaymentRequestProgressStatus { get; set; }
    public short PaymentProgressStatus { get; set; }
    public short InvoiceProgressStatus { get; set; }
    public DateTime? OrderCreateTime { get; set; }
    public string? PurchaseUserName { get; set; }
    public string? CreateUserName { get; set; }
    public string? Pn { get; set; }
    public string? Brand { get; set; }
    public decimal Qty { get; set; }
    public decimal Cost { get; set; }
    public decimal LineTotal { get; set; }
    public short Currency { get; set; }
}
