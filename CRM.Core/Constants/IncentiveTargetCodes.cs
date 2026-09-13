namespace CRM.Core.Constants;

public static class IncentiveTargetPermissionCodes
{
    public const string Read = "incentive-target.read";
    public const string Write = "incentive-target.write";
}

public static class IncentiveTargetPeriodKinds
{
    public const short Term = 1;
    public const short Year = 2;

    public static bool IsValid(short value) => value is Term or Year;
}

public static class IncentiveTargetLimits
{
    public const decimal MaxUsd = 99_999_999.99m;
    public const int Scale = 2;
}
