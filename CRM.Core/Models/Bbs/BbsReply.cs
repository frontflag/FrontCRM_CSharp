using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Bbs;

[Table("bbs_reply")]
public class BbsReply
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    [Column("subject_id")]
    public string SubjectId { get; set; } = string.Empty;

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("anonymous")]
    public bool Anonymous { get; set; }

    [Column("create_time")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [StringLength(36)]
    [Column("create_by")]
    public string? CreateBy { get; set; }

    [Column("modify_time")]
    public DateTime? ModifyTime { get; set; }

    [StringLength(36)]
    [Column("modify_by")]
    public string? ModifyBy { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}
