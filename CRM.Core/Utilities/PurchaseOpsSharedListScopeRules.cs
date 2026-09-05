using CRM.Core.Interfaces;

namespace CRM.Core.Utilities;

/// <summary>
/// 采购运营职员（<c>purchase_ops_operator</c>）共享范围：不按 <c>PurchaseUserId</c> / 分配采购员 / 财务禁止范围收缩。
/// 用于采购申请、报价、需求主表、付款/进项发票列表及付款侧栏菜单（与备货采购清单口径一致）。
/// </summary>
public static class PurchaseOpsSharedListScopeRules
{
    public const string PurchaseOpsOperatorRoleCode = "purchase_ops_operator";

    public static bool UsesSharedListScope(UserPermissionSummaryDto? summary)
    {
        if (summary == null)
            return false;
        if (summary.IsSysAdmin || summary.HasBizDataBypass)
            return true;
        if (summary.PurchaseDataScope == 4)
            return false;
        return HasPurchaseOpsOperatorRole(summary.RoleCodes);
    }

    private static bool HasPurchaseOpsOperatorRole(IReadOnlyList<string>? roleCodes)
    {
        if (roleCodes == null || roleCodes.Count == 0)
            return false;
        foreach (var r in roleCodes)
        {
            if (string.IsNullOrWhiteSpace(r))
                continue;
            if (string.Equals(r.Trim(), PurchaseOpsOperatorRoleCode, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
