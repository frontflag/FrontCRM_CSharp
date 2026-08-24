using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.System;

/// <summary>点对点系统通知（发送即落库，无草稿）。</summary>
[Table("sys_user_notice")]
public class SysUserNotice
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    [Column("recipient_user_id")]
    public string RecipientUserId { get; set; } = string.Empty;

    [Column("is_urgent")]
    public bool IsUrgent { get; set; }

    [Required]
    [StringLength(100)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    [Column("body")]
    public string Body { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    [Column("sender_user_id")]
    public string SenderUserId { get; set; } = string.Empty;

    [Column("create_time")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [Column("read_at")]
    public DateTime? ReadAt { get; set; }
}

public static class SysUserNoticeLimits
{
    public const int TitleMaxLength = 100;
    public const int BodyMaxLength = 4000;
    public const int MaxImageCount = 9;
    public const int MaxImageSizeMb = 8;

    public static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
}
