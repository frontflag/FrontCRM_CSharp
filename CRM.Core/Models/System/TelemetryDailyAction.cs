using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.System;

[Table("telemetry_daily_action")]
public class TelemetryDailyAction
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("stat_date")]
    public DateOnly StatDate { get; set; }

    [StringLength(200)]
    [Column("page_key")]
    public string PageKey { get; set; } = string.Empty;

    [StringLength(200)]
    [Column("action_id")]
    public string ActionId { get; set; } = string.Empty;

    [Column("click_count")]
    public long ClickCount { get; set; }
}
