using CRM.Core.Models.Sales;

namespace CRM.Core.Interfaces
{
    /// <summary>销售订单服务接口</summary>
    public interface ISalesOrderService
    {
        Task<SellOrder> CreateAsync(CreateSalesOrderRequest request);
        Task<SellOrder?> GetByIdAsync(string id);
        Task<IEnumerable<SellOrder>> GetAllAsync();
        Task<SellOrder> UpdateAsync(string id, UpdateSalesOrderRequest request);
        Task DeleteAsync(string id);
        /// <param name="auditRemark">审核拒绝时写入的原因（仅 AuditFailed 时有效）</param>
        Task UpdateStatusAsync(string id, SellOrderMainStatus status, string? auditRemark = null);
        Task<PagedResult<SellOrder>> GetPagedAsync(SalesOrderQueryRequest request);
        /// <summary>根据客户ID获取销售订单列表</summary>
        Task<IEnumerable<SellOrder>> GetByCustomerIdAsync(string customerId);
        /// <summary>获取销售订单关联的采购订单列表</summary>
        Task<IEnumerable<object>> GetRelatedPurchaseOrdersAsync(string sellOrderId);

        /// <summary>分页查询销售订单明细行（含订单头字段），用于明细列表</summary>
        Task<PagedResult<SellOrderItemLineDto>> GetSellOrderItemLinesPagedAsync(SellOrderItemLineQueryRequest request);
    }

    /// <summary>销售订单明细列表查询</summary>
    public class SellOrderItemLineQueryRequest
    {
        /// <summary>订单创建时间起（含）</summary>
        public DateTime? OrderCreateStart { get; set; }
        /// <summary>订单创建时间止（含当日）</summary>
        public DateTime? OrderCreateEnd { get; set; }
        public string? CustomerName { get; set; }
        public string? SalesUserName { get; set; }
        public string? Pn { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? CurrentUserId { get; set; }
    }

    /// <summary>销售订单明细列表行 DTO</summary>
    public class SellOrderItemLineDto
    {
        public string SellOrderItemId { get; set; } = string.Empty;
        public string SellOrderId { get; set; } = string.Empty;
        public string SellOrderCode { get; set; } = string.Empty;
        public short OrderStatus { get; set; }
        public DateTime OrderCreateTime { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? SalesUserName { get; set; }
        public string? PN { get; set; }
        public string? Brand { get; set; }
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public decimal LineTotal { get; set; }
        public short Currency { get; set; }
        /// <summary>折算美金单价：仅当明细币别为 USD 时有值</summary>
        public decimal? UsdUnitPrice { get; set; }
        /// <summary>折算美金总额：仅当明细币别为 USD 时有值；其他币别暂无汇率时为空</summary>
        public decimal? UsdLineTotal { get; set; }
        public short ItemStatus { get; set; }
    }

    public class CreateSalesOrderRequest
    {
        /// <summary>销售单号</summary>
        public string SellOrderCode { get; set; } = string.Empty;
        /// <summary>客户ID</summary>
        public string CustomerId { get; set; } = string.Empty;
        /// <summary>客户名称</summary>
        public string? CustomerName { get; set; }
        /// <summary>业务员ID</summary>
        public string? SalesUserId { get; set; }
        /// <summary>业务员名称</summary>
        public string? SalesUserName { get; set; }
        /// <summary>订单类型 1=普通 2=紧急 3=样品</summary>
        public short Type { get; set; } = 1;
        /// <summary>币别 1=RMB 2=USD 3=EUR</summary>
        public short Currency { get; set; } = 1;
        /// <summary>交货日期</summary>
        public DateTime? DeliveryDate { get; set; }
        /// <summary>送货地址</summary>
        public string? DeliveryAddress { get; set; }
        /// <summary>备注</summary>
        public string? Comment { get; set; }
        /// <summary>明细行</summary>
        public List<CreateSalesOrderItemRequest> Items { get; set; } = new();
    }

    public class CreateSalesOrderItemRequest
    {
        /// <summary>报价ID(来源)</summary>
        public string? QuoteId { get; set; }
        /// <summary>商品/物料ID</summary>
        public string? ProductId { get; set; }
        /// <summary>物料型号(PN)</summary>
        public string? PN { get; set; }
        /// <summary>品牌</summary>
        public string? Brand { get; set; }
        /// <summary>客户料号</summary>
        public string? CustomerPnNo { get; set; }
        /// <summary>销售数量</summary>
        public decimal Qty { get; set; }
        /// <summary>销售单价</summary>
        public decimal Price { get; set; }
        /// <summary>币别</summary>
        public short Currency { get; set; } = 1;
        /// <summary>生产日期要求</summary>
        public string? DateCode { get; set; }
        /// <summary>交货日期</summary>
        public DateTime? DeliveryDate { get; set; }
        /// <summary>备注</summary>
        public string? Comment { get; set; }
    }

    public class UpdateSalesOrderRequest
    {
        public string? CustomerName { get; set; }
        /// <summary>业务员用户 ID</summary>
        public string? SalesUserId { get; set; }
        public string? SalesUserName { get; set; }
        public short? Type { get; set; }
        public short? Currency { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? Comment { get; set; }
        public List<CreateSalesOrderItemRequest>? Items { get; set; }
    }

    public class SalesOrderQueryRequest
    {
        public string? Keyword { get; set; }
        public short? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? CurrentUserId { get; set; }
    }
}
