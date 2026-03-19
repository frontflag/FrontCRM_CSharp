using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Tag
{
    /// <summary>
    /// 标签关联表
    /// </summary>
    [Table("tag_relation")]
    public class TagRelation : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("RelationId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 标签ID
        /// </summary>
        [Required]
        [StringLength(36)]
        public string TagId { get; set; } = string.Empty;

        /// <summary>
        /// 实体类型（CUSTOMER/VENDOR/SALES_ORDER/...）
        /// </summary>
        [Required]
        [StringLength(50)]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// 实体主键ID
        /// </summary>
        [Required]
        [StringLength(36)]
        public string EntityId { get; set; } = string.Empty;

        /// <summary>
        /// 打标签人
        /// </summary>
        public long AppliedByUserId { get; set; }

        /// <summary>
        /// 打标签时间
        /// </summary>
        public DateTime AppliedTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 标签来源（Manual/Batch/System）
        /// </summary>
        [StringLength(20)]
        public string? Source { get; set; }
    }
}
