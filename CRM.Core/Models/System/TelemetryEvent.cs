using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.System;

[Table("telemetry_event")]
public class TelemetryEvent
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [StringLength(36)]
    [Column("event_id")]
    public string EventId { get; set; } = string.Empty;

    [StringLength(32)]
    [Column("event_type")]
    public string EventType { get; set; } = string.Empty;

    [StringLength(64)]
    [Column("event_name")]
    public string EventName { get; set; } = string.Empty;

    [Column("occurred_at")]
    public DateTime OccurredAt { get; set; }

    [Column("received_at")]
    public DateTime ReceivedAt { get; set; }

    [StringLength(36)]
    [Column("session_id")]
    public string? SessionId { get; set; }

    [StringLength(36)]
    [Column("user_id")]
    public string? UserId { get; set; }

    [StringLength(50)]
    [Column("user_name")]
    public string? UserName { get; set; }

    [StringLength(200)]
    [Column("page_key")]
    public string? PageKey { get; set; }

    [StringLength(500)]
    [Column("route_path")]
    public string? RoutePath { get; set; }

    [StringLength(80)]
    [Column("browser")]
    public string? Browser { get; set; }

    [StringLength(80)]
    [Column("os")]
    public string? Os { get; set; }

    [StringLength(40)]
    [Column("device_type")]
    public string? DeviceType { get; set; }

    [Column("screen_w")]
    public int? ScreenW { get; set; }

    [Column("screen_h")]
    public int? ScreenH { get; set; }

    [StringLength(500)]
    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("payload_json")]
    public string? PayloadJson { get; set; }
}
