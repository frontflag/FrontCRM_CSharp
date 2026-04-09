using CRM.Core.Interfaces;

namespace CRM.API.Authorization;

/// <summary>
/// 创建采购订单：销售员主部门(1)已剥离 purchase-order.read，不得仅凭 PR 写建 PO。
/// 采购侧：PO 写；或 PR 写且（采购主部门 / PO 读 / 采购业务角色如 purchase_buyer）。
/// </summary>
internal static class PurchaseOrderCreateGate
{
    private static readonly string[] PurchasingRoleCodes =
    {
        "purchase_buyer",
        "purchase_operator",
        "purchase_ops_operator"
    };

    public static bool CanCreate(UserPermissionSummaryDto summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        if (summary.IsSysAdmin) return true;
        var codes = summary.PermissionCodes;
        if (codes == null || codes.Count == 0) return false;
        if (Has(codes, "purchase-order.write")) return true;
        if (!Has(codes, "purchase-requisition.write")) return false;
        var buyerDept = summary.IdentityType == 2 || summary.IdentityType == 3;
        if (buyerDept) return true;
        if (Has(codes, "purchase-order.read")) return true;
        return HasPurchasingRole(summary.RoleCodes);
    }

    private static bool HasPurchasingRole(IReadOnlyList<string>? roleCodes)
    {
        if (roleCodes == null || roleCodes.Count == 0) return false;
        foreach (var r in roleCodes)
        {
            if (string.IsNullOrWhiteSpace(r)) continue;
            foreach (var p in PurchasingRoleCodes)
            {
                if (string.Equals(r.Trim(), p, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }

    private static bool Has(IReadOnlyList<string> codes, string code) =>
        codes.Any(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase));
}
