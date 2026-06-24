using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Interfaces;
using CRM.Core.Models;

namespace CRM.Core.Models.Ai;

[Table("ai_prompt_template")]
public class AiPromptTemplate : BaseGuidEntity, ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [StringLength(100)]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [Column("version")]
    public int Version { get; set; } = 1;

    [Column("system_prompt")]
    public string SystemPrompt { get; set; } = string.Empty;

    [Column("user_prompt_template")]
    public string UserPromptTemplate { get; set; } = string.Empty;

    [StringLength(20)]
    [Column("output_format")]
    public string OutputFormat { get; set; } = "json";

    [Column("json_schema_hint")]
    public string? JsonSchemaHint { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}
