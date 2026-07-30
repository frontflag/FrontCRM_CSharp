using CRM.Core.Constants;
using CRM.Core.Interfaces;

namespace CRM.API.Authorization;

/// <summary>判断权限摘要是否具备指定权限码（含遗留 rbac.manage → system.*）。</summary>
public static class PermissionSummaryExtensions
{
    public static bool HasPermissionCode(this UserPermissionSummaryDto? summary, string permissionCode)
    {
        if (summary == null || string.IsNullOrWhiteSpace(permissionCode)) return false;
        if (summary.IsSysAdmin) return true;
        var isSystem = SystemPermissionCodes.IsSystemPermission(permissionCode);
        // 管理角色业务权限与 SuperAdmin 对齐（system.* 仍须管理身份 + 权限码）
        if (!isSystem && summary.HasBizDataBypass) return true;
        if (isSystem && !summary.HasManagementAccess)
            return false;
        if (summary.PermissionCodes.Any(c =>
                string.Equals(c, permissionCode, StringComparison.OrdinalIgnoreCase)))
            return true;
        if (SystemPermissionCodes.IsSystemPermission(permissionCode) &&
            summary.PermissionCodes.Any(c =>
                string.Equals(c, SystemPermissionCodes.LegacyRbacManage, StringComparison.OrdinalIgnoreCase)))
            return true;
        return false;
    }
}
