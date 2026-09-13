using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.Finance;

[Table("incentive_target")]
public class IncentiveTarget : ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [StringLength(36)]
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("role_type")]
    public short RoleType { get; set; }

    [Column("period_kind")]
    public short PeriodKind { get; set; }

    [StringLength(8)]
    [Column("period_key")]
    public string PeriodKey { get; set; } = string.Empty;

    [Column("target_amount_usd", TypeName = "numeric(18,2)")]
    public decimal TargetAmountUsd { get; set; }

    [Column("create_time")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [Column("modify_time")]
    public DateTime ModifyTime { get; set; } = DateTime.UtcNow;

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}
