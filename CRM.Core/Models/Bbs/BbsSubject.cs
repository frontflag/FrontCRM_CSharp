using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Bbs;

[Table("bbs_subject")]
public class BbsSubject
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("type")]
    public int Type { get; set; }

    [Column("status")]
    public int Status { get; set; } = 1;

    [Column("is_top")]
    public bool IsTop { get; set; }

    [Column("is_hot")]
    public bool IsHot { get; set; }

    [Column("anonymous")]
    public bool Anonymous { get; set; }

    [Column("view_count")]
    public int ViewCount { get; set; }

    [Column("reply_count")]
    public int ReplyCount { get; set; }

    [Column("like_count")]
    public int LikeCount { get; set; }

    [Column("dislike_count")]
    public int DislikeCount { get; set; }

    [Column("last_reply_time")]
    public DateTime? LastReplyTime { get; set; }

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
