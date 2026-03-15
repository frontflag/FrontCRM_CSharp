using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Finance
{
    /// <summary>
    /// 付款单主表
    /// 向供应商付款 ← 采购订单
    /// </summary>
    [Table("payment")]
    public class Payment : BaseGuidEntity
    {
        /// <summary>
        /// 付款单ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("PaymentId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 付款单号
        /// </summary>
        [Required]
        [StringLength(32)]
        public string PaymentCode { get; set; } = string.Empty;

        /// <summary>
        /// 付款类型 (1:采购付款 2:费用付款 3:预付款 4:其他)
        /// </summary>
        public short PaymentType { get; set; } = 1;

        /// <summary>
        /// 供应商ID
        /// </summary>
        [Required]
        [StringLength(36)]
        public string VendorId { get; set; } = string.Empty;

        /// <summary>
        /// 供应商名称
        /// </summary>
        [StringLength(200)]
        public string? VendorName { get; set; }

        /// <summary>
        /// 采购订单ID
        /// </summary>
        [StringLength(36)]
        public string? PurchaseOrderId { get; set; }

        /// <summary>
        /// 采购订单编号
        /// </summary>
        [StringLength(50)]
        public string? PurchaseOrderCode { get; set; }

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
        /// 申请日期
        /// </summary>
        public DateTime ApplyDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 付款日期
        /// </summary>
        public DateTime? PaymentDate { get; set; }

        /// <summary>
        /// 申请金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal ApplyAmount { get; set; } = 0.00m;

        /// <summary>
        /// 付款金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal PaymentAmount { get; set; } = 0.00m;

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
        /// 付款方式 (1:银行转账 2:现金 3:支票 4:承兑汇票 5:信用证)
        /// </summary>
        public short PaymentMethod { get; set; } = 1;

        /// <summary>
        /// 付款账户ID
        /// </summary>
        [StringLength(36)]
        public string? PaymentAccountId { get; set; }

        /// <summary>
        /// 付款账户名称
        /// </summary>
        [StringLength(100)]
        public string? PaymentAccountName { get; set; }

        /// <summary>
        /// 付款银行
        /// </summary>
        [StringLength(100)]
        public string? PaymentBank { get; set; }

        /// <summary>
        /// 收款账户
        /// </summary>
        [StringLength(50)]
        public string? ReceiveAccount { get; set; }

        /// <summary>
        /// 收款银行
        /// </summary>
        [StringLength(100)]
        public string? ReceiveBank { get; set; }

        /// <summary>
        /// 收款人
        /// </summary>
        [StringLength(50)]
        public string? Payee { get; set; }

        /// <summary>
        /// 状态 (0:草稿 1:待审核 2:已审核 3:已付款 4:已取消)
        /// </summary>
        public short Status { get; set; } = 0;

        /// <summary>
        /// 申请人ID
        /// </summary>
        [StringLength(36)]
        public string? ApplicantId { get; set; }

        /// <summary>
        /// 申请人名称
        /// </summary>
        [StringLength(50)]
        public string? ApplicantName { get; set; }

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
        /// 付款人ID
        /// </summary>
        [StringLength(36)]
        public string? PayerId { get; set; }

        /// <summary>
        /// 付款人名称
        /// </summary>
        [StringLength(50)]
        public string? PayerName { get; set; }

        /// <summary>
        /// 银行流水号
        /// </summary>
        [StringLength(100)]
        public string? BankSerialNo { get; set; }

        /// <summary>
        /// 是否预付款
        /// </summary>
        public bool IsAdvancePayment { get; set; } = false;

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
        public virtual ICollection<PaymentItem> Items { get; set; } = new List<PaymentItem>();
    }

    /// <summary>
    /// 付款单明细表
    /// </summary>
    [Table("paymentitem")]
    public class PaymentItem : BaseGuidEntity
    {
        /// <summary>
        /// 明细ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("ItemId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 付款单ID (外键)
        /// </summary>
        [Required]
        [StringLength(36)]
        public string PaymentId { get; set; } = string.Empty;

        /// <summary>
        /// 行号
        /// </summary>
        public int LineNo { get; set; } = 1;

        /// <summary>
        /// 采购订单ID
        /// </summary>
        [StringLength(36)]
        public string? PurchaseOrderId { get; set; }

        /// <summary>
        /// 采购订单编号
        /// </summary>
        [StringLength(50)]
        public string? PurchaseOrderCode { get; set; }

        /// <summary>
        /// 本次付款金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal PaymentAmount { get; set; } = 0.00m;

        /// <summary>
        /// 订单总金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal OrderTotalAmount { get; set; } = 0.00m;

        /// <summary>
        /// 已付款金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal PaidAmount { get; set; } = 0.00m;

        /// <summary>
        /// 未付款金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal UnpaidAmount { get; set; } = 0.00m;

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(200)]
        public string? Remark { get; set; }

        // 导航属性
        [ForeignKey("PaymentId")]
        public virtual Payment? Payment { get; set; }
    }

    /// <summary>
    /// 付款类型枚举
    /// </summary>
    public enum PaymentType
    {
        采购付款 = 1,
        费用付款 = 2,
        预付款 = 3,
        其他 = 4
    }

    /// <summary>
    /// 付款方式枚举
    /// </summary>
    public enum PaymentMethod
    {
        银行转账 = 1,
        现金 = 2,
        支票 = 3,
        承兑汇票 = 4,
        信用证 = 5
    }

    /// <summary>
    /// 付款状态枚举
    /// </summary>
    public enum PaymentStatus
    {
        草稿 = 0,
        待审核 = 1,
        已审核 = 2,
        已付款 = 3,
        已取消 = 4
    }
}
