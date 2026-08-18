using CRM.Core.Interfaces;

namespace CRM.Core.Utilities;

/// <summary>
/// 需求回收站：系统/平台管理员、业务经理，或主部门为销售的部门总监。
/// </summary>
public static class RfqRecycleBinAccessRules
{
    /// <summary>与部门身份约定：1=销售。</summary>
    public const short SalesIdentityType = 1;

    public const string DeptDirectorRoleCode = "DEPT_DIRECTOR";

    public static bool CanAccess(UserPermissionSummaryDto? summary)
    {
        if (summary == null)
            return false;
        if (summary.IsSysAdmin || summary.IsSysManager || summary.IsBizManager)
            return true;
        if (summary.IdentityType != SalesIdentityType)
            return false;
        if (summary.RoleCodes == null || summary.RoleCodes.Count == 0)
            return false;

        return summary.RoleCodes.Any(c =>
            string.Equals(c?.Trim(), DeptDirectorRoleCode, StringComparison.OrdinalIgnoreCase));
    }
}
