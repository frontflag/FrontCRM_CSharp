using CRM.Core.Interfaces;
using CRM.Core.Models.Rbac;

namespace CRM.Core.Utilities;

/// <summary>
/// 报表「个人」下拉：部门总监看本部门主岗职员；SYS_MANAGER 看该页业务方向的全部主岗职员。
/// </summary>
public static class AnalyticsPersonalRoster
{
    public static bool IsDepartmentDirector(UserPermissionSummaryDto summary) =>
        summary.RoleCodes != null && summary.RoleCodes.Any(c =>
            string.Equals(c?.Trim(), "DEPT_DIRECTOR", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 需要收窄个人名单时返回用户 Id；返回 null 表示沿用原数据范围。
    /// </summary>
    public static HashSet<string>? Resolve(
        UserPermissionSummaryDto summary,
        IReadOnlyList<RbacDepartment> departments,
        IReadOnlyList<RbacUserDepartment> userDepartments,
        IReadOnlyCollection<short> directionIdentityTypes)
    {
        if (summary.IsSysAdmin)
            return null;

        if (summary.IsSysManager)
            return UsersInDirections(departments, userDepartments, directionIdentityTypes);

        if (IsDepartmentDirector(summary))
            return UsersInDepartment(summary.PrimaryDepartmentId, userDepartments);

        return null;
    }

    public static bool CanPickOtherUsers(UserPermissionSummaryDto summary) =>
        summary.IsSysManager || IsDepartmentDirector(summary);

    private static HashSet<string> UsersInDepartment(
        string? departmentId,
        IReadOnlyList<RbacUserDepartment> userDepartments)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(departmentId))
            return ids;

        foreach (var rel in userDepartments)
        {
            if (rel.IsDeleted || !rel.IsPrimary)
                continue;
            if (string.Equals(rel.DepartmentId, departmentId, StringComparison.OrdinalIgnoreCase))
                ids.Add(rel.UserId);
        }

        return ids;
    }

    private static HashSet<string> UsersInDirections(
        IReadOnlyList<RbacDepartment> departments,
        IReadOnlyList<RbacUserDepartment> userDepartments,
        IReadOnlyCollection<short> directionIdentityTypes)
    {
        var deptIds = departments
            .Where(d => d.Status == 1 && directionIdentityTypes.Contains(d.IdentityType))
            .Select(d => d.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rel in userDepartments)
        {
            if (rel.IsDeleted || !rel.IsPrimary)
                continue;
            if (deptIds.Contains(rel.DepartmentId))
                ids.Add(rel.UserId);
        }

        return ids;
    }
}
