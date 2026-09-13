using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Constants;

namespace CRM.Core.Models.System;

[Table("industry_news_briefing")]
public class IndustryNewsBriefing
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("D");

    [Column("briefing_date")]
    public DateOnly BriefingDate { get; set; }

    [Column("period_start")]
    public DateOnly PeriodStart { get; set; }

    [Column("period_end")]
    public DateOnly PeriodEnd { get; set; }

    [Column("items_json", TypeName = "jsonb")]
    public string ItemsJson { get; set; } = "[]";

    [Column("markdown")]
    public string Markdown { get; set; } = string.Empty;

    [StringLength(20)]
    [Column("status")]
    public string Status { get; set; } = IndustryNewsCodes.StatusFailed;

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [StringLength(36)]
    [Column("invocation_id")]
    public string? InvocationId { get; set; }

    [Column("generated_at")]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    [Column("create_time")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [Column("modify_time")]
    public DateTime ModifyTime { get; set; } = DateTime.UtcNow;
}
