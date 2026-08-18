using CRM.Core.Interfaces;

namespace CRM.Core.Utilities;

/// <summary>
/// 入库/出库运维检查：系统/平台管理员，或主部门为财务的部门总监。
/// </summary>
public static class InventoryOpsCheckAccessRules
{
    /// <summary>与部门身份约定：5=财务。</summary>
    public const short FinanceIdentityType = 5;

    public const string DeptDirectorRoleCode = "DEPT_DIRECTOR";

    public static bool CanAccess(UserPermissionSummaryDto? summary)
    {
        if (summary == null)
            return false;
        if (summary.IsSysAdmin || summary.IsSysManager)
            return true;
        if (summary.IdentityType != FinanceIdentityType)
            return false;
        if (summary.RoleCodes == null || summary.RoleCodes.Count == 0)
            return false;

        return summary.RoleCodes.Any(c =>
            string.Equals(c?.Trim(), DeptDirectorRoleCode, StringComparison.OrdinalIgnoreCase));
    }
}
