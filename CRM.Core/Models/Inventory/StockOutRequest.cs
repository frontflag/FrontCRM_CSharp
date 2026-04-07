using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CRM.Core.Constants;

namespace CRM.Core.Models.Inventory
{
    /// <summary>
    /// 出库申请单（单表：一条通知对应一条销售订单明细）
    /// </summary>
    [Table("stockoutrequest")]
    public class StockOutRequest : BaseGuidEntity
    {
        /// <summary>
        /// 申请单号
        /// </summary>
        [StringLength(50)]
        public string RequestCode { get; set; } = string.Empty;

        /// <summary>
        /// 销售订单ID
        /// </summary>
        [StringLength(36)]
        public string SalesOrderId { get; set; } = string.Empty;

        /// <summary>
        /// 销售订单明细ID（sellorderitem.SellOrderItemId）
        /// </summary>
        [StringLength(36)]
        public string SalesOrderItemId { get; set; } = string.Empty;

        /// <summary>
        /// 物料型号（PN）
        /// </summary>
        [StringLength(200)]
        public string MaterialCode { get; set; } = string.Empty;

        /// <summary>
        /// 品牌 / 物料名称
        /// </summary>
        [StringLength(200)]
        public string? MaterialName { get; set; }

        /// <summary>
        /// 出库通知数量
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// 客户ID
        /// </summary>
        [StringLength(36)]
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>
        /// 申请人ID
        /// </summary>
        [StringLength(36)]
        public string RequestUserId { get; set; } = string.Empty;

        /// <summary>
        /// 申请日期
        /// </summary>
        public DateTime RequestDate { get; set; }

        /// <summary>
        /// 状态 (0:待出库 1:已出库 2:已取消)
        /// </summary>
        public short Status { get; set; } = 0;

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        /// <summary>
        /// 出货方式（数据字典 LogisticsArrivalMethod 的 ItemCode，与物流「来货方式」同源）
        /// </summary>
        [StringLength(64)]
        public string? ShipmentMethod { get; set; }

        /// <summary>地域类型 RegionType：10=境内 20=境外（与仓库、到货通知共用枚举）</summary>
        [Column("RegionType")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public short RegionType { get; set; } = RegionTypeCode.Domestic;

        [StringLength(36)]
        [Column("create_by_user_id")]
        public string? CreateByUserId { get; set; }

        [StringLength(36)]
        [Column("modify_by_user_id")]
        public string? ModifyByUserId { get; set; }
    }
}
