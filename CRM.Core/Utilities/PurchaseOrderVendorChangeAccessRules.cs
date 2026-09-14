using CRM.Core.Interfaces;

namespace CRM.Core.Utilities;

/// <summary>
/// 采购订单更换供应商 / 联系人：SuperAdmin、产品 Admin（SYS_MANAGER）、
/// 采购部/采购运营部总监，或显式 RBAC 权限；审核前采购员凭写权限亦可。
/// </summary>
public static class PurchaseOrderVendorChangeAccessRules
{
    public const string PermissionCode = "purchase-order.change-vendor";

    /// <summary>新建 / 待审核 / 审核失败：采购员可换供应商；审批按落库后的最新供应商。</summary>
    public static bool IsPreAuditStatus(short orderStatus) =>
        orderStatus is 1 or 2 or -1;

    /// <summary>
    /// 不区分订单状态：<c>SYS_ADMIN</c>、<c>SYS_MANAGER</c>、采购部总监、采购运营部总监、或 <see cref="PermissionCode"/>。
    /// 主部门是否销售不影响 Admin / SuperAdmin。
    /// </summary>
    public static bool CanChangeVendor(UserPermissionSummaryDto? summary)
    {
        if (summary == null)
            return false;
        if (summary.IsSysAdmin || summary.IsSysManager)
            return true;
        if (summary.PermissionCodes != null
            && summary.PermissionCodes.Any(c =>
                string.Equals(c?.Trim(), PermissionCode, StringComparison.OrdinalIgnoreCase)))
            return true;
        return RfqItemQuoteAccessRules.IsPurchaseDepartmentDirector(summary);
    }

    /// <summary>
    /// 按订单主状态判定能否换供应商 / 刷新供应商名称。
    /// 高权限账号不限状态（含销售部 SYS_MANAGER）；其余采购脱敏身份一律否；采购员凭 <c>purchase-order.write</c> 仅限审核前。
    /// </summary>
    public static bool CanChangeVendorOnOrder(UserPermissionSummaryDto? summary, short orderStatus)
    {
        if (summary == null)
            return false;
        // 高权限先放行，避免销售部 Admin（SYS_MANAGER）被 511 脱敏误拦
        if (CanChangeVendor(summary))
            return true;
        if (PurchaseSensitiveFieldMask511.ShouldMask(summary))
            return false;
        if (!IsPreAuditStatus(orderStatus))
            return false;
        return summary.IsSysAdmin
            || summary.IsSysManager
            || (summary.PermissionCodes != null
                && summary.PermissionCodes.Any(c =>
                    string.Equals(c?.Trim(), "purchase-order.write", StringComparison.OrdinalIgnoreCase)));
    }
}
