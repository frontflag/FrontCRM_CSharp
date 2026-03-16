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

        /// <summary>类型：call / visit / quote / order 等</summary>
        [StringLength(50)]
        public string Type { get; set; } = "call";

        [StringLength(500)]
        public string? Content { get; set; }

        public DateTime Time { get; set; } = DateTime.UtcNow;
    }
}
