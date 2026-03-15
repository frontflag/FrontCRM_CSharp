using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Finance
{
    /// <summary>
    /// 发票主表
    /// 进项发票(InvoiceType=1) → 采购订单
    /// 销项发票(InvoiceType=2) → 销售订单
    /// </summary>
    [Table("invoice")]
    public class Invoice : BaseGuidEntity
    {
        /// <summary>
        /// 发票ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("InvoiceId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 发票号码
        /// </summary>
        [Required]
        [StringLength(50)]
        public string InvoiceNo { get; set; } = string.Empty;

        /// <summary>
        /// 发票代码
        /// </summary>
        [StringLength(50)]
        public string? InvoiceCode { get; set; }

        /// <summary>
        /// 发票类型 (1:进项发票 2:销项发票)
        /// </summary>
        [Required]
        public short InvoiceType { get; set; }

        /// <summary>
        /// 发票种类 (1:增值税专用发票 2:增值税普通发票 3:电子发票)
        /// </summary>
        public short InvoiceCategory { get; set; } = 1;

        /// <summary>
        /// 关联订单类型 (SellOrder/PurchaseOrder)
        /// </summary>
        [StringLength(50)]
        public string? OrderType { get; set; }

        /// <summary>
        /// 关联订单ID
        /// </summary>
        [StringLength(36)]
        public string? OrderId { get; set; }

        /// <summary>
        /// 关联订单编号
        /// </summary>
        [StringLength(50)]
        public string? OrderCode { get; set; }

        /// <summary>
        /// 客户ID (销项发票)
        /// </summary>
        [StringLength(36)]
        public string? CustomerId { get; set; }

        /// <summary>
        /// 客户名称
        /// </summary>
        [StringLength(200)]
        public string? CustomerName { get; set; }

        /// <summary>
        /// 供应商ID (进项发票)
        /// </summary>
        [StringLength(36)]
        public string? VendorId { get; set; }

        /// <summary>
        /// 供应商名称
        /// </summary>
        [StringLength(200)]
        public string? VendorName { get; set; }

        /// <summary>
        /// 开票日期
        /// </summary>
        public DateTime InvoiceDate { get; set; }

        /// <summary>
        /// 开票金额(不含税)
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal Amount { get; set; } = 0.00m;

        /// <summary>
        /// 税率(%)
        /// </summary>
        [Column(TypeName = "numeric(5,2)")]
        public decimal TaxRate { get; set; } = 13.00m;

        /// <summary>
        /// 税额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal TaxAmount { get; set; } = 0.00m;

        /// <summary>
        /// 价税合计
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal TotalAmount { get; set; } = 0.00m;

        /// <summary>
        /// 币种
        /// </summary>
        public short Currency { get; set; } = 1;

        /// <summary>
        /// 汇率
        /// </summary>
        [Column(TypeName = "numeric(18,6)")]
        public decimal ExchangeRate { get; set; } = 1.000000m;

        /// <summary>
        /// 开票单位名称
        /// </summary>
        [StringLength(200)]
        public string? SellerName { get; set; }

        /// <summary>
        /// 开票单位税号
        /// </summary>
        [StringLength(50)]
        public string? SellerTaxNo { get; set; }

        /// <summary>
        /// 开票单位地址电话
        /// </summary>
        [StringLength(200)]
        public string? SellerAddressPhone { get; set; }

        /// <summary>
        /// 开票单位银行账号
        /// </summary>
        [StringLength(200)]
        public string? SellerBankAccount { get; set; }

        /// <summary>
        /// 收票单位名称
        /// </summary>
        [StringLength(200)]
        public string? BuyerName { get; set; }

        /// <summary>
        /// 收票单位税号
        /// </summary>
        [StringLength(50)]
        public string? BuyerTaxNo { get; set; }

        /// <summary>
        /// 收票单位地址电话
        /// </summary>
        [StringLength(200)]
        public string? BuyerAddressPhone { get; set; }

        /// <summary>
        /// 收票单位银行账号
        /// </summary>
        [StringLength(200)]
        public string? BuyerBankAccount { get; set; }

        /// <summary>
        /// 发票状态 (0:待认证 1:已认证 2:已作废 3:已红冲)
        /// </summary>
        public short Status { get; set; } = 0;

        /// <summary>
        /// 认证日期
        /// </summary>
        public DateTime? CertificationDate { get; set; }

        /// <summary>
        /// 是否已抵扣
        /// </summary>
        public bool IsDeducted { get; set; } = false;

        /// <summary>
        /// 抵扣日期
        /// </summary>
        public DateTime? DeductionDate { get; set; }

        /// <summary>
        /// 扫描件路径
        /// </summary>
        [StringLength(500)]
        public string? ScanFilePath { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        // 导航属性
        public virtual ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    }

    /// <summary>
    /// 发票明细表
    /// </summary>
    [Table("invoiceitem")]
    public class InvoiceItem : BaseGuidEntity
    {
        /// <summary>
        /// 明细ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("ItemId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 发票ID (外键)
        /// </summary>
        [Required]
        [StringLength(36)]
        public string InvoiceId { get; set; } = string.Empty;

        /// <summary>
        /// 行号
        /// </summary>
        public int LineNo { get; set; } = 1;

        /// <summary>
        /// 货物或应税劳务名称
        /// </summary>
        [StringLength(200)]
        public string? GoodsName { get; set; }

        /// <summary>
        /// 规格型号
        /// </summary>
        [StringLength(100)]
        public string? Specification { get; set; }

        /// <summary>
        /// 单位
        /// </summary>
        [StringLength(20)]
        public string? Unit { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal Quantity { get; set; } = 0.0000m;

        /// <summary>
        /// 单价(不含税)
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal UnitPrice { get; set; } = 0.0000m;

        /// <summary>
        /// 金额(不含税)
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal Amount { get; set; } = 0.00m;

        /// <summary>
        /// 税率(%)
        /// </summary>
        [Column(TypeName = "numeric(5,2)")]
        public decimal TaxRate { get; set; } = 13.00m;

        /// <summary>
        /// 税额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal TaxAmount { get; set; } = 0.00m;

        /// <summary>
        /// 关联订单明细ID
        /// </summary>
        [StringLength(36)]
        public string? OrderItemId { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(200)]
        public string? Remark { get; set; }

        // 导航属性
        [ForeignKey("InvoiceId")]
        public virtual Invoice? Invoice { get; set; }
    }

    /// <summary>
    /// 发票类型枚举
    /// </summary>
    public enum InvoiceType
    {
        进项发票 = 1,
        销项发票 = 2
    }

    /// <summary>
    /// 发票种类枚举
    /// </summary>
    public enum InvoiceCategory
    {
        增值税专用发票 = 1,
        增值税普通发票 = 2,
        电子发票 = 3
    }

    /// <summary>
    /// 发票状态枚举
    /// </summary>
    public enum InvoiceStatus
    {
        待认证 = 0,
        已认证 = 1,
        已作废 = 2,
        已红冲 = 3
    }
}
