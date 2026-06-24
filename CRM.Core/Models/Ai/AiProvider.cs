using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Interfaces;
using CRM.Core.Models;

namespace CRM.Core.Models.Ai;

[Table("ai_provider")]
public class AiProvider : BaseGuidEntity, ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [StringLength(64)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [StringLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Column("base_url")]
    public string BaseUrl { get; set; } = string.Empty;

    [StringLength(128)]
    [Column("api_key_env")]
    public string? ApiKeyEnv { get; set; }

    [StringLength(100)]
    [Column("default_model")]
    public string DefaultModel { get; set; } = string.Empty;

    [Column("timeout_seconds")]
    public int TimeoutSeconds { get; set; } = 120;

    [Column("is_enabled")]
    public bool IsEnabled { get; set; } = true;

    [Column("extra_headers")]
    public string? ExtraHeadersJson { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}
