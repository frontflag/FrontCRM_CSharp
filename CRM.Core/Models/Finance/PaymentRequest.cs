using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Finance
{
    /// <summary>
    /// 付款申请单
    /// </summary>
    [Table("paymentrequest")]
    public class PaymentRequest : BaseGuidEntity
    {
        /// <summary>
        /// 申请单号
        /// </summary>
        [StringLength(50)]
        public string RequestCode { get; set; } = string.Empty;

        /// <summary>
        /// 采购订单ID
        /// </summary>
        [StringLength(36)]
        public string? PurchaseOrderId { get; set; }

        /// <summary>
        /// 供应商ID
        /// </summary>
        [StringLength(36)]
        public string VendorId { get; set; } = string.Empty;

        /// <summary>
        /// 申请人ID
        /// </summary>
        [StringLength(36)]
        public string RequestUserId { get; set; } = string.Empty;

        /// <summary>
        /// 申请日期
        /// </summary>
        public DateTime RequestDate { get; set; }

        /// <summary>
        /// 金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// 币种
        /// </summary>
        public short Currency { get; set; } = 1;

        /// <summary>
        /// 付款方式
        /// </summary>
        public short PaymentMethod { get; set; }

        /// <summary>
        /// 银行账号
        /// </summary>
        [StringLength(50)]
        public string? BankAccount { get; set; }

        /// <summary>
        /// 银行名称
        /// </summary>
        [StringLength(100)]
        public string? BankName { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        /// <summary>
        /// 状态 (0:待审批 1:已审批 2:已付款 3:已拒绝)
        /// </summary>
        public short Status { get; set; } = 0;
    }
}
