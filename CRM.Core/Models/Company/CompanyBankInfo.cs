using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Company
{
    /// <summary>公司银行信息（公司信息页多组维护）。</summary>
    [Table("company_bankinfo")]
    public class CompanyBankInfo : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("Id")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        [StringLength(200)]
        [Column("bank_name")]
        public string BankName { get; set; } = string.Empty;

        [StringLength(500)]
        [Column("bank_address")]
        public string BankAddress { get; set; } = string.Empty;

        [StringLength(64)]
        [Column("swift")]
        public string Swift { get; set; } = string.Empty;

        /// <summary>银行号 / 联行号。</summary>
        [StringLength(32)]
        [Column("bank_code")]
        public string BankCode { get; set; } = string.Empty;

        /// <summary>账户类型：rmb / foreign。</summary>
        [StringLength(32)]
        [Column("account_type")]
        public string AccountType { get; set; } = "rmb";

        [StringLength(500)]
        [Column("remark")]
        public string Remark { get; set; } = string.Empty;

        [StringLength(200)]
        [Column("account_name")]
        public string AccountName { get; set; } = string.Empty;

        [StringLength(64)]
        [Column("account_number")]
        public string AccountNumber { get; set; } = string.Empty;

        [StringLength(16)]
        [Column("currency")]
        public string Currency { get; set; } = "RMB";

        [StringLength(100)]
        [Column("country")]
        public string Country { get; set; } = string.Empty;

        [StringLength(64)]
        [Column("iban")]
        public string Iban { get; set; } = string.Empty;

        /// <summary>用途：payment / receipt。</summary>
        [StringLength(32)]
        [Column("purpose_type")]
        public string PurposeType { get; set; } = "payment";

        [Column("is_default")]
        public bool IsDefault { get; set; }

        [Column("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>可用付款：勾选后出现在付款单「付款银行」下拉。</summary>
        [Column("available_for_payment")]
        public bool AvailableForPayment { get; set; }

        [Column("sort_order")]
        public int SortOrder { get; set; }
    }
}
