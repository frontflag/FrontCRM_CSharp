using System.Text.Json.Serialization;
using CRM.Core.Constants;
using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 出库服务接口
    /// </summary>
    public interface IStockOutService
    {
        Task<StockOutRequest> CreateStockOutRequestAsync(CreateStockOutRequestRequest request, string? actingUserId = null);
        /// <summary>销售订单明细申请出库前的数量上下文（前端只读展示）</summary>
        Task<StockOutApplyContextDto> GetApplyContextAsync(string salesOrderId, string salesOrderItemId);
        Task<IEnumerable<StockOutRequestListItemDto>> GetStockOutRequestListAsync();
        /// <summary>
        /// 执行出库（内部包含预占/拣货/出库确认的 FIFO 逻辑）
        /// </summary>
        Task<StockOut> ExecuteStockOutAsync(ExecuteStockOutRequest request, string? actingUserId = null);
        Task<StockOut?> GetByIdAsync(string id);
        /// <summary>出库单详情（列表展示字段 + 仓库/明细主键，供前端详情页）</summary>
        Task<StockOutDetailViewDto?> GetDetailViewAsync(string id);
        /// <summary>出库单列表（含客户、业务员、销售明细编号等展示字段）</summary>
        Task<IEnumerable<StockOutListItemDto>> GetStockOutListAsync();

        /// <summary>出库明细（<c>stockoutitem</c>）全量列表，头表字段与 <see cref="GetStockOutListAsync"/> 展示口径一致。</summary>
        Task<IEnumerable<StockOutItemListRowDto>> GetStockOutItemListAsync(StockOutItemListQuery? query);
        Task UpdateStatusAsync(string id, short status, string? actingUserId = null);
        /// <summary>可改：出库日期、出货方式、快递单号</summary>
        Task UpdateHeaderAsync(string id, UpdateStockOutHeaderRequest request, string? actingUserId = null);
    }

    public class CreateStockOutRequestRequest
    {
        public string RequestCode { get; set; } = string.Empty;
        public string SalesOrderId { get; set; } = string.Empty;
        /// <summary>销售订单明细主键（sellorderitem）</summary>
        public string SalesOrderItemId { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string RequestUserId { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string? Remark { get; set; }
        /// <summary>出货方式（字典 LogisticsArrivalMethod ItemCode）</summary>
        public string? ShipmentMethod { get; set; }

        /// <summary>地域类型 RegionType：10=境内 20=境外（与仓库、到货通知共用）</summary>
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public short RegionType { get; set; } = RegionTypeCode.Domestic;
    }

    /// <summary>销售明细「申请出库」弹窗用：数量口径由服务端计算，前端仅展示。</summary>
    public class StockOutApplyContextDto
    {
        public string salesOrderItemId { get; set; } = string.Empty;
        public decimal salesOrderQty { get; set; }
        /// <summary>已占用：出库通知单数量合计（状态≠2 已取消）</summary>
        public decimal alreadyNotifiedQty { get; set; }
        /// <summary>按销售行尚可申请的通知数量上限</summary>
        public decimal remainingNotifyQty { get; set; }
        /// <summary>当前可用库存（与库存中心口径一致）</summary>
        public decimal availableStockQty { get; set; }
        /// <summary>建议本次填写上限 = min(remainingNotifyQty, availableStockQty)</summary>
        public decimal suggestedMaxQty { get; set; }
    }

    public class StockOutListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string StockOutCode { get; set; } = string.Empty;
        public short StockOutType { get; set; }
        public string? SourceCode { get; set; }
        public string? SourceId { get; set; }
        public DateTime StockOutDate { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
        public short Status { get; set; }
        public string? Remark { get; set; }
        public DateTime CreateTime { get; set; }
        public string? CreateByUserId { get; set; }
        public string? CreateUserName { get; set; }
        public string? CustomerName { get; set; }
        public string? SalesUserName { get; set; }
        public string? SellOrderItemCode { get; set; }
        /// <summary>出货方式（字典 ItemCode）</summary>
        public string? ShipmentMethod { get; set; }
        public string? CourierTrackingNo { get; set; }
    }

    public class StockOutDetailViewDto : StockOutListItemDto
    {
        public string? WarehouseId { get; set; }
        /// <summary>仓库编号（由 WarehouseId 解析；无档案时为空）</summary>
        public string? WarehouseCode { get; set; }
        public string? SellOrderItemId { get; set; }
    }

    public class UpdateStockOutHeaderRequest
    {
        public DateTime StockOutDate { get; set; }
        public string? ShipmentMethod { get; set; }
        public string? CourierTrackingNo { get; set; }
    }

    public class StockOutRequestListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string RequestCode { get; set; } = string.Empty;
        public string SalesOrderId { get; set; } = string.Empty;
        public string SalesOrderItemId { get; set; } = string.Empty;
        public string? SalesOrderCode { get; set; }
        public string? MaterialModel { get; set; }
        public string? Brand { get; set; }
        /// <summary>出库通知数量（单表 Quantity）</summary>
        public int OutQuantity { get; set; }
        public DateTime? ExpectedStockOutDate { get; set; }
        public string? SalesUserName { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string RequestUserId { get; set; } = string.Empty;
        public string? RequestUserName { get; set; }
        public DateTime RequestDate { get; set; }
        public short Status { get; set; }
        public string? Remark { get; set; }
        /// <summary>出货方式（字典 LogisticsArrivalMethod ItemCode）</summary>
        public string? ShipmentMethod { get; set; }

        /// <summary>地域类型：10=境内 20=境外</summary>
        public short RegionType { get; set; }

        public DateTime CreateTime { get; set; }
    }

    public class ExecuteStockOutRequest
    {
        public string StockOutRequestId { get; set; } = string.Empty;
        public string StockOutCode { get; set; } = string.Empty;
        public string WarehouseId { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
        public DateTime StockOutDate { get; set; }
        public string? Remark { get; set; }
        public List<ExecuteStockOutItemRequest> Items { get; set; } = new();
    }

    public class ExecuteStockOutItemRequest
    {
        public int LineNo { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? BatchNo { get; set; }
        public string? WarehouseLocation { get; set; }
    }

    /// <summary>出库明细列表查询（空则不作为条件）。</summary>
    public class StockOutItemListQuery
    {
        /// <summary>出库单头状态；不传则不过滤</summary>
        public short? Status { get; set; }

        public string? StockOutCode { get; set; }
        public DateTime? StockOutDateFrom { get; set; }
        public DateTime? StockOutDateTo { get; set; }
        public string? CustomerName { get; set; }
        public string? SalesUserName { get; set; }
        public string? PurchasePn { get; set; }
        public string? SellOrderItemCode { get; set; }
    }

    /// <summary><c>stockoutitem</c> 行 + 头表展示字段。</summary>
    public class StockOutItemListRowDto
    {
        public string StockOutItemId { get; set; } = string.Empty;
        public string StockOutId { get; set; } = string.Empty;
        public short Status { get; set; }
        public string StockOutCode { get; set; } = string.Empty;
        public DateTime StockOutDate { get; set; }
        public string? CustomerName { get; set; }
        public string? SalesUserName { get; set; }
        public string? PurchasePn { get; set; }
        public string? PurchaseBrand { get; set; }
        /// <summary>出库数量：优先 <c>ActualQty</c>，否则 <c>Quantity</c></summary>
        public int OutQuantity { get; set; }
        public string? ShipmentMethod { get; set; }
        public string? CourierTrackingNo { get; set; }
        public string? SellOrderItemCode { get; set; }
    }
}
