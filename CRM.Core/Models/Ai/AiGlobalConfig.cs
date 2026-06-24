using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Ai;

[Table("ai_global_config")]
public class AiGlobalConfig
{
    [Key]
    [StringLength(64)]
    [Column("config_key")]
    public string ConfigKey { get; set; } = string.Empty;

    [StringLength(500)]
    [Column("config_value")]
    public string ConfigValue { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Column("modify_time")]
    public DateTime? ModifyTime { get; set; }
}
