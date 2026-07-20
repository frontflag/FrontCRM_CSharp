using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.System;

[Table("telemetry_daily_api")]
public class TelemetryDailyApi
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("stat_date")]
    public DateOnly StatDate { get; set; }

    [StringLength(16)]
    [Column("method")]
    public string Method { get; set; } = "GET";

    [StringLength(300)]
    [Column("path_template")]
    public string PathTemplate { get; set; } = string.Empty;

    [Column("call_count")]
    public long CallCount { get; set; }

    [Column("fail_count")]
    public long FailCount { get; set; }

    [Column("duration_ms_sum")]
    public long DurationMsSum { get; set; }

    [Column("duration_ms_max")]
    public int DurationMsMax { get; set; }
}
