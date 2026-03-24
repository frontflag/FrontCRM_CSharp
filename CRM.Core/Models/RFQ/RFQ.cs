using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.RFQ
{
    /// <summary>
    /// 需求主表 (RFQ - Request for Quotation)
    /// 业务文档: 一个需求单可包含多个物料明细(1:N)
    /// </summary>
    [Table("rfq")]
    public class RFQ : BaseEntity
    {
        /// <summary>主键ID</summary>
        [Key]
        [StringLength(36)]
        [Column("rfq_id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>需求单号 (唯一, 系统自动生成, 格式: RF+年月日+序号)</summary>
        [Required]
        [StringLength(32)]
        [Column("rfq_code")]
        public string RfqCode { get; set; } = string.Empty;

        /// <summary>客户ID (外键)</summary>
        [StringLength(36)]
        [Column("customer_id")]
        public string? CustomerId { get; set; }

        /// <summary>客户名称（仅详情/列表展示用，不入库）</summary>
        [NotMapped]
        public string? CustomerName { get; set; }

        /// <summary>业务员显示名（仅详情展示用，不入库）</summary>
        [NotMapped]
        public string? SalesUserName { get; set; }

        /// <summary>联系人姓名（仅详情展示用，不入库）</summary>
        [NotMapped]
        public string? ContactPersonName { get; set; }

        /// <summary>客户联系人ID</summary>
        [StringLength(36)]
        [Column("contact_id")]
        public string? ContactId { get; set; }

        /// <summary>联系人邮箱</summary>
        [StringLength(200)]
        [Column("contact_email")]
        public string? ContactEmail { get; set; }

        /// <summary>业务员ID</summary>
        [StringLength(36)]
        [Column("sales_user_id")]
        public string? SalesUserId { get; set; }

        /// <summary>需求类型 (1:现货 2:期货 3:样品 4:批量)</summary>
        [Column("rfq_type")]
        public short RfqType { get; set; } = 1;

        /// <summary>报价方式 (1:不接受任何消息 2:仅邮件 3:仅系统 4:全部方式)</summary>
        [Column("quote_method")]
        public short QuoteMethod { get; set; } = 1;

        /// <summary>分配方式 (1:系统分配多人采购 2:系统分配单人采购 3:手动分配)</summary>
        [Column("assign_method")]
        public short AssignMethod { get; set; } = 1;

        /// <summary>行业</summary>
        [StringLength(100)]
        [Column("industry")]
        public string? Industry { get; set; }

        /// <summary>产品</summary>
        [StringLength(200)]
        [Column("product")]
        public string? Product { get; set; }

        /// <summary>目标类型 (1:比价需求 2:独家需求 3:紧急需求 4:常规需求)</summary>
        [Column("target_type")]
        public short TargetType { get; set; } = 1;

        /// <summary>重要程度 (1-10)</summary>
        [Column("importance")]
        public short Importance { get; set; } = 5;

        /// <summary>是否最后一次询价</summary>
        [Column("is_last_inquiry")]
        public bool IsLastInquiry { get; set; } = false;

        /// <summary>项目背景</summary>
        [StringLength(500)]
        [Column("project_background")]
        public string? ProjectBackground { get; set; }

        /// <summary>竞争对手</summary>
        [StringLength(200)]
        [Column("competitor")]
        public string? Competitor { get; set; }

        /// <summary>状态 (0:待分配 1:已分配 2:报价中 3:已报价 4:已选价 5:已转订单 6:已关闭)</summary>
        [Column("status")]
        public short Status { get; set; } = 0;

        /// <summary>明细数量 (冗余字段, 方便列表查询)</summary>
        [Column("item_count")]
        public int ItemCount { get; set; } = 0;

        /// <summary>备注</summary>
        [StringLength(500)]
        [Column("remark")]
        public string? Remark { get; set; }

        /// <summary>需求日期</summary>
        [Column("rfq_date")]
        public DateTime RfqDate { get; set; } = DateTime.UtcNow;

        // 导航属性
        public virtual ICollection<RFQItem> Items { get; set; } = new List<RFQItem>();
    }

    /// <summary>
    /// 需求明细表 (RFQ Item)
    /// 业务文档: 一个需求明细可收到多个供应商报价(1:N)
    /// </summary>
    [Table("rfqitem")]
    public class RFQItem : BaseEntity
    {
        /// <summary>明细ID (主键)</summary>
        [Key]
        [StringLength(36)]
        [Column("item_id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>需求ID (外键)</summary>
        [Required]
        [StringLength(36)]
        [Column("rfq_id")]
        public string RfqId { get; set; } = string.Empty;

        /// <summary>行号</summary>
        [Column("line_no")]
        public int LineNo { get; set; } = 1;

        /// <summary>客户物料型号 (客户自己的物料编号, 最多100字符)</summary>
        [StringLength(100)]
        [Column("customer_mpn")]
        public string? CustomerMpn { get; set; }

        /// <summary>物料型号 (标准物料编号, 必填)</summary>
        [Required]
        [StringLength(200)]
        [Column("mpn")]
        public string Mpn { get; set; } = string.Empty;

        /// <summary>物料型号（与 Mpn 同义，供前端字段 materialModel 序列化）</summary>
        [NotMapped]
        public string? MaterialModel
        {
            get => string.IsNullOrEmpty(Mpn) ? null : Mpn;
            set => Mpn = value ?? string.Empty;
        }

        /// <summary>客户物料型号（与 CustomerMpn 同义，供前端 customerMaterialModel）</summary>
        [NotMapped]
        public string? CustomerMaterialModel
        {
            get => CustomerMpn;
            set => CustomerMpn = value;
        }

        /// <summary>客户品牌 (客户指定品牌, 必填)</summary>
        [Required]
        [StringLength(100)]
        [Column("customer_brand")]
        public string CustomerBrand { get; set; } = string.Empty;

        /// <summary>品牌 (实际供应品牌, 如 VISHAY/威世, 必填)</summary>
        [Required]
        [StringLength(100)]
        [Column("brand")]
        public string Brand { get; set; } = string.Empty;

        /// <summary>目标价格</summary>
        [Column("target_price", TypeName = "numeric(18,4)")]
        public decimal? TargetPrice { get; set; }

        /// <summary>目标价币种 (1:RMB 2:USD 3:EUR 4:HKD)</summary>
        [Column("price_currency")]
        public short PriceCurrency { get; set; } = 1;

        /// <summary>数量</summary>
        [Column("quantity", TypeName = "numeric(18,4)")]
        public decimal Quantity { get; set; } = 1;

        /// <summary>生产日期要求 (文本, 如"2年内")</summary>
        [StringLength(50)]
        [Column("production_date")]
        public string? ProductionDate { get; set; }

        /// <summary>失效日期</summary>
        [Column("expiry_date")]
        public DateTime? ExpiryDate { get; set; }

        /// <summary>最小包装数</summary>
        [Column("min_package_qty", TypeName = "numeric(18,4)")]
        public decimal? MinPackageQty { get; set; }

        /// <summary>最小起订量</summary>
        [Column("moq", TypeName = "numeric(18,4)")]
        public decimal? Moq { get; set; }

        /// <summary>可替代料 (逗号分隔)</summary>
        [StringLength(500)]
        [Column("alternatives")]
        public string? Alternatives { get; set; }

        /// <summary>备注</summary>
        [StringLength(500)]
        [Column("remark")]
        public string? Remark { get; set; }

        /// <summary>状态 (0:待报价 1:已报价 2:已接受 3:已拒绝)</summary>
        [Column("status")]
        public short Status { get; set; } = 0;

        // 导航属性
        [ForeignKey("RfqId")]
        public virtual RFQ? RFQ { get; set; }
    }
}
