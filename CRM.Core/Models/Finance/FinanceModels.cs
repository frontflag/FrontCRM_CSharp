using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Finance
{
    // ==================== 付款模块 ====================

    /// <summary>
    /// 付款单主表 (FinancePayment)
    /// </summary>
    [Table("financepayment")]
    public class FinancePayment : BaseGuidEntity
    {
        [Key][StringLength(36)][Column("FinancePaymentId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>付款单号（如 PAY + 5 位 32 进制，前缀见 sys_serial_number.FinancePayment）</summary>
        [Required][StringLength(16)]
        public string FinancePaymentCode { get; set; } = string.Empty;

        /// <summary>供应商ID</summary>
        [Required][StringLength(36)]
        public string VendorId { get; set; } = string.Empty;

        /// <summary>供应商名称（冗余）</summary>
        [StringLength(200)]
        public string? VendorName { get; set; }

        /// <summary>供应商编码（展示用，来自 vendorinfo，不落库）</summary>
        [NotMapped]
        public string? VendorCode { get; set; }

        /// <summary>付款状态 1新建 2待审核 10审核通过 100付款完成 -1审核失败 -2取消</summary>
        public short Status { get; set; } = 1;

        /// <summary>待付总额（请款金额）</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal PaymentAmountToBe { get; set; } = 0m;

        /// <summary>已付货款总额</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal PaymentAmount { get; set; } = 0m;

        /// <summary>已付总额（含费用）</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal PaymentTotalAmount { get; set; } = 0m;

        /// <summary>付款币别 1:人民币 2:美元 3:欧元</summary>
        public byte PaymentCurrency { get; set; } = 1;

        /// <summary>付款日期</summary>
        public DateTime? PaymentDate { get; set; }

        /// <summary>付款人ID</summary>
        [StringLength(36)]
        public string? PaymentUserId { get; set; }

        /// <summary>付款方式 1:银行转账 2:现金 3:支票 4:承兑汇票</summary>
        public short PaymentMode { get; set; } = 1;

        /// <summary>银行水单号码</summary>
        [StringLength(100)]
        public string? BankSlipNo { get; set; }

        /// <summary>备注</summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        [StringLength(36)]
        [Column("create_by_user_id")]
        public string? CreateByUserId { get; set; }

        [StringLength(36)]
        [Column("modify_by_user_id")]
        public string? ModifyByUserId { get; set; }

        /// <summary>创建人显示名（列表/详情 API 填充，不落库）</summary>
        [NotMapped]
        public string? CreateUserName { get; set; }

        public virtual ICollection<FinancePaymentItem> Items { get; set; } = new List<FinancePaymentItem>();
    }

    /// <summary>
    /// 付款明细表 (FinancePaymentItem)
    /// </summary>
    [Table("financepaymentitem")]
    public class FinancePaymentItem : BaseGuidEntity
    {
        [Key][StringLength(36)][Column("FinancePaymentItemId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>付款单ID（外键）</summary>
        [Required][StringLength(36)]
        public string FinancePaymentId { get; set; } = string.Empty;

        /// <summary>采购订单ID</summary>
        [StringLength(36)]
        public string? PurchaseOrderId { get; set; }

        /// <summary>采购订单明细ID</summary>
        [StringLength(36)]
        public string? PurchaseOrderItemId { get; set; }

        /// <summary>已付金额</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal PaymentAmount { get; set; } = 0m;

        /// <summary>请款金额</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal PaymentAmountToBe { get; set; } = 0m;

        /// <summary>物料ID</summary>
        [StringLength(36)]
        public string? ProductId { get; set; }

        /// <summary>物料型号</summary>
        [StringLength(64)]
        public string? PN { get; set; }

        /// <summary>品牌</summary>
        [StringLength(64)]
        public string? Brand { get; set; }

        /// <summary>核销状态 0未核销 1部分核销 2核销完成</summary>
        public short VerificationStatus { get; set; } = 0;

        /// <summary>已核销金额</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal VerificationDone { get; set; } = 0m;

        /// <summary>待核销金额</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal VerificationToBe { get; set; } = 0m;

        [ForeignKey("FinancePaymentId")]
        public virtual FinancePayment? Payment { get; set; }
    }

    // ==================== 收款模块 ====================

    /// <summary>
    /// 收款单主表 (FinanceReceipt)
    /// </summary>
    [Table("financereceipt")]
    public class FinanceReceipt : BaseGuidEntity
    {
        [Key][StringLength(36)][Column("FinanceReceiptId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>收款单号（如 REC + 5 位 32 进制，前缀见 sys_serial_number.Receipt）</summary>
        [Required][StringLength(32)]
        public string FinanceReceiptCode { get; set; } = string.Empty;

        /// <summary>客户ID</summary>
        [Required][StringLength(36)]
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>客户名称（冗余）</summary>
        [StringLength(200)]
        public string? CustomerName { get; set; }

        /// <summary>业务员ID</summary>
        [StringLength(36)]
        public string? SalesUserId { get; set; }

        /// <summary>采购员组ID</summary>
        [StringLength(36)]
        public string? PurchaseGroupId { get; set; }

        /// <summary>收款状态 0草稿 1待审核 2已审核 3已收款 4已取消</summary>
        public short Status { get; set; } = 0;

        /// <summary>收款总额</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceiptAmount { get; set; } = 0m;

        /// <summary>收款币别 1:人民币 2:美元 3:欧元</summary>
        public byte ReceiptCurrency { get; set; } = 1;

        /// <summary>收款日期</summary>
        public DateTime? ReceiptDate { get; set; }

        /// <summary>收款人ID</summary>
        [StringLength(36)]
        public string? ReceiptUserId { get; set; }

        /// <summary>收款方式 1:银行转账 2:现金 3:支票 4:承兑汇票</summary>
        public short ReceiptMode { get; set; } = 1;

        /// <summary>收款银行ID</summary>
        [StringLength(36)]
        public string? ReceiptBankId { get; set; }

        /// <summary>银行水单号码</summary>
        [StringLength(100)]
        public string? BankSlipNo { get; set; }

        /// <summary>备注</summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        [StringLength(36)]
        [Column("create_by_user_id")]
        public string? CreateByUserId { get; set; }

        [StringLength(36)]
        [Column("modify_by_user_id")]
        public string? ModifyByUserId { get; set; }

        /// <summary>创建人显示名（列表/详情 API 填充，不落库）</summary>
        [NotMapped]
        public string? CreateUserName { get; set; }

        public virtual ICollection<FinanceReceiptItem> Items { get; set; } = new List<FinanceReceiptItem>();
    }

    /// <summary>
    /// 收款明细表 (FinanceReceiptItem)
    /// </summary>
    [Table("financereceiptitem")]
    public class FinanceReceiptItem : BaseGuidEntity
    {
        [Key][StringLength(36)][Column("FinanceReceiptItemId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>收款单ID（外键）</summary>
        [Required][StringLength(36)]
        public string FinanceReceiptId { get; set; } = string.Empty;

        /// <summary>销售订单ID</summary>
        [StringLength(36)]
        public string? SellOrderId { get; set; }

        /// <summary>销售订单明细ID</summary>
        [StringLength(36)]
        public string? SellOrderItemId { get; set; }

        /// <summary>销项发票ID</summary>
        [StringLength(36)]
        public string? FinanceSellInvoiceId { get; set; }

        /// <summary>销项发票明细ID</summary>
        [StringLength(36)]
        public string? FinanceSellInvoiceItemId { get; set; }

        /// <summary>收款金额</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceiptAmount { get; set; } = 0m;

        /// <summary>收款折算金额</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceiptConvertAmount { get; set; } = 0m;

        /// <summary>出库明细ID</summary>
        [StringLength(36)]
        public string? StockOutItemId { get; set; }

        /// <summary>物料ID</summary>
        [StringLength(36)]
        public string? ProductId { get; set; }

        /// <summary>物料型号</summary>
        [StringLength(64)]
        public string? PN { get; set; }

        /// <summary>品牌</summary>
        [StringLength(64)]
        public string? Brand { get; set; }

        /// <summary>核销状态 0未核销 1部分核销 2核销完成</summary>
        public short VerificationStatus { get; set; } = 0;

        /// <summary>累计已核销至销项发票的金额（支持多次核销）。</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal VerifiedAmount { get; set; }

        [ForeignKey("FinanceReceiptId")]
        public virtual FinanceReceipt? Receipt { get; set; }
    }

    // ==================== 进项发票模块 ====================

    /// <summary>
    /// 进项发票主表 (FinancePurchaseInvoice)
    /// </summary>
    [Table("financepurchaseinvoice")]
    public class FinancePurchaseInvoice : BaseGuidEntity
    {
        [Key][StringLength(36)][Column("FinancePurchaseInvoiceId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>供应商ID</summary>
        [Required][StringLength(36)]
        public string VendorId { get; set; } = string.Empty;

        /// <summary>供应商名称（冗余）</summary>
        [StringLength(200)]
        public string? VendorName { get; set; }

        /// <summary>发票号码（纸质）</summary>
        [StringLength(32)]
        public string? InvoiceNo { get; set; }

        /// <summary>发票金额（含税总额）</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal InvoiceAmount { get; set; } = 0m;

        /// <summary>物料发票金额</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal BillAmount { get; set; } = 0m;

        /// <summary>增值税额</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal TaxAmount { get; set; } = 0m;

        /// <summary>不含税总额</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal ExcludTaxAmount { get; set; } = 0m;

        /// <summary>开票日期</summary>
        public DateTime? InvoiceDate { get; set; }

        /// <summary>认证日期</summary>
        public DateTime? ConfirmDate { get; set; }

        /// <summary>认证状态 0未认证 1已认证</summary>
        public byte ConfirmStatus { get; set; } = 0;

        /// <summary>冲红状态 0正常 1已冲红</summary>
        public short RedInvoiceStatus { get; set; } = 0;

        /// <summary>备注</summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        [StringLength(36)]
        [Column("create_by_user_id")]
        public string? CreateByUserId { get; set; }

        [StringLength(36)]
        [Column("modify_by_user_id")]
        public string? ModifyByUserId { get; set; }

        public virtual ICollection<FinancePurchaseInvoiceItem> Items { get; set; } = new List<FinancePurchaseInvoiceItem>();
    }

    /// <summary>
    /// 进项发票明细表 (FinancePurchaseInvoiceItem)
    /// </summary>
    [Table("financepurchaseinvoiceitem")]
    public class FinancePurchaseInvoiceItem : BaseGuidEntity
    {
        [Key][StringLength(36)][Column("FinancePurchaseInvoiceItemId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>进项发票ID（外键）</summary>
        [Required][StringLength(36)]
        public string FinancePurchaseInvoiceId { get; set; } = string.Empty;

        /// <summary>入库ID</summary>
        [StringLength(36)]
        public string? StockInId { get; set; }

        /// <summary>入库单号</summary>
        [StringLength(32)]
        public string? StockInCode { get; set; }

        /// <summary>采购单号</summary>
        [StringLength(32)]
        public string? PurchaseOrderCode { get; set; }

        /// <summary>采购单价（入库）</summary>
        [Column(TypeName = "numeric(18,6)")]
        public decimal StockInCost { get; set; } = 0m;

        /// <summary>票据采购单价</summary>
        [Column(TypeName = "numeric(18,6)")]
        public decimal BillCost { get; set; } = 0m;

        /// <summary>发票物料数量</summary>
        public long BillQty { get; set; } = 0;

        /// <summary>发票含税金额</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal BillAmount { get; set; } = 0m;

        /// <summary>税率</summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal TaxRate { get; set; } = 0m;

        /// <summary>增值税额</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal TaxAmount { get; set; } = 0m;

        /// <summary>不含税总额</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal ExcludTaxAmount { get; set; } = 0m;

        [ForeignKey("FinancePurchaseInvoiceId")]
        public virtual FinancePurchaseInvoice? PurchaseInvoice { get; set; }
    }

    // ==================== 销项发票模块 ====================

    /// <summary>
    /// 销项发票主表 (FinanceSellInvoice)
    /// </summary>
    [Table("financesellinvoice")]
    public class FinanceSellInvoice : BaseGuidEntity
    {
        [Key][StringLength(36)][Column("FinanceSellInvoiceId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>客户ID</summary>
        [Required][StringLength(36)]
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>客户名称（冗余）</summary>
        [StringLength(200)]
        public string? CustomerName { get; set; }

        /// <summary>发票单号（系统编号）</summary>
        [StringLength(32)]
        public string? InvoiceCode { get; set; }

        /// <summary>纸张发票号码</summary>
        [StringLength(64)]
        public string? InvoiceNo { get; set; }

        /// <summary>发票总额</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal InvoiceTotal { get; set; } = 0m;

        /// <summary>开票日期</summary>
        public DateTime? MakeInvoiceDate { get; set; }

        /// <summary>收款状态 0未收款 1部分收款 2收款完成</summary>
        public byte ReceiveStatus { get; set; } = 0;

        /// <summary>已收总额（已核销）</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceiveDone { get; set; } = 0m;

        /// <summary>待收总额（待核销）</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceiveToBe { get; set; } = 0m;

        /// <summary>币别 1:人民币 2:美元 3:欧元</summary>
        public byte Currency { get; set; } = 1;

        /// <summary>发票类型 10:蓝字发票 20:红字发票</summary>
        public short Type { get; set; } = 10;

        /// <summary>发票状态 1未申请 2申请中 100已开票 101开票失败 -1已作废</summary>
        public short InvoiceStatus { get; set; } = 1;

        /// <summary>销项发票类别 100:增值税专用发票 200:增值税普通发票</summary>
        public short SellInvoiceType { get; set; } = 100;

        /// <summary>备注</summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        [StringLength(36)]
        [Column("create_by_user_id")]
        public string? CreateByUserId { get; set; }

        [StringLength(36)]
        [Column("modify_by_user_id")]
        public string? ModifyByUserId { get; set; }

        public virtual ICollection<SellInvoiceItem> Items { get; set; } = new List<SellInvoiceItem>();
    }

    /// <summary>
    /// 销项发票明细表 (SellInvoiceItem)
    /// </summary>
    [Table("sellinvoiceitem")]
    public class SellInvoiceItem : BaseGuidEntity
    {
        [Key][StringLength(36)][Column("FinanceSellInvoiceItemId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>销项发票ID（外键）</summary>
        [Required][StringLength(36)]
        public string FinanceSellInvoiceId { get; set; } = string.Empty;

        /// <summary>开票总额</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal InvoiceTotal { get; set; } = 0m;

        /// <summary>税率</summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal TaxRate { get; set; } = 0m;

        /// <summary>增值税额</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal ValueAddedTax { get; set; } = 0m;

        /// <summary>不含税金额</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal TaxFreeTotal { get; set; } = 0m;

        /// <summary>销售单价</summary>
        [Column(TypeName = "numeric(18,6)")]
        public decimal Price { get; set; } = 0m;

        /// <summary>数量</summary>
        public long Qty { get; set; } = 0;

        /// <summary>出库明细ID</summary>
        [StringLength(36)]
        public string? StockOutItemId { get; set; }

        /// <summary>币别</summary>
        public byte Currency { get; set; } = 1;

        /// <summary>收款状态 0未收款 1部分收款 2收款完成</summary>
        public short ReceiveStatus { get; set; } = 0;

        [ForeignKey("FinanceSellInvoiceId")]
        public virtual FinanceSellInvoice? SellInvoice { get; set; }
    }
}
