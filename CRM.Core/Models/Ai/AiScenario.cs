using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Interfaces;
using CRM.Core.Models;

namespace CRM.Core.Models.Ai;

[Table("ai_scenario")]
public class AiScenario : BaseGuidEntity, ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [StringLength(100)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [StringLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [StringLength(64)]
    [Column("provider_code")]
    public string ProviderCode { get; set; } = string.Empty;

    [StringLength(100)]
    [Column("model")]
    public string Model { get; set; } = string.Empty;

    [StringLength(36)]
    [Column("prompt_template_id")]
    public string PromptTemplateId { get; set; } = string.Empty;

    [Column("cache_ttl_seconds")]
    public int CacheTtlSeconds { get; set; }

    [Column("cache_key_fields")]
    public string CacheKeyFieldsJson { get; set; } = "[]";

    [Column("allowed_input_fields")]
    public string AllowedInputFieldsJson { get; set; } = "[]";

    [Column("max_tokens")]
    public int MaxTokens { get; set; } = 2048;

    [Column("temperature")]
    public decimal Temperature { get; set; } = 0.30m;

    [StringLength(100)]
    [Column("permission_code")]
    public string PermissionCode { get; set; } = string.Empty;

    [Column("rate_limit_per_user_per_min")]
    public int RateLimitPerUserPerMin { get; set; } = 10;

    [Column("is_enabled")]
    public bool IsEnabled { get; set; } = true;

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}
