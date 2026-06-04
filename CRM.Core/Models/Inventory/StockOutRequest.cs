using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CRM.Core.Constants;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.Inventory
{
    /// <summary>
    /// 出库通知（表 <c>stockout_notify</c>：单表模型，一条记录 = 一条销售订单明细上的一次申请出库）。
    /// </summary>
    [Table("stockout_notify")]
    public class StockOutRequest : BaseGuidEntity, ISoftDeletable
    {
        /// <summary>主键（列 <c>ID</c>）；装箱明细、报关单等外键指向本列。</summary>
        [Key]
        [StringLength(36)]
        [Column("ID")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>出库通知业务单号（列 <c>Code</c>，模块 STOR 流水，如 STOR00001）。</summary>
        [StringLength(50)]
        [Column("Code")]
        public string RequestCode { get; set; } = string.Empty;

        /// <summary>所属销售订单主键（<c>sellorder.SellOrderId</c>）。</summary>
        [StringLength(36)]
        public string SalesOrderId { get; set; } = string.Empty;

        /// <summary>所属销售订单明细主键（<c>sellorderitem.SellOrderItemId</c>）；本表一行仅绑定一条销售明细。</summary>
        [StringLength(36)]
        public string SalesOrderItemId { get; set; } = string.Empty;

        /// <summary>申请出库物料型号（PN）快照；创建时默认取自销售明细，不随后续改单自动刷新。</summary>
        [StringLength(200)]
        public string MaterialCode { get; set; } = string.Empty;

        /// <summary>申请出库品牌快照；创建时默认取自销售明细 <c>brand</c>。</summary>
        [StringLength(200)]
        public string? MaterialName { get; set; }

        /// <summary>本通知申请出库数量；与同销售明细其它未取消通知合计不得超过可出库余量。</summary>
        public int Quantity { get; set; }

        /// <summary>发货客户主键（<c>customerinfo</c>）；创建时由请求或销售订单解析。</summary>
        [StringLength(36)]
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>申请人用户主键（GUID，与 JWT 登录用户一致）。</summary>
        [StringLength(36)]
        public string RequestUserId { get; set; } = string.Empty;

        /// <summary>申请/计划出库日期（UTC）。</summary>
        public DateTime RequestDate { get; set; }

        /// <summary>
        /// 出库通知状态，见 <see cref="StockOutRequestStatusCode"/>：
        /// 5=待报关，10=待装箱，20=已装箱，100=已出库，-1=已取消。
        /// </summary>
        public short Status { get; set; } = StockOutRequestStatusCode.PendingPacking;

        /// <summary>
        /// 报关状态，见 <see cref="StockOutNotifyCustomsStatusCode"/>：
        /// 0=未知，10=无需报关，20=待报关，30=报关中，100=报关完成。
        /// </summary>
        public short CustomsStatus { get; set; } = StockOutNotifyCustomsStatusCode.Unknown;

        /// <summary>仅 StockOutType=20（报关出库通知）使用，指向 <c>customs_pendlist</c>。</summary>
        [StringLength(36)]
        [Column("customs_pendlist_id")]
        public string? CustomsPendlistId { get; set; }

        /// <summary>业务备注（自由文本）。</summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        /// <summary>
        /// 出货方式：数据字典 <c>LogisticsArrivalMethod</c> 的 ItemCode（与物流「来货方式」同源，存编码）。
        /// </summary>
        [StringLength(64)]
        public string? ShipmentMethod { get; set; }

        /// <summary>快递公司：数据字典 <c>LogisticsExpressMethod</c> 的 ItemCode。</summary>
        [StringLength(64)]
        [Column("ExpressCompany")]
        public string? ExpressCompany { get; set; }

        /// <summary>地域类型，见 <see cref="RegionTypeCode"/>：10=境内，20=境外。</summary>
        [Column("RegionType")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public short RegionType { get; set; } = RegionTypeCode.Domestic;

        /// <summary>出库业务类型，见 <see cref="StockOutTypeCode"/>；经 <see cref="StockOutTypeCode.NormalizeForNotify"/> 归一，默认销售出库。</summary>
        [Column("StockOutType")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public short StockOutType { get; set; } = StockOutTypeCode.Sales;

        /// <summary>创建本通知时的登录用户 GUID（JWT 用户主键）。</summary>
        [StringLength(36)]
        [Column("create_by_user_id")]
        public string? CreateByUserId { get; set; }

        /// <summary>最后修改本通知时的登录用户 GUID。</summary>
        [StringLength(36)]
        [Column("modify_by_user_id")]
        public string? ModifyByUserId { get; set; }

        /// <summary>软删除标记；为 true 时全局查询过滤器排除。</summary>
        [Column("is_deleted")]
        public bool IsDeleted { get; set; }
    }
}
