using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

        public int SortOrder { get; set; }

        /// <summary>为 true 时视为禁用，不在业务下拉中使用。</summary>
        public bool IsDisabled { get; set; }
    }
}
