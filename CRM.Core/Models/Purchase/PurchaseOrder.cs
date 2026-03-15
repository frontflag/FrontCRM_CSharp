using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Purchase
{
    /// <summary>
    /// 采购订单主表
    /// </summary>
    [Table("purchaseorder")]
    public class PurchaseOrder : BaseGuidEntity
    {
        /// <summary>
        /// 采购订单ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("PurchaseOrderId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 采购订单编号
        /// </summary>
        [Required]
        [StringLength(32)]
        public string PurchaseOrderCode { get; set; } = string.Empty;

        /// <summary>
        /// 供应商ID (外键)
        /// </summary>
        [Required]
        [StringLength(36)]
        public string VendorId { get; set; } = string.Empty;

        /// <summary>
        /// 供应商联系人ID
        /// </summary>
        [StringLength(36)]
        public string? VendorContactId { get; set; }

        /// <summary>
        /// 采购员ID
        /// </summary>
        [StringLength(36)]
        public string? PurchaseUserId { get; set; }

        /// <summary>
        /// 采购组ID
        /// </summary>
        [StringLength(36)]
        public string? PurchaseGroupId { get; set; }

        /// <summary>
        /// 销售组ID
        /// </summary>
        [StringLength(36)]
        public string? SalesGroupId { get; set; }

        /// <summary>
        /// 订单状态
        /// </summary>
        public short Status { get; set; } = 0;

        /// <summary>
        /// 订单类型
        /// </summary>
        public short? Type { get; set; }

        /// <summary>
        /// 币别
        /// </summary>
        public short? Currency { get; set; }

        /// <summary>
        /// 订单总金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal Total { get; set; } = 0.00m;

        /// <summary>
        /// 折算金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal ConvertTotal { get; set; } = 0.00m;

        /// <summary>
        /// 行项目数
        /// </summary>
        public int ItemRows { get; set; } = 0;

        /// <summary>
        /// 库存状态
        /// </summary>
        public short StockStatus { get; set; } = 0;

        /// <summary>
        /// 财务状态
        /// </summary>
        public short FinanceStatus { get; set; } = 0;

        /// <summary>
        /// 出库状态
        /// </summary>
        public short StockOutStatus { get; set; } = 0;

        /// <summary>
        /// 开票状态
        /// </summary>
        public short InvoiceStatus { get; set; } = 0;

        /// <summary>
        /// 送货地址
        /// </summary>
        [StringLength(200)]
        public string? DeliveryAddress { get; set; }

        /// <summary>
        /// 交货日期
        /// </summary>
        public DateTime? DeliveryDate { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        // 导航属性
        public virtual ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    }

    /// <summary>
    /// 采购订单明细表
    /// </summary>
    [Table("purchaseorderitem")]
    public class PurchaseOrderItem : BaseGuidEntity
    {
        /// <summary>
        /// 明细ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("ItemId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 采购订单ID (外键)
        /// </summary>
        [Required]
        [StringLength(36)]
        public string PurchaseOrderId { get; set; } = string.Empty;

        /// <summary>
        /// 物料ID
        /// </summary>
        [StringLength(36)]
        public string? MaterialId { get; set; }

        /// <summary>
        /// 物料编码
        /// </summary>
        [StringLength(50)]
        public string? MaterialCode { get; set; }

        /// <summary>
        /// 物料名称
        /// </summary>
        [StringLength(200)]
        public string? MaterialName { get; set; }

        /// <summary>
        /// 规格型号
        /// </summary>
        [StringLength(100)]
        public string? MaterialModel { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal Quantity { get; set; } = 0.0000m;

        /// <summary>
        /// 单位
        /// </summary>
        [StringLength(20)]
        public string? Unit { get; set; }

        /// <summary>
        /// 单价
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal Price { get; set; } = 0.0000m;

        /// <summary>
        /// 金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal Amount { get; set; } = 0.00m;

        /// <summary>
        /// 税率
        /// </summary>
        [Column(TypeName = "numeric(5,2)")]
        public decimal TaxRate { get; set; } = 0.00m;

        /// <summary>
        /// 税额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal TaxAmount { get; set; } = 0.00m;

        /// <summary>
        /// 含税金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal TotalAmount { get; set; } = 0.00m;

        /// <summary>
        /// 交货日期
        /// </summary>
        public DateTime? DeliveryDate { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        // 导航属性
        [ForeignKey("PurchaseOrderId")]
        public virtual PurchaseOrder? PurchaseOrder { get; set; }
    }
}
