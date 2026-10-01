using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Bbs;

/// <summary>板块设置：自定义名称 + 版主（每类型一行；版主可空）。</summary>
[Table("bbs_board_moderator")]
public class BbsBoardModerator
{
    [Key]
    [Column("subject_type")]
    public int SubjectType { get; set; }

    [StringLength(36)]
    [Column("user_id")]
    public string? UserId { get; set; }

    /// <summary>自定义板块名称；空则用系统默认文案。</summary>
    [StringLength(50)]
    [Column("display_name")]
    public string? DisplayName { get; set; }

    /// <summary>软删除：无帖时可删；删除后侧栏与发帖类型中隐藏。</summary>
    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    /// <summary>可配置板块的显示顺序（越小越靠前）；固定板块忽略。</summary>
    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("update_time")]
    public DateTime UpdateTime { get; set; } = DateTime.UtcNow;

    [StringLength(36)]
    [Column("update_by")]
    public string? UpdateBy { get; set; }
}
