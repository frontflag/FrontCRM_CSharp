using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.System;

[Table("telemetry_daily_page")]
public class TelemetryDailyPage
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("stat_date")]
    public DateOnly StatDate { get; set; }

    [StringLength(200)]
    [Column("page_key")]
    public string PageKey { get; set; } = string.Empty;

    [Column("view_count")]
    public long ViewCount { get; set; }

    [Column("visible_ms_sum")]
    public long VisibleMsSum { get; set; }

    [Column("active_ms_sum")]
    public long ActiveMsSum { get; set; }
}
