namespace CRM.Core.Constants;

/// <summary>提成系数类型（版本头 <c>commission_rate_version.role_type</c>）。</summary>
public enum CommissionRoleType : short
{
    Sales = 1,
    Purchase = 2
}

/// <summary>提成系数版本状态。</summary>
public static class CommissionRateVersionStatus
{
    public const short Draft = 1;
    public const short Active = 2;
    public const short Disabled = 3;

    public static bool IsValid(short value) =>
        value is Draft or Active or Disabled;
}

public static class CommissionLadder
{
    public const int Count = 10;

    public static bool IsRoleType(short value) =>
        value is (short)CommissionRoleType.Sales or (short)CommissionRoleType.Purchase;
}

/// <summary>提成计算全局参数（不跟系数版本）。</summary>
public static class CommissionCalcSettingCode
{
    public const string SeedId = "c2000000-0000-4000-8000-000000000010";
    public const int DelayDaysMin = 0;
    public const int DelayDaysMax = 3650;

    public static bool IsDelayDays(int value) =>
        value is >= DelayDaysMin and <= DelayDaysMax;
}
