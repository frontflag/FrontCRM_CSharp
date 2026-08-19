namespace CRM.Core.Constants;

/// <summary>员工用户等级（<c>user.Level</c> / <c>user_level_history</c>）。</summary>
public static class UserLevelCode
{
    public const short Min = 1;
    public const short Max = 20;
    public const short Default = 1;

    public static bool IsValid(short value) => value is >= Min and <= Max;
}
