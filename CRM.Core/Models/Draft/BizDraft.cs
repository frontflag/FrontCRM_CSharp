using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Draft
{
    [Table("biz_draft")]
    public class BizDraft : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("DraftId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public long UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string EntityType { get; set; } = string.Empty;

        [StringLength(200)]
        public string? DraftName { get; set; }

        [Required]
        [Column(TypeName = "text")]
        public string PayloadJson { get; set; } = "{}";

        /// <summary>
        /// 0=草稿,1=已转正式,2=已废弃
        /// </summary>
        public short Status { get; set; } = 0;

        [StringLength(500)]
        public string? Remark { get; set; }

        [StringLength(36)]
        public string? ConvertedEntityId { get; set; }

        public DateTime? ConvertedAt { get; set; }
    }
}
