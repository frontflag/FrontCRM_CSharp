using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Ai;

[Table("ai_invocation_cache")]
public class AiInvocationCache
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [StringLength(64)]
    [Column("cache_key")]
    public string CacheKey { get; set; } = string.Empty;

    [StringLength(100)]
    [Column("scenario_code")]
    public string ScenarioCode { get; set; } = string.Empty;

    [Column("request_fingerprint")]
    public string RequestFingerprintJson { get; set; } = "{}";

    [Column("response_content")]
    public string ResponseContent { get; set; } = string.Empty;

    [Column("response_json")]
    public string? ResponseJson { get; set; }

    [StringLength(64)]
    [Column("provider_code")]
    public string ProviderCode { get; set; } = string.Empty;

    [StringLength(100)]
    [Column("model")]
    public string Model { get; set; } = string.Empty;

    [Column("template_version")]
    public int TemplateVersion { get; set; } = 1;

    [Column("hit_count")]
    public int HitCount { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }
}
