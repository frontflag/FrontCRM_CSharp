using CRM.Core.Interfaces;

namespace CRM.Core.Utilities;

/// <summary>
/// 销售总监可审批本人提交的客户、销售订单。经理/员工仍禁止自审；采购/财务总监不适用。
/// </summary>
public static class SalesDirectorSelfApprovalRules
{
    /// <summary>与部门身份约定：1=销售。</summary>
    public const short SalesIdentityType = 1;

    public const string DeptDirectorRoleCode = "DEPT_DIRECTOR";

    /// <summary>系统管理员或销售部门总监：可对本人提交的客户 / 销售订单做通过或拒绝。</summary>
    public static bool AllowsOwnCustomerOrSalesOrderDecide(UserPermissionSummaryDto? summary)
    {
        if (summary == null)
            return false;
        if (summary.IsSysAdmin)
            return true;
        if (summary.IdentityType != SalesIdentityType)
            return false;
        if (summary.RoleCodes == null || summary.RoleCodes.Count == 0)
            return false;

        return summary.RoleCodes.Any(c =>
            string.Equals(c?.Trim(), DeptDirectorRoleCode, StringComparison.OrdinalIgnoreCase));
    }
}
