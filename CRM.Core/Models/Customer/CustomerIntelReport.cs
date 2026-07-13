using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Customer;

/// <summary>客户 AI 情报调查报告（全公司共享，最新 + 历史）。</summary>
[Table("customer_intel_report")]
public class CustomerIntelReport
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [StringLength(36)]
    [Column("customer_id")]
    public string? CustomerId { get; set; }

    [Required]
    [StringLength(256)]
    [Column("company_name")]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(64)]
    [Column("credit_code")]
    public string? CreditCode { get; set; }

    [Required]
    [StringLength(64)]
    [Column("query_fingerprint")]
    public string QueryFingerprint { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    [Column("scenario_code")]
    public string ScenarioCode { get; set; } = string.Empty;

    [Column("report_json")]
    public string ReportJson { get; set; } = "{}";

    [StringLength(16)]
    [Column("schema_version")]
    public string SchemaVersion { get; set; } = "1.0";

    [Required]
    [StringLength(16)]
    [Column("source")]
    public string Source { get; set; } = "live";

    [StringLength(36)]
    [Column("invocation_log_id")]
    public string? InvocationLogId { get; set; }

    [Column("is_latest")]
    public bool IsLatest { get; set; } = true;

    [StringLength(36)]
    [Column("created_by")]
    public string? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
