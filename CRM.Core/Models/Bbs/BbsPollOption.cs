using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Bbs;

[Table("bbs_poll_option")]
public class BbsPollOption
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    [Column("subject_id")]
    public string SubjectId { get; set; } = string.Empty;

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Required]
    [StringLength(100)]
    [Column("text")]
    public string Text { get; set; } = string.Empty;

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}
