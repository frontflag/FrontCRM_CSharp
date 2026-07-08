using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.Customs;

/// <summary>
/// 采购报关系数（全局配置，对齐 EBS PurchaseCostParam）。
/// </summary>
[Table("purchase_cost_param")]
public class PurchaseCostParam : BaseGuidEntity, ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("ratio", TypeName = "numeric(10,4)")]
    public decimal Ratio { get; set; } = 1m;

    [Column("start_time")]
    public DateTime StartTime { get; set; }

    [StringLength(500)]
    [Column("remark")]
    public string? Remark { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [StringLength(36)]
    [Column("create_by_user_id")]
    public string? CreateByUserId { get; set; }

    [StringLength(36)]
    [Column("modify_by_user_id")]
    public string? ModifyByUserId { get; set; }
}

[Table("purchase_cost_param_change_log")]
public class PurchaseCostParamChangeLog : BaseGuidEntity
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [StringLength(36)]
    [Column("purchase_cost_param_id")]
    public string? PurchaseCostParamId { get; set; }

    [Column("ratio", TypeName = "numeric(10,4)")]
    public decimal Ratio { get; set; }

    [Column("start_time")]
    public DateTime StartTime { get; set; }

    [StringLength(36)]
    [Column("change_user_id")]
    public string? ChangeUserId { get; set; }

    [StringLength(100)]
    [Column("change_user_name")]
    public string? ChangeUserName { get; set; }

    [StringLength(500)]
    [Column("change_summary")]
    public string? ChangeSummary { get; set; }
}
