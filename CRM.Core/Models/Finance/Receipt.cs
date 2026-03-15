using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Finance
{
    /// <summary>
    /// 收款单主表
    /// 向客户收款 ← 销售订单
    /// </summary>
    [Table("receipt")]
    public class Receipt : BaseGuidEntity
    {
        /// <summary>
        /// 收款单ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("ReceiptId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 收款单号
        /// </summary>
        [Required]
        [StringLength(32)]
        public string ReceiptCode { get; set; } = string.Empty;

        /// <summary>
        /// 收款类型 (1:销售收款 2:预收款 3:退款 4:其他)
        /// </summary>
        public short ReceiptType { get; set; } = 1;

        /// <summary>
        /// 客户ID
        /// </summary>
        [Required]
        [StringLength(36)]
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>
        /// 客户名称
        /// </summary>
        [StringLength(200)]
        public string? CustomerName { get; set; }

        /// <summary>
        /// 销售订单ID
        /// </summary>
        [StringLength(36)]
        public string? SellOrderId { get; set; }

        /// <summary>
        /// 销售订单编号
        /// </summary>
        [StringLength(50)]
        public string? SellOrderCode { get; set; }

        /// <summary>
        /// 关联发票ID
        /// </summary>
        [StringLength(36)]
        public string? InvoiceId { get; set; }

        /// <summary>
        /// 关联发票号码
        /// </summary>
        [StringLength(50)]
        public string? InvoiceNo { get; set; }

        /// <summary>
        /// 收款日期
        /// </summary>
        public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 应收金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceivableAmount { get; set; } = 0.00m;

        /// <summary>
        /// 实收金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceiptAmount { get; set; } = 0.00m;

        /// <summary>
        /// 折扣金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal DiscountAmount { get; set; } = 0.00m;

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
        /// 收款方式 (1:银行转账 2:现金 3:支票 4:承兑汇票 5:信用证 6:支付宝 7:微信)
        /// </summary>
        public short ReceiptMethod { get; set; } = 1;

        /// <summary>
        /// 收款账户ID
        /// </summary>
        [StringLength(36)]
        public string? ReceiptAccountId { get; set; }

        /// <summary>
        /// 收款账户名称
        /// </summary>
        [StringLength(100)]
        public string? ReceiptAccountName { get; set; }

        /// <summary>
        /// 收款银行
        /// </summary>
        [StringLength(100)]
        public string? ReceiptBank { get; set; }

        /// <summary>
        /// 付款方账户
        /// </summary>
        [StringLength(50)]
        public string? PayerAccount { get; set; }

        /// <summary>
        /// 付款方银行
        /// </summary>
        [StringLength(100)]
        public string? PayerBank { get; set; }

        /// <summary>
        /// 付款人
        /// </summary>
        [StringLength(50)]
        public string? Payer { get; set; }

        /// <summary>
        /// 状态 (0:草稿 1:待审核 2:已审核 3:已收款 4:已取消)
        /// </summary>
        public short Status { get; set; } = 0;

        /// <summary>
        /// 经手人ID
        /// </summary>
        [StringLength(36)]
        public string? HandlerId { get; set; }

        /// <summary>
        /// 经手人名称
        /// </summary>
        [StringLength(50)]
        public string? HandlerName { get; set; }

        /// <summary>
        /// 审核人ID
        /// </summary>
        [StringLength(36)]
        public string? ApproverId { get; set; }

        /// <summary>
        /// 审核人名称
        /// </summary>
        [StringLength(50)]
        public string? ApproverName { get; set; }

        /// <summary>
        /// 审核时间
        /// </summary>
        public DateTime? ApproveTime { get; set; }

        /// <summary>
        /// 审核意见
        /// </summary>
        [StringLength(500)]
        public string? ApproveRemark { get; set; }

        /// <summary>
        /// 确认人ID
        /// </summary>
        [StringLength(36)]
        public string? ConfirmerId { get; set; }

        /// <summary>
        /// 确认人名称
        /// </summary>
        [StringLength(50)]
        public string? ConfirmerName { get; set; }

        /// <summary>
        /// 银行流水号
        /// </summary>
        [StringLength(100)]
        public string? BankSerialNo { get; set; }

        /// <summary>
        /// 是否预收款
        /// </summary>
        public bool IsAdvanceReceipt { get; set; } = false;

        /// <summary>
        /// 预付比例(%)
        /// </summary>
        [Column(TypeName = "numeric(5,2)")]
        public decimal? AdvanceRate { get; set; }

        /// <summary>
        /// 用途/摘要
        /// </summary>
        [StringLength(200)]
        public string? Purpose { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        // 导航属性
        public virtual ICollection<ReceiptItem> Items { get; set; } = new List<ReceiptItem>();
    }

    /// <summary>
    /// 收款单明细表
    /// </summary>
    [Table("receiptitem")]
    public class ReceiptItem : BaseGuidEntity
    {
        /// <summary>
        /// 明细ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("ItemId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 收款单ID (外键)
        /// </summary>
        [Required]
        [StringLength(36)]
        public string ReceiptId { get; set; } = string.Empty;

        /// <summary>
        /// 行号
        /// </summary>
        public int LineNo { get; set; } = 1;

        /// <summary>
        /// 销售订单ID
        /// </summary>
        [StringLength(36)]
        public string? SellOrderId { get; set; }

        /// <summary>
        /// 销售订单编号
        /// </summary>
        [StringLength(50)]
        public string? SellOrderCode { get; set; }

        /// <summary>
        /// 本次收款金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceiptAmount { get; set; } = 0.00m;

        /// <summary>
        /// 订单总金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal OrderTotalAmount { get; set; } = 0.00m;

        /// <summary>
        /// 已收款金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceivedAmount { get; set; } = 0.00m;

        /// <summary>
        /// 未收款金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal UnreceivedAmount { get; set; } = 0.00m;

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(200)]
        public string? Remark { get; set; }

        // 导航属性
        [ForeignKey("ReceiptId")]
        public virtual Receipt? Receipt { get; set; }
    }

    /// <summary>
    /// 收款类型枚举
    /// </summary>
    public enum ReceiptType
    {
        销售收款 = 1,
        预收款 = 2,
        退款 = 3,
        其他 = 4
    }

    /// <summary>
    /// 收款方式枚举
    /// </summary>
    public enum ReceiptMethod
    {
        银行转账 = 1,
        现金 = 2,
        支票 = 3,
        承兑汇票 = 4,
        信用证 = 5,
        支付宝 = 6,
        微信支付 = 7
    }

    /// <summary>
    /// 收款状态枚举
    /// </summary>
    public enum ReceiptStatus
    {
        草稿 = 0,
        待审核 = 1,
        已审核 = 2,
        已收款 = 3,
        已取消 = 4
    }
}
