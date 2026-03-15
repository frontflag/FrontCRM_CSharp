using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Quote
{
    /// <summary>
    /// 报价单主表 (Quotation)
    /// </summary>
    [Table("quote")]
    public class Quote : BaseGuidEntity
    {
        /// <summary>
        /// 报价单ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("QuoteId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 报价单号
        /// </summary>
        [Required]
        [StringLength(32)]
        public string QuoteCode { get; set; } = string.Empty;

        /// <summary>
        /// 报价单类型 (1:销售报价 2:采购报价)
        /// </summary>
        public short QuoteType { get; set; } = 1;

        /// <summary>
        /// 询价单ID (外键)
        /// </summary>
        [StringLength(36)]
        public string? RFQId { get; set; }

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
        /// 报价日期
        /// </summary>
        public DateTime QuoteDate { get; set; } = DateTime.UtcNow;

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
        /// 报价总金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal TotalAmount { get; set; } = 0.00m;

        /// <summary>
        /// 成本总金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal TotalCost { get; set; } = 0.00m;

        /// <summary>
        /// 利润总金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal TotalProfit { get; set; } = 0.00m;

        /// <summary>
        /// 总利润率(%)
        /// </summary>
        [Column(TypeName = "numeric(5,2)")]
        public decimal? TotalProfitRate { get; set; }

        /// <summary>
        /// 行项目数
        /// </summary>
        public int ItemRows { get; set; } = 0;

        /// <summary>
        /// 状态 (0:草稿 1:待审核 2:已审核 3:已发送 4:已接受 5:已拒绝 6:已过期 7:已关闭)
        /// </summary>
        public short Status { get; set; } = 0;

        /// <summary>
        /// 版本号
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// 是否最新版本
        /// </summary>
        public bool IsLatestVersion { get; set; } = true;

        /// <summary>
        /// 父报价单ID (用于版本管理)
        /// </summary>
        [StringLength(36)]
        public string? ParentQuoteId { get; set; }

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
        /// 付款条款
        /// </summary>
        [StringLength(500)]
        public string? PaymentTerms { get; set; }

        /// <summary>
        /// 交货条款
        /// </summary>
        [StringLength(500)]
        public string? DeliveryTerms { get; set; }

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
        /// 运输方式
        /// </summary>
        public short? ShippingMethod { get; set; }

        /// <summary>
        /// 运费承担方 (1:卖方 2:买方)
        /// </summary>
        public short? FreightBearer { get; set; }

        /// <summary>
        /// 运费金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal? FreightAmount { get; set; }

        /// <summary>
        /// 包装费用
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal? PackagingFee { get; set; }

        /// <summary>
        /// 保险费用
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal? InsuranceFee { get; set; }

        /// <summary>
        /// 其他费用
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal? OtherFee { get; set; }

        /// <summary>
        /// 折扣金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal? DiscountAmount { get; set; }

        /// <summary>
        /// 折扣率(%)
        /// </summary>
        [Column(TypeName = "numeric(5,2)")]
        public decimal? DiscountRate { get; set; }

        /// <summary>
        /// 含税总金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal? TotalAmountWithTax { get; set; }

        /// <summary>
        /// 税率(%)
        /// </summary>
        [Column(TypeName = "numeric(5,2)")]
        public decimal? TaxRate { get; set; }

        /// <summary>
        /// 税额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal? TaxAmount { get; set; }

        /// <summary>
        /// 质保期(月)
        /// </summary>
        public int? WarrantyPeriod { get; set; }

        /// <summary>
        /// 报价条款
        /// </summary>
        public string? QuoteTerms { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(1000)]
        public string? Remark { get; set; }

        /// <summary>
        /// 客户反馈
        /// </summary>
        [StringLength(1000)]
        public string? CustomerFeedback { get; set; }

        /// <summary>
        /// 拒绝原因
        /// </summary>
        [StringLength(500)]
        public string? RejectReason { get; set; }

        /// <summary>
        /// 附件数量
        /// </summary>
        public int AttachmentCount { get; set; } = 0;

        /// <summary>
        /// 审核人ID
        /// </summary>
        [StringLength(36)]
        public string? ApproverId { get; set; }

        /// <summary>
        /// 审核时间
        /// </summary>
        public DateTime? ApproveTime { get; set; }

        /// <summary>
        /// 审核意见
        /// </summary>
        [StringLength(500)]
        public string? ApproveRemark { get; set; }

        // 导航属性
        public virtual ICollection<QuoteItem> Items { get; set; } = new List<QuoteItem>();
    }

    /// <summary>
    /// 报价单明细表
    /// </summary>
    [Table("quoteitem")]
    public class QuoteItem : BaseGuidEntity
    {
        /// <summary>
        /// 明细ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("ItemId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 报价单ID (外键)
        /// </summary>
        [Required]
        [StringLength(36)]
        public string QuoteId { get; set; } = string.Empty;

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
        /// 成本单价
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal CostPrice { get; set; } = 0.0000m;

        /// <summary>
        /// 成本金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal CostAmount { get; set; } = 0.00m;

        /// <summary>
        /// 报价单价
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal QuotePrice { get; set; } = 0.0000m;

        /// <summary>
        /// 报价金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal QuoteAmount { get; set; } = 0.00m;

        /// <summary>
        /// 利润金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal ProfitAmount { get; set; } = 0.00m;

        /// <summary>
        /// 利润率(%)
        /// </summary>
        [Column(TypeName = "numeric(5,2)")]
        public decimal? ProfitRate { get; set; }

        /// <summary>
        /// 折扣率(%)
        /// </summary>
        [Column(TypeName = "numeric(5,2)")]
        public decimal? DiscountRate { get; set; }

        /// <summary>
        /// 折扣金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal? DiscountAmount { get; set; }

        /// <summary>
        /// 税率(%)
        /// </summary>
        [Column(TypeName = "numeric(5,2)")]
        public decimal? TaxRate { get; set; }

        /// <summary>
        /// 税额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal? TaxAmount { get; set; }

        /// <summary>
        /// 含税单价
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal? PriceWithTax { get; set; }

        /// <summary>
        /// 含税金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal? AmountWithTax { get; set; }

        /// <summary>
        /// 品牌
        /// </summary>
        [StringLength(100)]
        public string? Brand { get; set; }

        /// <summary>
        /// 产地
        /// </summary>
        [StringLength(100)]
        public string? Origin { get; set; }

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
        /// 质保期(月)
        /// </summary>
        public int? WarrantyPeriod { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        /// <summary>
        /// 状态 (0:有效 1:已取消)
        /// </summary>
        public short Status { get; set; } = 0;

        // 导航属性
        [ForeignKey("QuoteId")]
        public virtual Quote? Quote { get; set; }
    }
}
