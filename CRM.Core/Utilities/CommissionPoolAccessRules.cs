using CRM.Core.Interfaces;

namespace CRM.Core.Utilities;

/// <summary>
/// 提成池入口与行范围：管理员看全部；业务员只看本人业务员行；采购员只看本人采购员行。无独立权限码。
/// </summary>
public static class CommissionPoolAccessRules
{
    public static bool CanEnter(UserPermissionSummaryDto? summary)
    {
        if (summary == null) return false;
        if (SeesAll(summary)) return true;
        return IsSalesperson(summary) || IsPurchaser(summary);
    }

    public static bool SeesAll(UserPermissionSummaryDto summary) =>
        summary.CanForceDelete || summary.HasBizDataBypass;

    public static bool IsSalesperson(UserPermissionSummaryDto summary) =>
        summary.IdentityType == 1;

    public static bool IsPurchaser(UserPermissionSummaryDto summary) =>
        summary.IdentityType is 2 or 3 || summary.BelongsToPurchaseDept;

    /// <summary>
    /// 非全量时返回须匹配的业务员/采购员用户 ID；两侧都有时按「或」裁行。
    /// </summary>
    public static CommissionPoolRowScope ResolveRowScope(UserPermissionSummaryDto summary)
    {
        if (SeesAll(summary))
            return new CommissionPoolRowScope(null, null, false);

        var salesId = IsSalesperson(summary) ? NormalizeId(summary.UserId) : null;
        var purchaseId = IsPurchaser(summary) ? NormalizeId(summary.UserId) : null;
        return new CommissionPoolRowScope(
            salesId,
            purchaseId,
            salesId != null && purchaseId != null);
    }

    static string? NormalizeId(string? userId)
    {
        var t = userId?.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }
}

public readonly record struct CommissionPoolRowScope(
    string? RestrictSalesUserId,
    string? RestrictPurchaseUserId,
    bool RestrictEither);
