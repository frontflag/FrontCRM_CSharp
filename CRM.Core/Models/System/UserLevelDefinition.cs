using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.System;

/// <summary>用户等级主数据（1～20 固定行，仅维护说明）。</summary>
[Table("user_level_def")]
public class UserLevelDefinition : BaseGuidEntity
{
    [Column("UserLevelDefId")]
    [StringLength(36)]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("UserLevel")]
    public short UserLevel { get; set; }

    [StringLength(200)]
    [Column("Description")]
    public string? Description { get; set; }
}
