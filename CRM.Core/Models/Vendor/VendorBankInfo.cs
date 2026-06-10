using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.Vendor
{
    /// <summary>
    /// 供应商银行账户表
    /// </summary>
    [Table("vendorbankinfo")]
    public class VendorBankInfo : BaseGuidEntity, ISoftDeletable
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
        /// 财务参数-付款银行主键（financepaymentbank）；业务下拉存 Id，<see cref="BankName"/> 冗余展示名。
        /// </summary>
        [StringLength(36)]
        [Column("FinancePaymentBankId")]
        public string? FinancePaymentBankId { get; set; }

        /// <summary>
        /// 银行名称（仅服务端根据 <see cref="FinancePaymentBankId"/> 从 financepaymentbank 回填的展示冗余，勿作为业务入参）。
        /// </summary>
        [Obsolete("请使用 FinancePaymentBankId；BankName 仅由服务端按付款银行主数据同步。")]
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
        /// 银行地址
        /// </summary>
        [StringLength(500)]
        public string? BankAddress { get; set; }

        /// <summary>
        /// 国际银行代码 SWIFT
        /// </summary>
        [StringLength(64)]
        public string? Swift { get; set; }

        /// <summary>
        /// 国际银行账号 IBAN
        /// </summary>
        [StringLength(64)]
        public string? Iban { get; set; }

        /// <summary>
        /// 联行号 / 银行号
        /// </summary>
        [StringLength(32)]
        public string? BankCode { get; set; }

        /// <summary>
        /// 所在国家
        /// </summary>
        [StringLength(100)]
        public string? Country { get; set; }

        /// <summary>
        /// 账户类型：rmb / foreign
        /// </summary>
        [StringLength(32)]
        public string AccountType { get; set; } = "rmb";

        /// <summary>
        /// 用途：payment / receipt
        /// </summary>
        [StringLength(32)]
        public string PurposeType { get; set; } = "payment";

        /// <summary>
        /// 币别
        /// </summary>
        public short? Currency { get; set; }

        /// <summary>
        /// 是否默认账户
        /// </summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        // 导航属性
        [ForeignKey("VendorId")]
        [JsonIgnore]
        public virtual VendorInfo? Vendor { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; }
    }
}
