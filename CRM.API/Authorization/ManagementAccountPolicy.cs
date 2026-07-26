using CRM.Core.Constants;
using CRM.Core.Interfaces;

namespace CRM.API.Authorization;

/// <summary>管理角色账号维护边界与可赋角色白名单。</summary>
public static class ManagementAccountPolicy
{
    private static readonly HashSet<string> BusinessRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "DEPT_DIRECTOR",
        "DEPT_MANAGER",
        "DEPT_EMPLOYEE",
        "purchase_buyer",
        "biz_all",
        "sales_operator",
        "purchase_operator",
        "commerce_operator",
        "purchase_ops_operator",
        "logistics_operator",
        "finance_operator"
    };

    public static IReadOnlyCollection<string> GetAssignableRoleCodes(UserPermissionSummaryDto actor)
    {
        if (actor.IsSysAdmin)
        {
            var all = new HashSet<string>(BusinessRoleCodes, StringComparer.OrdinalIgnoreCase)
            {
                ManagementRoleCodes.SuperAdmin,
                ManagementRoleCodes.Admin,
                ManagementRoleCodes.Manager
            };
            return all;
        }

        if (actor.IsSysManager)
        {
            var adminSet = new HashSet<string>(BusinessRoleCodes, StringComparer.OrdinalIgnoreCase)
            {
                ManagementRoleCodes.Manager
            };
            return adminSet;
        }

        if (actor.IsBizManager)
            return BusinessRoleCodes;

        return Array.Empty<string>();
    }

    /// <summary>操作者是否可查看/维护目标用户（按目标 RoleCodes）。</summary>
    public static bool CanMaintainTarget(UserPermissionSummaryDto actor, IEnumerable<string>? targetRoleCodes)
    {
        if (actor.IsSysAdmin)
            return true;

        if (ManagementRoleCodes.TargetIsSuperAdmin(targetRoleCodes))
            return false;

        if (actor.IsSysManager)
            return true;

        if (actor.IsBizManager)
            return !ManagementRoleCodes.TargetHasManagementRole(targetRoleCodes);

        return false;
    }

    public static bool CanAssignRoleCode(UserPermissionSummaryDto actor, string roleCode) =>
        GetAssignableRoleCodes(actor).Contains(roleCode);
}
