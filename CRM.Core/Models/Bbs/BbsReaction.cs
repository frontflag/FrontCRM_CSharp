using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Bbs;

/// <summary>论坛赞/踩记录；同一用户对同一目标仅一条。</summary>
[Table("bbs_reaction")]
public class BbsReaction
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>1=主题 2=回复</summary>
    [Column("target_type")]
    public int TargetType { get; set; }

    [Required]
    [StringLength(36)]
    [Column("target_id")]
    public string TargetId { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>1=赞 -1=踩</summary>
    [Column("value")]
    public int Value { get; set; }

    [Column("create_time")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [Column("modify_time")]
    public DateTime? ModifyTime { get; set; }
}
