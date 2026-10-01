namespace CRM.Core.Models.Bbs;

/// <summary>论坛操作者：全局版主或按板块版主。</summary>
public sealed class BbsActorContext
{
    public required string UserId { get; init; }

    /// <summary>是否系统管理员（SYS_ADMIN）。</summary>
    public bool IsSysAdmin { get; init; }

    /// <summary>SYS_ADMIN / SYS_MANAGER / bbs.moderate，可管全部板块。</summary>
    public bool IsGlobalModerator { get; init; }

    /// <summary>当前用户担任版主的板块类型集合。</summary>
    public IReadOnlySet<int> ModeratedTypes { get; init; } = new HashSet<int>();

    public bool CanModerateType(int type) =>
        IsGlobalModerator || ModeratedTypes.Contains(type);
}
