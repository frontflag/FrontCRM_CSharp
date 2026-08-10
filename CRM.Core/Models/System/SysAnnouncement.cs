using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.System;

[Table("sys_announcement")]
public class SysAnnouncement
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(100)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>platform_notice | version_update</summary>
    [Required]
    [StringLength(32)]
    [Column("type")]
    public string Type { get; set; } = SysAnnouncementTypes.PlatformNotice;

    [Required]
    [Column("body_md")]
    public string BodyMd { get; set; } = string.Empty;

    /// <summary>draft | published</summary>
    [Required]
    [StringLength(16)]
    [Column("status")]
    public string Status { get; set; } = SysAnnouncementStatuses.Draft;

    [Column("published_at")]
    public DateTime? PublishedAt { get; set; }

    [StringLength(36)]
    [Column("published_by")]
    public string? PublishedBy { get; set; }

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
}

public static class SysAnnouncementStatuses
{
    public const string Draft = "draft";
    public const string Published = "published";
}

public static class SysAnnouncementTypes
{
    public const string PlatformNotice = "platform_notice";
    public const string VersionUpdate = "version_update";

    public static bool IsValid(string? type) =>
        type == PlatformNotice || type == VersionUpdate;
}

public static class SysAnnouncementLimits
{
    public const int TitleMaxLength = 100;
    public const int BodyMaxBytes = 50 * 1024;
}
