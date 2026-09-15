using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Constants;

namespace CRM.Core.Models.Customer;

[Table("customer_news_briefing")]
public class CustomerNewsBriefing
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("D");

    [Required]
    [StringLength(36)]
    [Column("customer_id")]
    public string CustomerId { get; set; } = string.Empty;

    [Column("briefing_date")]
    public DateOnly BriefingDate { get; set; }

    [Column("period_start")]
    public DateOnly PeriodStart { get; set; }

    [Column("period_end")]
    public DateOnly PeriodEnd { get; set; }

    [Column("markdown")]
    public string Markdown { get; set; } = string.Empty;

    [StringLength(20)]
    [Column("status")]
    public string Status { get; set; } = CustomerNewsCodes.StatusFailed;

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [StringLength(36)]
    [Column("invocation_id")]
    public string? InvocationId { get; set; }

    [StringLength(36)]
    [Column("requested_by")]
    public string? RequestedBy { get; set; }

    [Column("generated_at")]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    [Column("create_time")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [Column("modify_time")]
    public DateTime ModifyTime { get; set; } = DateTime.UtcNow;
}
