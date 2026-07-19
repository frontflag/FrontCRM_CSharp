using CRM.Core.Interfaces;

namespace CRM.Core.Utilities;

/// <summary>采购订单更换供应商：管理员、采购侧总监，或显式 RBAC 权限。</summary>
public static class PurchaseOrderVendorChangeAccessRules
{
    public const string PermissionCode = "purchase-order.change-vendor";

    public static bool CanChangeVendor(UserPermissionSummaryDto? summary)
    {
        if (summary == null)
            return false;
        if (summary.IsSysAdmin)
            return true;
        if (summary.PermissionCodes != null
            && summary.PermissionCodes.Any(c =>
                string.Equals(c?.Trim(), PermissionCode, StringComparison.OrdinalIgnoreCase)))
            return true;
        return RfqItemQuoteAccessRules.IsPurchaseDepartmentDirector(summary);
    }
}
