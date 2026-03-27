using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Quote
{
    /// <summary>
    /// 报价单主表 (Quote)
    /// 对应数据库表: quote
    /// 说明: 一个报价单对应一个 RFQ 明细行（rfqitem），包含多个供应商报价行（quoteitem）
    /// </summary>
    [Table("quote")]
    public class Quote : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("QuoteId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>报价单号，如 QT0001</summary>
        [Required]
        [StringLength(32)]
        [Column("quote_code")]
        public string QuoteCode { get; set; } = string.Empty;

        /// <summary>关联 RFQ 主表 ID</summary>
        [StringLength(36)]
        [Column("rfq_id")]
        public string? RFQId { get; set; }

        /// <summary>关联 RFQ 明细行 ID（一个报价单对应一个 rfqitem）</summary>
        [StringLength(36)]
        [Column("rfq_item_id")]
        public string? RFQItemId { get; set; }

        /// <summary>物料型号（冗余自 rfqitem，方便查询）</summary>
        [StringLength(200)]
        [Column("mpn")]
        public string? Mpn { get; set; }

        /// <summary>客户 ID</summary>
        [StringLength(36)]
        [Column("customer_id")]
        public string? CustomerId { get; set; }

        /// <summary>业务员 ID</summary>
        [StringLength(36)]
        [Column("sales_user_id")]
        public string? SalesUserId { get; set; }

        /// <summary>采购员 ID</summary>
        [StringLength(36)]
        [Column("purchase_user_id")]
        public string? PurchaseUserId { get; set; }

        /// <summary>报价日期</summary>
        [Column("quote_date")]
        public DateTime QuoteDate { get; set; } = DateTime.UtcNow;

        /// <summary>状态 (0:草稿 1:待审核 2:已审核 3:已发送 4:已接受 5:已拒绝 6:已过期 7:已关闭)</summary>
        [Column("status")]
        public short Status { get; set; } = 0;

        /// <summary>备注</summary>
        [StringLength(1000)]
        [Column("remark")]
        public string? Remark { get; set; }

        // 导航属性
        public virtual ICollection<QuoteItem> Items { get; set; } = new List<QuoteItem>();
    }

    /// <summary>
    /// 报价明细行 (QuoteItem)
    /// 对应数据库表: quoteitem
    /// 说明: 每行对应一个供应商的报价，包含价格、库存、交期等信息
    /// </summary>
    [Table("quoteitem")]
    public class QuoteItem : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("QuoteItemId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>所属报价单 ID（外键）</summary>
        [Required]
        [StringLength(36)]
        [Column("quote_id")]
        public string QuoteId { get; set; } = string.Empty;

        // ─── 供应商信息 ───────────────────────────────────────────────

        /// <summary>供应商 ID</summary>
        [StringLength(36)]
        [Column("vendor_id")]
        public string? VendorId { get; set; }

        /// <summary>供应商名称（如 Digikey）</summary>
        [StringLength(200)]
        [Column("vendor_name")]
        public string? VendorName { get; set; }

        /// <summary>供应商代码（如 VH0LUA）</summary>
        [StringLength(50)]
        [Column("vendor_code")]
        public string? VendorCode { get; set; }

        /// <summary>联系人 ID</summary>
        [StringLength(36)]
        [Column("contact_id")]
        public string? ContactId { get; set; }

        /// <summary>联系人名称（如 Internatinal Sales）</summary>
        [StringLength(100)]
        [Column("contact_name")]
        public string? ContactName { get; set; }

        // ─── 价格类型 ─────────────────────────────────────────────────

        /// <summary>价格类型（如：现货价、期货价、样品价等）</summary>
        [StringLength(50)]
        [Column("price_type")]
        public string? PriceType { get; set; }

        /// <summary>失效日期</summary>
        [Column("expiry_date")]
        public DateTime? ExpiryDate { get; set; }

        // ─── 物料信息 ─────────────────────────────────────────────────

        /// <summary>物料型号（MPN，如 REF3430QDBVRQ1）</summary>
        [StringLength(200)]
        [Column("mpn")]
        public string? Mpn { get; set; }

        /// <summary>品牌（如 TEXAS INSTRUMENTS/德州仪器）</summary>
        [StringLength(200)]
        [Column("brand")]
        public string? Brand { get; set; }

        /// <summary>品牌属地（如 美国）</summary>
        [StringLength(100)]
        [Column("brand_origin")]
        public string? BrandOrigin { get; set; }

        // ─── 时效信息 ─────────────────────────────────────────────────

        /// <summary>生产日期（如 NA、2年内）</summary>
        [StringLength(100)]
        [Column("date_code")]
        public string? DateCode { get; set; }

        /// <summary>交期（如 10-12days DC下单限制）</summary>
        [StringLength(200)]
        [Column("lead_time")]
        public string? LeadTime { get; set; }

        // ─── 涂标 / 产地 ──────────────────────────────────────────────

        /// <summary>涂标 (0:不涂标 1:涂标 2:待确定)</summary>
        [Column("label_type")]
        public short LabelType { get; set; } = 2;

        /// <summary>报价晶圆产地 (0:美产 1:非美产 2:待确定)</summary>
        [Column("wafer_origin")]
        public short WaferOrigin { get; set; } = 2;

        /// <summary>报价封装产地 (0:美产 1:非美产 2:待确定)</summary>
        [Column("package_origin")]
        public short PackageOrigin { get; set; } = 2;

        /// <summary>是否包邮</summary>
        [Column("free_shipping")]
        public bool FreeShipping { get; set; } = false;

        // ─── 价格信息 ─────────────────────────────────────────────────

        /// <summary>报价币别 (统一币别编码：1=RMB 2=USD 3=EUR 4=HKD 5=JPY 6=GBP)</summary>
        [Column("currency")]
        public short Currency { get; set; } = 1;

        /// <summary>报价数量</summary>
        [Column("quantity", TypeName = "numeric(18,4)")]
        public decimal Quantity { get; set; } = 0;

        /// <summary>报价单价（含6位小数，如 2.267910）</summary>
        [Column("unit_price", TypeName = "numeric(18,6)")]
        public decimal UnitPrice { get; set; } = 0;

        /// <summary>折算价（按汇率折算后的价格）</summary>
        [Column("converted_price", TypeName = "numeric(18,6)")]
        public decimal? ConvertedPrice { get; set; }

        // ─── 库存 / 订购信息 ──────────────────────────────────────────

        /// <summary>最小包装数量</summary>
        [Column("min_package_qty")]
        public int MinPackageQty { get; set; } = 0;

        /// <summary>最小包装单位（如 Reel、Tube、Tray）</summary>
        [StringLength(50)]
        [Column("min_package_unit")]
        public string? MinPackageUnit { get; set; }

        /// <summary>库存数量</summary>
        [Column("stock_qty")]
        public int StockQty { get; set; } = 0;

        /// <summary>起订量（MOQ）</summary>
        [Column("moq")]
        public int Moq { get; set; } = 0;

        /// <summary>备注（如 散料）</summary>
        [StringLength(500)]
        [Column("remark")]
        public string? Remark { get; set; }

        /// <summary>状态 (0:有效 1:已取消)</summary>
        [Column("status")]
        public short Status { get; set; } = 0;

        // 导航属性
        public virtual Quote? Quote { get; set; }
    }
}
