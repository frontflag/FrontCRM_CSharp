using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.System;

/// <summary>
/// 用户等级变更履历（只追加）。当前等级在 <see cref="User.Level"/>；本表供回溯与以后提成按日取级。
/// </summary>
[Table("user_level_history")]
public class UserLevelHistory : BaseGuidEntity
{
    [Column("UserLevelHistoryId")]
    [StringLength(36)]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>被改等级的员工 Id。</summary>
    [Required]
    [StringLength(36)]
    [Column("UserId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>变更当时该员工的登录账号快照（账号以后改名仍可读）。</summary>
    [Required]
    [StringLength(50)]
    [Column("UserName")]
    public string UserName { get; set; } = string.Empty;

    [Column("OldLevel")]
    public short OldLevel { get; set; }

    [Column("NewLevel")]
    public short NewLevel { get; set; }

    [StringLength(200)]
    [Column("Remark")]
    public string? Remark { get; set; }

    /// <summary>新等级开始时刻（UTC）。</summary>
    [Column("ChangeTime")]
    public DateTime ChangeTime { get; set; }

    [StringLength(36)]
    [Column("OperatorUserId")]
    public string? OperatorUserId { get; set; }

    [StringLength(50)]
    [Column("OperatorUserName")]
    public string? OperatorUserName { get; set; }
}
