using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Ai;

[Table("ai_invocation_log")]
public class AiInvocationLog
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [StringLength(100)]
    [Column("scenario_code")]
    public string ScenarioCode { get; set; } = string.Empty;

    [StringLength(64)]
    [Column("provider_code")]
    public string ProviderCode { get; set; } = string.Empty;

    [StringLength(100)]
    [Column("model")]
    public string Model { get; set; } = string.Empty;

    [Column("template_version")]
    public int TemplateVersion { get; set; } = 1;

    [StringLength(36)]
    [Column("user_id")]
    public string? UserId { get; set; }

    [StringLength(64)]
    [Column("biz_type")]
    public string? BizType { get; set; }

    [StringLength(64)]
    [Column("biz_id")]
    public string? BizId { get; set; }

    [Column("request_fingerprint")]
    public string RequestFingerprintJson { get; set; } = "{}";

    [StringLength(64)]
    [Column("prompt_hash")]
    public string PromptHash { get; set; } = string.Empty;

    [StringLength(200)]
    [Column("prompt_preview")]
    public string? PromptPreview { get; set; }

    [StringLength(20)]
    [Column("status")]
    public string Status { get; set; } = string.Empty;

    [StringLength(20)]
    [Column("trigger_type")]
    public string? TriggerType { get; set; }

    [Column("from_cache")]
    public bool FromCache { get; set; }

    [Column("latency_ms")]
    public int LatencyMs { get; set; }

    [StringLength(1000)]
    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("prompt_tokens")]
    public int? PromptTokens { get; set; }

    [Column("completion_tokens")]
    public int? CompletionTokens { get; set; }

    [Column("total_tokens")]
    public int? TotalTokens { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
