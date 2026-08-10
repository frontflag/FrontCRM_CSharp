using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.System;

[Table("sys_announcement_read")]
public class SysAnnouncementRead
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    [Column("announcement_id")]
    public string AnnouncementId { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("read_at")]
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;
}
