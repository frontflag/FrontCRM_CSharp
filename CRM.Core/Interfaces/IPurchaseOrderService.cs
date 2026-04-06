using CRM.Core.Models.Purchase;

namespace CRM.Core.Interfaces
{
    /// <summary>采购订单服务接口</summary>
    public interface IPurchaseOrderService
    {
        /// <param name="actingUserId">当前登录用户 ID（写入 create_by_user_id）</param>
        Task<PurchaseOrder> CreateAsync(CreatePurchaseOrderRequest request, string? actingUserId = null);
        Task<PurchaseOrder?> GetByIdAsync(string id);
        Task<IEnumerable<PurchaseOrder>> GetAllAsync();
        /// <param name="actingUserId">当前登录用户 ID（写入 modify_by_user_id）</param>
        Task<PurchaseOrder> UpdateAsync(string id, UpdatePurchaseOrderRequest request, string? actingUserId = null);
        Task DeleteAsync(string id);
        /// <param name="actingUserId">当前登录用户 ID（写入 modify_by_user_id）</param>
        Task UpdateStatusAsync(string id, short status, string? actingUserId = null);
        Task<PagedResult<PurchaseOrder>> GetPagedAsync(PurchaseOrderQueryRequest request);
        /// <summary>根据销售订单号获取关联的采购订单列表</summary>
        Task<IEnumerable<PurchaseOrder>> GetBySellOrderCodeAsync(string sellOrderCode);
        /// <summary>根据销售订单明细ID列表获取采购订单明细列表</summary>
        Task<IEnumerable<PurchaseOrderItem>> GetItemsBySellOrderItemIdsAsync(List<string> sellOrderItemIds);
        /// <summary>自动生成采购订单(以销定采)</summary>
        /// <param name="actingUserId">当前登录用户 ID（写入各新生成单的 create_by_user_id）</param>
        Task<IEnumerable<PurchaseOrder>> AutoGenerateFromSellOrderAsync(string sellOrderId, string? actingUserId = null);
    }

    public class CreatePurchaseOrderRequest
    {
        /// <summary>采购单号</summary>
        public string PurchaseOrderCode { get; set; } = string.Empty;
        /// <summary>供应商ID</summary>
        public string VendorId { get; set; } = string.Empty;
        /// <summary>供应商名称</summary>
        public string? VendorName { get; set; }
        /// <summary>供应商编号</summary>
        public string? VendorCode { get; set; }
        /// <summary>供应商联系人ID</summary>
        public string? VendorContactId { get; set; }
        /// <summary>采购员ID</summary>
        public string? PurchaseUserId { get; set; }
        /// <summary>采购员名称</summary>
        public string? PurchaseUserName { get; set; }
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
        /// <summary>内部备注</summary>
        public string? InnerComment { get; set; }
        /// <summary>明细行</summary>
        public List<CreatePurchaseOrderItemRequest> Items { get; set; } = new();
    }

    public class CreatePurchaseOrderItemRequest
    {
        /// <summary>销售订单明细ID(以销定采核心字段)</summary>
        public string SellOrderItemId { get; set; } = string.Empty;
        /// <summary>供应商ID</summary>
        public string VendorId { get; set; } = string.Empty;
        /// <summary>商品/物料ID</summary>
        public string? ProductId { get; set; }
        /// <summary>物料型号(PN)</summary>
        public string? PN { get; set; }
        /// <summary>品牌</summary>
        public string? Brand { get; set; }
        /// <summary>采购数量</summary>
        public decimal Qty { get; set; }
        /// <summary>采购单价(成本)</summary>
        public decimal Cost { get; set; }
        /// <summary>币别</summary>
        public short Currency { get; set; } = 1;
        /// <summary>交货日期</summary>
        public DateTime? DeliveryDate { get; set; }
        /// <summary>备注</summary>
        public string? Comment { get; set; }

        /// <summary>内部备注</summary>
        public string? InnerComment { get; set; }
    }

    public class UpdatePurchaseOrderRequest
    {
        public string? VendorName { get; set; }
        public string? PurchaseUserId { get; set; }
        public string? PurchaseUserName { get; set; }
        public short? Type { get; set; }
        public short? Currency { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? Comment { get; set; }
        public string? InnerComment { get; set; }
        public List<CreatePurchaseOrderItemRequest>? Items { get; set; }
    }

    public class PurchaseOrderQueryRequest
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
