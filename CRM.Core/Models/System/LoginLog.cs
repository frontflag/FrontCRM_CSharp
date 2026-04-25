using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.System;

[Table("log_login")]
public class LoginLog
{
    [Key]
    [StringLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required, StringLength(36)]
    public string UserId { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string UserName { get; set; } = string.Empty;

    public DateTime LoginAt { get; set; } = DateTime.UtcNow;

    [Required, StringLength(45)]
    public string ClientIp { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Country { get; set; }

    [StringLength(100)]
    public string? Province { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? District { get; set; }

    [StringLength(200)]
    public string? Street { get; set; }

    [StringLength(500)]
    public string? AddressLine { get; set; }

    [StringLength(500)]
    public string? RegionRaw { get; set; }

    public short LoginMethod { get; set; }

    [StringLength(36)]
    public string? ActorUserId { get; set; }

    [Required, StringLength(32)]
    public string GeoSource { get; set; } = "none";
}
