using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Models;

namespace CRM.Core.Models.Finance
{
    /// <summary>
    /// 财务汇率设置（全局单行，Id 固定）。
    /// 含义：1 USD 可兑换的各币种数量（如 USD/CNY=6.9194 表示 1 美元 = 6.9194 人民币）。
    /// </summary>
    [Table("financeexchangeratesetting")]
    public class FinanceExchangeRateSetting : BaseGuidEntity
    {
        public const string SingletonId = "00000000-0000-4000-8000-0000000000E1";

        [Key]
        [StringLength(36)]
        [Column("FinanceExchangeRateSettingId")]
        public override string Id { get; set; } = SingletonId;

        [Column(TypeName = "numeric(12,4)")]
        public decimal UsdToCny { get; set; }

        [Column(TypeName = "numeric(12,4)")]
        public decimal UsdToHkd { get; set; }

        [Column(TypeName = "numeric(12,4)")]
        public decimal UsdToEur { get; set; }

        /// <summary>最后保存人（用户主键 GUID 字符串，与 BaseEntity.ModifyUserId long 区分）</summary>
        [StringLength(36)]
        public string? EditorUserId { get; set; }

        [StringLength(100)]
        public string? EditorUserName { get; set; }
    }

    /// <summary>
    /// 汇率修改日志（每次保存成功追加一行）。
    /// </summary>
    [Table("financeexchangeratechangelog")]
    public class FinanceExchangeRateChangeLog : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("FinanceExchangeRateChangeLogId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        [Column(TypeName = "numeric(12,4)")]
        public decimal UsdToCny { get; set; }

        [Column(TypeName = "numeric(12,4)")]
        public decimal UsdToHkd { get; set; }

        [Column(TypeName = "numeric(12,4)")]
        public decimal UsdToEur { get; set; }

        [StringLength(36)]
        public string? ChangeUserId { get; set; }

        [StringLength(100)]
        public string? ChangeUserName { get; set; }

        /// <summary>便于列表直接展示的摘要，如：人民币=6.9194；港币=7.8367；欧元=0.8725</summary>
        [StringLength(500)]
        public string? ChangeSummary { get; set; }
    }
}
