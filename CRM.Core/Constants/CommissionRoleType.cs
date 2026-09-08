namespace CRM.Core.Constants;

/// <summary>提成系数行类型（<c>commission_rate.role_type</c>）。</summary>
public enum CommissionRoleType : short
{
    Sales = 1,
    Purchase = 2
}

public static class CommissionLadder
{
    public const int Count = 10;

    public static bool IsRoleType(short value) =>
        value is (short)CommissionRoleType.Sales or (short)CommissionRoleType.Purchase;
}
