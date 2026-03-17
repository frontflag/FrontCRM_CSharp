using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Models;

namespace CRM.Core.Models.Customer
{
    /// <summary>
    /// 客户联系历史记录（如电话、拜访、报价、订单等）
    /// </summary>
    [Table("customercontacthistory")]
    public class CustomerContactHistory : BaseGuidEntity
    {
        [StringLength(36)]
        [Column("HistoryId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(36)]
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>类型：call / visit / email / meeting / other</summary>
        [StringLength(50)]
        public string Type { get; set; } = "call";

        /// <summary>主题</summary>
        [StringLength(200)]
        public string? Subject { get; set; }

        /// <summary>联系内容</summary>
        [StringLength(2000)]
        public string? Content { get; set; }

        /// <summary>联系人（客户方）</summary>
        [StringLength(100)]
        public string? ContactPerson { get; set; }

        /// <summary>联系时间</summary>
        public DateTime Time { get; set; } = DateTime.UtcNow;

        /// <summary>下次跟进时间</summary>
        public DateTime? NextFollowUpTime { get; set; }

        /// <summary>联系结果</summary>
        [StringLength(500)]
        public string? Result { get; set; }

        /// <summary>操作人ID</summary>
        [StringLength(36)]
        public string? OperatorId { get; set; }
    }
}
