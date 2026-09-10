namespace CRM.Core.Interfaces;

/// <summary>装箱单列表筛选条件。</summary>
public class PackingListQueryRequest
{
    public string? PackingCode { get; set; }
    /// <summary>报关单号（模糊匹配；头表 packing_id 或装箱单 customs_declaration_id）。</summary>
    public string? DeclarationCode { get; set; }
    public short? Status { get; set; }
    public short? StockOutType { get; set; }
    public short? MaterialType { get; set; }
    public string? CustomerName { get; set; }
    public string? SalesUserName { get; set; }
    public DateTime? CreateTimeFrom { get; set; }
    public DateTime? CreateTimeTo { get; set; }

    /// <summary>当前登录用户 Id（销售数据范围过滤）。</summary>
    public string? CurrentUserId { get; set; }
}

/// <summary>装箱单明细列表筛选条件。</summary>
public class PackingItemListQueryRequest
{
    public string? Keyword { get; set; }
    public string? PackingCode { get; set; }
    /// <summary>客户名称（装箱单客户 / 销售订单客户名 / 明细扩展客户，模糊匹配）。</summary>
    public string? CustomerName { get; set; }
    /// <summary>客户订单号（模糊匹配装箱明细扩展或销售明细 <c>customer_so</c>）。</summary>
    public string? CustomerSo { get; set; }
    /// <summary>销售订单号（模糊匹配 <c>sell_order.SellOrderCode</c>）。</summary>
    public string? SellOrderCode { get; set; }
    /// <summary>货代单号（经销售明细关联采购订单头）。</summary>
    public string? FreightForwarderOrderNo { get; set; }
    /// <summary>当前登录用户 Id（销售数据范围过滤）。</summary>
    public string? CurrentUserId { get; set; }
}
