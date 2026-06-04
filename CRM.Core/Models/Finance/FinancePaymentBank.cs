using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Constants;
using CRM.Core.Models;

namespace CRM.Core.Models.Finance
{
    /// <summary>
    /// 财务参数：付款银行（维护名称、排序、是否禁用）。
    /// </summary>
    [Table("financepaymentbank")]
    public class FinancePaymentBank : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("FinancePaymentBankId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(200)]
        public string BankName { get; set; } = string.Empty;

        /// <summary>银行简称。</summary>
        [StringLength(100)]
        [Column("ShortName")]
        public string? ShortName { get; set; }

        /// <summary>银行英文名称。</summary>
        [StringLength(200)]
        [Column("EBankName")]
        public string? EBankName { get; set; }

        /// <summary>币别类型：<see cref="FinancePaymentBankCurrencyType"/>（10=人民币银行，20=外币银行）。</summary>
        [Column("CurrencyType")]
        public int CurrencyType { get; set; } = FinancePaymentBankCurrencyType.Cny;

        public int SortOrder { get; set; }

        /// <summary>为 true 时视为禁用，不在业务下拉中使用。</summary>
        public bool IsDisabled { get; set; }
    }
}
