namespace CRM.Core.Constants;

/// <summary>与员工管理、登录校验一致：0 停用、1 正常、2 冻结（软锁定，不可登录与执行业务）。</summary>
public static class UserAccountStatus
{
    public const short Disabled = 0;
    public const short Active = 1;
    public const short Frozen = 2;
}
