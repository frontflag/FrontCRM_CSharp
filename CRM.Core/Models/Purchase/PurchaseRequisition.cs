using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Models.Sales;

namespace CRM.Core.Models.Purchase
{
    /// <summary>
    /// 采购申请主表（PurchaseRequisition）
    /// PRD 采购申请：作为销售订单明细（SellOrderItem）到采购订单（PurchaseOrder）的中间单据
    /// </summary>
    [Table("purchaserequisition")]
    public class PurchaseRequisition : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        // 实际数据库主键列名为 PurchaseRequisitionId（大小写敏感）
        [Column("PurchaseRequisitionId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>采购申请号（系统生成唯一编码）</summary>
        [Required]
        [StringLength(32)]
        [Column("bill_code")]
        public string BillCode { get; set; } = string.Empty;

        /// <summary>销售订单明细Id（关联销售订单明细）</summary>
        [Required]
        [StringLength(36)]
        [Column("sell_order_item_id")]
        public string SellOrderItemId { get; set; } = string.Empty;

        /// <summary>销售订单Id</summary>
        [Required]
        [StringLength(36)]
        [Column("sell_order_id")]
        public string SellOrderId { get; set; } = string.Empty;

        /// <summary>采购数量</summary>
        [Column("qty", TypeName = "numeric(18,4)")]
        public decimal Qty { get; set; } = 0m;

        /// <summary>预计采购时间</summary>
        [Column("expected_purchase_time")]
        public DateTime ExpectedPurchaseTime { get; set; }

        /// <summary>状态：0=新建 1=部分完成 2=全部完成 3=已取消</summary>
        [Column("status")]
        public short Status { get; set; } = 0;

        /// <summary>类型：0=专属 1=公开备货</summary>
        [Column("type")]
        public short Type { get; set; } = 0;

        /// <summary>采购员Id</summary>
        [StringLength(36)]
        [Column("purchase_user_id")]
        public string? PurchaseUserId { get; set; }

        /// <summary>业务员组Id / 销售业务员Id（冗余）</summary>
        [StringLength(36)]
        [Column("sales_user_id")]
        public string? SalesUserId { get; set; }

        /// <summary>报价供应商Id（冗余，后续用于报价/供应商链路）</summary>
        [StringLength(36)]
        [Column("quote_vendor_id")]
        public string? QuoteVendorId { get; set; }

        /// <summary>报价单价（冗余）</summary>
        [Column("quote_cost", TypeName = "numeric(18,6)")]
        public decimal QuoteCost { get; set; } = 0m;

        /// <summary>物料型号（PN，冗余）</summary>
        [StringLength(200)]
        [Column("pn")]
        public string? PN { get; set; }

        /// <summary>品牌（冗余）</summary>
        [StringLength(200)]
        [Column("brand")]
        public string? Brand { get; set; }

        /// <summary>备注</summary>
        [StringLength(500)]
        [Column("remark")]
        public string? Remark { get; set; }

        [StringLength(36)]
        [Column("create_by_user_id")]
        public string? CreateByUserId { get; set; }

        [StringLength(36)]
        [Column("modify_by_user_id")]
        public string? ModifyByUserId { get; set; }
    }
}
