using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.RFQ
{
    /// <summary>
    /// 询价单主表 (Request For Quotation)
    /// </summary>
    [Table("rfq")]
    public class RFQ : BaseGuidEntity
    {
        /// <summary>
        /// 询价单ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("RFQId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 询价单号
        /// </summary>
        [Required]
        [StringLength(32)]
        public string RFQCode { get; set; } = string.Empty;

        /// <summary>
        /// 询价单类型 (1:客户询价 2:内部询价)
        /// </summary>
        public short RFQType { get; set; } = 1;

        /// <summary>
        /// 客户ID (外键)
        /// </summary>
        [StringLength(36)]
        public string? CustomerId { get; set; }

        /// <summary>
        /// 供应商ID (外键)
        /// </summary>
        [StringLength(36)]
        public string? VendorId { get; set; }

        /// <summary>
        /// 联系人ID
        /// </summary>
        [StringLength(36)]
        public string? ContactId { get; set; }

        /// <summary>
        /// 业务员ID
        /// </summary>
        [StringLength(36)]
        public string? SalesUserId { get; set; }

        /// <summary>
        /// 采购员ID
        /// </summary>
        [StringLength(36)]
        public string? PurchaseUserId { get; set; }

        /// <summary>
        /// 询价日期
        /// </summary>
        public DateTime RFQDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 有效期至
        /// </summary>
        public DateTime? ValidityDate { get; set; }

        /// <summary>
        /// 币别
        /// </summary>
        public short? Currency { get; set; }

        /// <summary>
        /// 汇率
        /// </summary>
        [Column(TypeName = "numeric(18,6)")]
        public decimal ExchangeRate { get; set; } = 1.000000m;

        /// <summary>
        /// 询价总金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal TotalAmount { get; set; } = 0.00m;

        /// <summary>
        /// 行项目数
        /// </summary>
        public int ItemRows { get; set; } = 0;

        /// <summary>
        /// 状态 (0:草稿 1:待报价 2:已报价 3:已关闭 4:已取消)
        /// </summary>
        public short Status { get; set; } = 0;

        /// <summary>
        /// 优先级 (1:低 2:中 3:高 4:紧急)
        /// </summary>
        public short Priority { get; set; } = 2;

        /// <summary>
        /// 项目名称
        /// </summary>
        [StringLength(200)]
        public string? ProjectName { get; set; }

        /// <summary>
        /// 项目描述
        /// </summary>
        public string? ProjectDescription { get; set; }

        /// <summary>
        /// 预计订单日期
        /// </summary>
        public DateTime? ExpectedOrderDate { get; set; }

        /// <summary>
        /// 预计交货日期
        /// </summary>
        public DateTime? ExpectedDeliveryDate { get; set; }

        /// <summary>
        /// 交货地点
        /// </summary>
        [StringLength(200)]
        public string? DeliveryLocation { get; set; }

        /// <summary>
        /// 付款条款
        /// </summary>
        [StringLength(500)]
        public string? PaymentTerms { get; set; }

        /// <summary>
        /// 运输方式
        /// </summary>
        public short? ShippingMethod { get; set; }

        /// <summary>
        /// 包装要求
        /// </summary>
        [StringLength(500)]
        public string? PackagingRequirements { get; set; }

        /// <summary>
        /// 质量要求
        /// </summary>
        [StringLength(500)]
        public string? QualityRequirements { get; set; }

        /// <summary>
        /// 认证要求
        /// </summary>
        [StringLength(500)]
        public string? CertificationRequirements { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(1000)]
        public string? Remark { get; set; }

        /// <summary>
        /// 附件数量
        /// </summary>
        public int AttachmentCount { get; set; } = 0;

        // 导航属性
        public virtual ICollection<RFQItem> Items { get; set; } = new List<RFQItem>();
    }

    /// <summary>
    /// 询价单明细表
    /// </summary>
    [Table("rfqitem")]
    public class RFQItem : BaseGuidEntity
    {
        /// <summary>
        /// 明细ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("ItemId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 询价单ID (外键)
        /// </summary>
        [Required]
        [StringLength(36)]
        public string RFQId { get; set; } = string.Empty;

        /// <summary>
        /// 行号
        /// </summary>
        public int LineNo { get; set; } = 1;

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
        /// 客户物料编码
        /// </summary>
        [StringLength(50)]
        public string? CustomerMaterialCode { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

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
        /// 目标单价
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal? TargetPrice { get; set; }

        /// <summary>
        /// 目标金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal? TargetAmount { get; set; }

        /// <summary>
        /// 报价单价
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal? QuotedPrice { get; set; }

        /// <summary>
        /// 报价金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal? QuotedAmount { get; set; }

        /// <summary>
        /// 成本单价
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal? CostPrice { get; set; }

        /// <summary>
        /// 成本金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal? CostAmount { get; set; }

        /// <summary>
        /// 利润率(%)
        /// </summary>
        [Column(TypeName = "numeric(5,2)")]
        public decimal? ProfitRate { get; set; }

        /// <summary>
        /// 品牌要求
        /// </summary>
        [StringLength(100)]
        public string? BrandRequirement { get; set; }

        /// <summary>
        /// 产地要求
        /// </summary>
        [StringLength(100)]
        public string? OriginRequirement { get; set; }

        /// <summary>
        /// 交货日期
        /// </summary>
        public DateTime? DeliveryDate { get; set; }

        /// <summary>
        /// 最小订单量
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal? MOQ { get; set; }

        /// <summary>
        /// 包装规格
        /// </summary>
        [StringLength(200)]
        public string? PackagingSpec { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        /// <summary>
        /// 状态 (0:待报价 1:已报价 2:已接受 3:已拒绝)
        /// </summary>
        public short Status { get; set; } = 0;

        // 导航属性
        [ForeignKey("RFQId")]
        public virtual RFQ? RFQ { get; set; }
    }
}
