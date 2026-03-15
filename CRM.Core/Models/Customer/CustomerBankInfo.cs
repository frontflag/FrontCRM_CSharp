using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Customer
{
    /// <summary>
    /// 客户银行账户表
    /// </summary>
    [Table("customerbankinfo")]
    public class CustomerBankInfo : BaseGuidEntity
    {
        /// <summary>
        /// 银行账户ID (主键)
        /// </summary>
        [StringLength(36)]
        [Column("BankId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 客户ID (外键)
        /// </summary>
        [Required]
        [StringLength(36)]
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>
        /// 银行名称
        /// </summary>
        [StringLength(100)]
        public string? BankName { get; set; }

        /// <summary>
        /// 银行账号
        /// </summary>
        [StringLength(50)]
        public string? BankAccount { get; set; }

        /// <summary>
        /// 账户名称
        /// </summary>
        [StringLength(50)]
        public string? AccountName { get; set; }

        /// <summary>
        /// 银行支行
        /// </summary>
        [StringLength(100)]
        public string? BankBranch { get; set; }

        /// <summary>
        /// 币别
        /// </summary>
        public short? Currency { get; set; }

        /// <summary>
        /// 是否默认账户
        /// </summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        // 导航属性
        [ForeignKey("CustomerId")]
        public virtual CustomerInfo? Customer { get; set; }
    }
}
