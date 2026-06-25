using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Constants;

namespace CRM.Core.Models.Ai;

[Table("ai_entity_parse_log")]
public class AiEntityParseLog
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [StringLength(36)]
    [Column("invocation_id")]
    public string InvocationId { get; set; } = string.Empty;

    [StringLength(100)]
    [Column("scenario_code")]
    public string ScenarioCode { get; set; } = string.Empty;

    [StringLength(64)]
    [Column("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    [StringLength(36)]
    [Column("user_id")]
    public string? UserId { get; set; }

    [StringLength(64)]
    [Column("parent_biz_type")]
    public string? ParentBizType { get; set; }

    [StringLength(64)]
    [Column("parent_biz_id")]
    public string? ParentBizId { get; set; }

    [Column("raw_text")]
    public string RawText { get; set; } = string.Empty;

    [Column("parse_result_raw")]
    public string? ParseResultRaw { get; set; }

    [Column("parse_result_json")]
    public string ParseResultJson { get; set; } = "{}";

    [Column("confirmed_fields_json")]
    public string? ConfirmedFieldsJson { get; set; }

    [StringLength(20)]
    [Column("outcome")]
    public string Outcome { get; set; } = AiEntityParseOutcomeCode.Parsed;

    [Column("template_version")]
    public int TemplateVersion { get; set; } = 1;

    [StringLength(64)]
    [Column("provider_code")]
    public string ProviderCode { get; set; } = string.Empty;

    [StringLength(100)]
    [Column("model")]
    public string Model { get; set; } = string.Empty;

    [Column("from_cache")]
    public bool FromCache { get; set; }

    [Column("latency_ms")]
    public int LatencyMs { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("confirmed_at")]
    public DateTime? ConfirmedAt { get; set; }

    [StringLength(64)]
    [Column("saved_biz_id")]
    public string? SavedBizId { get; set; }

    [Column("saved_at")]
    public DateTime? SavedAt { get; set; }
}
