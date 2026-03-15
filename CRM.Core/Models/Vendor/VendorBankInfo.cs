using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Vendor
{
    /// <summary>
    /// 供应商银行账户表
    /// </summary>
    [Table("vendorbankinfo")]
    public class VendorBankInfo : BaseGuidEntity
    {
        /// <summary>
        /// 银行账户ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("BankId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 供应商ID (外键)
        /// </summary>
        [Required]
        [StringLength(36)]
        public string VendorId { get; set; } = string.Empty;

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
        [ForeignKey("VendorId")]
        public virtual VendorInfo? Vendor { get; set; }
    }
}
