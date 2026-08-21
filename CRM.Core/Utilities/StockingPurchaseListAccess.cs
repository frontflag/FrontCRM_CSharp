using System.Linq;
using CRM.Core.Interfaces;

namespace CRM.Core.Utilities;

/// <summary>
/// 备货采购清单入口：采购侧职员只读共享列表（不按采购员数据范围收缩）。
/// </summary>
public static class StockingPurchaseListAccess
{
    public static readonly string[] PurchasingRoleCodes =
    {
        "purchase_buyer",
        "purchase_operator",
        "purchase_ops_operator"
    };

    public static bool CanEnter(UserPermissionSummaryDto? summary)
    {
        if (summary == null)
            return false;

        if (summary.PurchaseDataScope == 4 && !summary.IsSysAdmin && !summary.HasBizDataBypass)
            return false;

        if (summary.IsSysAdmin || summary.HasBizDataBypass)
            return true;

        if (!HasPermission(summary, "purchase-order.read"))
            return false;

        if (summary.BelongsToPurchaseDept)
            return true;
        if (summary.IdentityType is 2 or 3)
            return true;
        return HasPurchasingRole(summary.RoleCodes);
    }

    public static bool CanReadStockingPurchaseOrder(UserPermissionSummaryDto? summary, short purchaseOrderType) =>
        CanEnter(summary) && purchaseOrderType == PurchaseOrderItemLinkRules.PurchaseOrderTypeStocking;

    private static bool HasPermission(UserPermissionSummaryDto summary, string code)
    {
        var codes = summary.PermissionCodes;
        if (codes == null || codes.Count == 0)
            return false;
        return codes.Any(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasPurchasingRole(IReadOnlyList<string>? roleCodes)
    {
        if (roleCodes == null || roleCodes.Count == 0)
            return false;
        foreach (var r in roleCodes)
        {
            if (string.IsNullOrWhiteSpace(r))
                continue;
            foreach (var p in PurchasingRoleCodes)
            {
                if (string.Equals(r.Trim(), p, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }
}
