using CRM.Core.Interfaces;
using CRM.Core.Models.RFQ;

namespace CRM.Core.Utilities;

/// <summary>
/// 需求明细「报价」操作权限：系统管理员、采购侧总监、或该行分配的报价员。
/// </summary>
public static class RfqItemQuoteAccessRules
{
    /// <summary>采购部 / 采购运营部总监（组织角色 DEPT_DIRECTOR + 主部门 IdentityType 2/3）。</summary>
    public static bool IsPurchaseDepartmentDirector(UserPermissionSummaryDto summary)
    {
        if (summary.RoleCodes == null || summary.RoleCodes.Count == 0)
            return false;

        var isDirector = summary.RoleCodes.Any(c =>
            string.Equals(c?.Trim(), "DEPT_DIRECTOR", StringComparison.OrdinalIgnoreCase));
        if (!isDirector)
            return false;

        return summary.IdentityType is 2 or 3;
    }

    public static bool IsAssignedQuoter(string? userId, RFQItem? item)
    {
        if (string.IsNullOrWhiteSpace(userId) || item == null)
            return false;

        var uid = userId.Trim();
        var id1 = item.AssignedPurchaserUserId1?.Trim();
        var id2 = item.AssignedPurchaserUserId2?.Trim();

        return (!string.IsNullOrEmpty(id1) && string.Equals(id1, uid, StringComparison.OrdinalIgnoreCase))
               || (!string.IsNullOrEmpty(id2) && string.Equals(id2, uid, StringComparison.OrdinalIgnoreCase));
    }

    public static bool CanQuote(UserPermissionSummaryDto summary, RFQItem item, string? actingUserId)
    {
        if (summary.IsSysAdmin)
            return true;
        if (IsPurchaseDepartmentDirector(summary))
            return true;
        return IsAssignedQuoter(actingUserId ?? summary.UserId, item);
    }

    public static bool CanQuote(UserPermissionSummaryDto summary, string? assignedPurchaserUserId1, string? assignedPurchaserUserId2, string? actingUserId)
    {
        if (summary.IsSysAdmin)
            return true;
        if (IsPurchaseDepartmentDirector(summary))
            return true;

        var item = new RFQItem
        {
            AssignedPurchaserUserId1 = assignedPurchaserUserId1,
            AssignedPurchaserUserId2 = assignedPurchaserUserId2
        };
        return IsAssignedQuoter(actingUserId ?? summary.UserId, item);
    }
}
