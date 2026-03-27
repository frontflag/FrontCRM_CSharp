using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.System
{
    [Table("approval_record")]
    public class ApprovalRecord : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("Id")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        [Required, StringLength(50)]
        public string BizType { get; set; } = string.Empty;

        [Required, StringLength(36)]
        public string BusinessId { get; set; } = string.Empty;

        [StringLength(64)]
        public string? DocumentCode { get; set; }

        /// <summary>事项描述（业务摘要）</summary>
        [StringLength(1000)]
        public string? ItemDescription { get; set; }

        // submit | approve | reject
        [Required, StringLength(20)]
        public string ActionType { get; set; } = "submit";

        public short? FromStatus { get; set; }
        public short? ToStatus { get; set; }

        [StringLength(500)]
        public string? SubmitRemark { get; set; }

        [StringLength(500)]
        public string? AuditRemark { get; set; }

        [StringLength(36)]
        public string? SubmitterUserId { get; set; }

        [StringLength(100)]
        public string? SubmitterUserName { get; set; }

        [StringLength(36)]
        public string? ApproverUserId { get; set; }

        [StringLength(100)]
        public string? ApproverUserName { get; set; }

        public DateTime ActionTime { get; set; } = DateTime.UtcNow;
    }
}

