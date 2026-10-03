using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Bbs;

/// <summary>桌面系统通告里，用户已点开的论坛「系统更新」帖。</summary>
[Table("bbs_dashboard_notice_dismiss")]
public class BbsDashboardNoticeDismiss
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
    [StringLength(36)]
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("dismissed_at")]
    public DateTime DismissedAt { get; set; } = DateTime.UtcNow;
}
