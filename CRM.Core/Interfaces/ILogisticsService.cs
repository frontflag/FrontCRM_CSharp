using System.Text.Json.Serialization;
using CRM.Core.Constants;
using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces
{
    public interface ILogisticsService
    {
        Task<IReadOnlyList<StockInNotify>> GetArrivalNoticesAsync();
        Task<StockInNotify> CreateArrivalNoticeAsync(CreateArrivalNoticeRequest request);
        Task<AutoGenerateArrivalNoticeResult> AutoGenerateArrivalNoticesAsync();
        Task UpdateArrivalNoticeStatusAsync(string id, short status);
        Task<StockInNotify> UpdateArrivalNoticeInfoAsync(string id, UpdateArrivalNoticeInfoRequest request);

        /// <summary>到货通知列表右侧「操作」面板：采购行摘要 + 关联质检/入库。</summary>
        Task<ArrivalNoticeOpsAggregates> GetArrivalNoticeOpsAggregatesAsync(
            string noticeId,
            CancellationToken cancellationToken = default);

        /// <summary>质检列表右侧「操作」面板：采购 + 到货通知 + 入库摘要。</summary>
        Task<QcOpsAggregates> GetQcOpsAggregatesAsync(
            string qcId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<QCInfo>> GetQcsAsync(QcQueryRequest? request = null);

        /// <summary>质检列表：数据库分页后再做行展示填充与列表侧自愈（与 <see cref="GetQcsAsync"/> 展示口径一致）。</summary>
        Task<PagedResult<QCInfo>> GetQcsPagedAsync(
            int page,
            int pageSize,
            QcQueryRequest? request = null,
            CancellationToken cancellationToken = default);
        Task<QCInfo> CreateQcAsync(CreateQcRequest request, string? actingUserId = null);
        Task<QCInfo> UpdateQcResultAsync(string id, UpdateQcResultRequest request, string? actingUserId = null);
        Task BindQcStockInAsync(string id, string stockInId, string? actingUserId = null);
        Task HandleStockInCompletedAsync(string stockInId, string? purchaseOrderId);

        Task ForceDeleteArrivalNoticeAsync(string id, string confirmBillCode, string actingUserId, string? actingUserName);
        Task ForceDeleteQcAsync(string id, string confirmBillCode, string actingUserId, string? actingUserName);
    }

    public class CreateArrivalNoticeRequest
    {
        /// <summary>采购明细 Id（单表到货通知必填）</summary>
        public string PurchaseOrderItemId { get; set; } = string.Empty;

        /// <summary>本批次预期到货数量</summary>
        public decimal ExpectQty { get; set; }

        public string PurchaseOrderId { get; set; } = string.Empty;

        /// <summary>预计到货日期（可选，缺省用采购明细/订单交货日）</summary>
        public DateTime? ExpectedArrivalDate { get; set; }

        /// <summary>地域类型 RegionType：10=境内 20=境外（与仓库档案共用）</summary>
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public short RegionType { get; set; } = RegionTypeCode.Domestic;

        /// <summary>入库类型 <see cref="StockInTypeCode"/>；未传或非法时服务端默认为采购入库。</summary>
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public short StockInType { get; set; } = StockInTypeCode.Purchase;

        /// <summary>到货通知备注（可选）。</summary>
        public string? Remark { get; set; }

        /// <summary>预计到货方式（字典 LogisticsArrivalMethod ItemCode，与出库通知出货方式同源）。</summary>
        public string? ShipmentMethod { get; set; }

        /// <summary>预计到货快递单号。</summary>
        public string? CourierTrackingNo { get; set; }

        /// <summary>快递公司（字典 LogisticsExpressMethod ItemCode）。</summary>
        public string? ExpressCompany { get; set; }
    }

    public class UpdateArrivalNoticeInfoRequest
    {
        /// <summary>预计到货方式（字典 LogisticsArrivalMethod ItemCode，可空）。</summary>
        public string? ShipmentMethod { get; set; }

        /// <summary>预计到货快递单号（可空）。</summary>
        public string? CourierTrackingNo { get; set; }

        /// <summary>快递公司（字典 LogisticsExpressMethod ItemCode，可空；非快递时服务端清空）。</summary>
        public string? ExpressCompany { get; set; }
    }

    public class CreateQcRequest
    {
        public string StockInNotifyId { get; set; } = string.Empty;
    }

    public class UpdateQcResultRequest
    {
        /// <summary>pass | partial | reject</summary>
        public string Result { get; set; } = "pass";
        public decimal PassQty { get; set; }
        public decimal RejectQty { get; set; }

        /// <summary>
        /// 为 <c>true</c> 时同步写入 <see cref="StockInPlanDate"/>（含置空）。不传或为 <c>false</c> 则不修改原值（兼容旧客户端）。
        /// </summary>
        public bool? HasStockInPlanDate { get; set; }

        /// <summary>计划入库日期（UTC 或可解析为 UTC 的 ISO 8601）。</summary>
        public DateTime? StockInPlanDate { get; set; }

        /// <summary>为 <c>true</c> 时同步写入 <see cref="Remark"/>（含置空）。</summary>
        public bool? HasRemark { get; set; }

        /// <summary>质检备注。</summary>
        public string? Remark { get; set; }
    }

    public class QcQueryRequest
    {
        /// <summary>按质检主键精确筛选（编辑页等场景）。</summary>
        public string? QcId { get; set; }

        /// <summary>质检单号（模糊匹配）。</summary>
        public string? QcCode { get; set; }

        public string? Model { get; set; }
        public string? VendorName { get; set; }
        public string? PurchaseOrderCode { get; set; }
        public string? FreightForwarderOrderNo { get; set; }
        public string? SalesOrderCode { get; set; }

        /// <summary>按销售订单明细主键精确筛选（销售明细详情面板等）。</summary>
        public List<string>? SellOrderItemIds { get; set; }

        /// <summary>到货类型（<see cref="StockInTypeCode"/>：10 采购 / 20 报关 / 30 退货 / 40 报废）。</summary>
        public short? StockInType { get; set; }

        /// <summary>左栏快捷检索 preset（见 <see cref="CRM.Core.Constants.QcListQuickFilterCodes"/>）。</summary>
        public string? Preset { get; set; }

        /// <summary>当前用户 Id（服务端注入，用于采购数据范围过滤）。</summary>
        public string? CurrentUserId { get; set; }
    }

    public class AutoGenerateArrivalNoticeResult
    {
        public int PurchaseOrdersScanned { get; set; }
        public int CreatedCount { get; set; }
        public int ExistingCount { get; set; }
    }

    public class ArrivalNoticeOpsAggregates
    {
        public ArrivalNoticeOpsPurchaseLine? Purchase { get; set; }
        public ArrivalNoticeOpsQc? Qc { get; set; }
        public ArrivalNoticeOpsStockIn? StockIn { get; set; }
    }

    public class QcOpsAggregates
    {
        public ArrivalNoticeOpsPurchaseLine? Purchase { get; set; }
        public QcOpsArrivalNotice? ArrivalNotice { get; set; }
        public ArrivalNoticeOpsStockIn? StockIn { get; set; }
    }

    public class QcOpsArrivalNotice
    {
        public string Id { get; set; } = string.Empty;
        public string NoticeCode { get; set; } = string.Empty;
        public short StockInType { get; set; }
        public DateTime? ActualArrivalDate { get; set; }
        public DateTime? ExpectedArrivalDate { get; set; }
        public decimal ExpectQty { get; set; }
    }

    public class ArrivalNoticeOpsPurchaseLine
    {
        public string PurchaseOrderItemId { get; set; } = string.Empty;
        public string PurchaseOrderItemCode { get; set; } = string.Empty;
        public string PurchaseOrderId { get; set; } = string.Empty;
        public string? PurchaseUserName { get; set; }
        public DateTime? PurchaseOrderCreateTime { get; set; }
        public decimal Qty { get; set; }
    }

    public class ArrivalNoticeOpsQc
    {
        public string Id { get; set; } = string.Empty;
        public string QcCode { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        public string? CreateUserName { get; set; }
        public decimal PassQty { get; set; }
        public decimal RejectQty { get; set; }
    }

    public class ArrivalNoticeOpsStockIn
    {
        public string Id { get; set; } = string.Empty;
        public string StockInCode { get; set; } = string.Empty;
        public DateTime? StockInDate { get; set; }
        public string? CreateUserName { get; set; }
        public short Status { get; set; }
        public short StockInType { get; set; }
        public string? WarehouseName { get; set; }
        public decimal TotalQuantity { get; set; }
    }
}
