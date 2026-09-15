using CRM.Core.Constants;
using CRM.Core.Interfaces;

namespace CRM.Core.Utilities;

/// <summary>
/// 客户详情「新闻动态」立即抓取：业务员本人、组织上级、SYS_ADMIN / SYS_MANAGER、或客户调查 AI 权限。
/// 521 脱敏账号不可看、不可抓（由调用方先判 <see cref="SaleSensitiveFieldMask521"/>）。
/// </summary>
public static class CustomerNewsMonitorAccessRules
{
    public static bool HasCustomerIntelPermission(UserPermissionSummaryDto? summary)
    {
        if (summary?.PermissionCodes == null)
            return false;
        return summary.PermissionCodes.Any(c =>
            string.Equals(c?.Trim(), AiPermissionCodes.CustomerIntelLookup, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>部门经理 / 总监视为上级；普通员工（level ≤ 1）即使数据范围覆盖同部门也不算上级。</summary>
    public static bool IsOrgSuperiorRole(UserPermissionSummaryDto? summary)
    {
        if (summary?.RoleCodes == null)
            return false;
        foreach (var code in summary.RoleCodes)
        {
            var c = (code ?? string.Empty).Trim().ToUpperInvariant();
            if (c is "DEPT_DIRECTOR" or "DEPT_MANAGER")
                return true;
        }

        return false;
    }

    public static bool CanFetch(
        UserPermissionSummaryDto? summary,
        bool isSalesOwner,
        bool salespersonInOrgAllowSet)
    {
        if (summary == null)
            return false;
        if (summary.IsSysAdmin || summary.IsSysManager)
            return true;
        if (ManagementRoleCodes.IsSuperAdmin(summary.RoleCodes)
            || ManagementRoleCodes.IsAdminRole(summary.RoleCodes))
            return true;
        if (HasCustomerIntelPermission(summary))
            return true;
        if (isSalesOwner)
            return true;
        return IsOrgSuperiorRole(summary) && salespersonInOrgAllowSet;
    }
}
