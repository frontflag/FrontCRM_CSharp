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
        /// <param name="actingUserId">当前登录用户 ID（写入 log_operation 与明细 deleted_by）</param>
        Task DeleteAsync(string id, string? actingUserId = null);
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
        /// <summary>按采购单批量重算明细扩展并返回变更结果。</summary>
        Task<PurchaseOrderItemExtendRefreshResult> RefreshItemExtendsAsync(string purchaseOrderId, CancellationToken cancellationToken = default);

        /// <summary>采购订单主表字段变更日志（<c>log_change_fldval</c>，BizType=<see cref="Constants.BusinessLogTypes.PurchaseOrder"/>）。</summary>
        Task<IReadOnlyList<PurchaseOrderFieldChangeLogDto>> GetFieldChangeLogsAsync(string purchaseOrderId);

        /// <summary>已软删除的采购订单明细行（<c>is_deleted=true</c>）。</summary>
        Task<IReadOnlyList<PurchaseOrderDeletedItemLogDto>> GetDeletedOrderItemsAsync(string purchaseOrderId);

        /// <summary>物流录入/修改/清空货代单号（审核通过后；系统内唯一）。</summary>
        Task<PurchaseOrder> UpdateFreightForwarderOrderNoAsync(string purchaseOrderId, string? freightForwarderOrderNo, string? actingUserId = null);
    }

    /// <summary>采购订单字段变更日志行。</summary>
    public class PurchaseOrderFieldChangeLogDto
    {
        public string Id { get; set; } = string.Empty;
        public string PurchaseOrderId { get; set; } = string.Empty;
        public string? PurchaseOrderCode { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string? FieldLabel { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? ChangedByUserId { get; set; }
        public string? ChangedByUserName { get; set; }
        public DateTime ChangedAt { get; set; }
    }

    /// <summary>已软删除的采购订单明细。</summary>
    public class PurchaseOrderDeletedItemLogDto
    {
        public string PurchaseOrderItemId { get; set; } = string.Empty;
        public string? PurchaseOrderItemCode { get; set; }
        public string? PN { get; set; }
        public string? Brand { get; set; }
        public decimal Qty { get; set; }
        public decimal Cost { get; set; }
        public short Currency { get; set; }
        public string? Comment { get; set; }
        public DateTime? CreateTime { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByUserId { get; set; }
        public string? DeletedByUserName { get; set; }
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
        /// <summary>采购助理用户 ID</summary>
        public string? Assistor { get; set; }
        /// <summary>订单类型 1=客单采购 2=备货采购 3=样品采购</summary>
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
        /// <summary>编辑保存时传入已有采购明细主键（PurchaseOrderItemId）；新建行省略。</summary>
        public string? PurchaseOrderItemId { get; set; }

        /// <summary>销售订单明细ID(以销定采核心字段)；无销售行时省略或传 null</summary>
        public string? SellOrderItemId { get; set; }
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
        /// <summary>生产日期/DC 要求（字典 ItemCode）</summary>
        public string? DateCode { get; set; }
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
        public string? Assistor { get; set; }
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
        /// <summary>兼容旧版：单关键字匹配采购单号或供应商名称（OR）；若同时传 <see cref="PurchaseOrderCodeFilter"/> / <see cref="VendorNameFilter"/> 则忽略本字段。</summary>
        public string? Keyword { get; set; }

        /// <summary>采购单号包含（与 <see cref="VendorNameFilter"/>、状态等为 AND）。</summary>
        public string? PurchaseOrderCodeFilter { get; set; }

        /// <summary>供应商名称包含。</summary>
        public string? VendorNameFilter { get; set; }

        /// <summary>货代单号包含。</summary>
        public string? FreightForwarderOrderNoFilter { get; set; }

        /// <summary>采购员姓名包含。</summary>
        public string? PurchaseUserNameFilter { get; set; }

        /// <summary>备注（主表 Comment）包含。</summary>
        public string? CommentFilter { get; set; }

        /// <summary>主表订单类型 1/2/3。</summary>
        public short? OrderType { get; set; }

        public short? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? CurrentUserId { get; set; }
    }
}
