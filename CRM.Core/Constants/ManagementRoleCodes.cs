namespace CRM.Core.Constants;

/// <summary>
/// 管理角色编码（产品口头：SuperAdmin / Admin / Manager）。
/// </summary>
public static class ManagementRoleCodes
{
    /// <summary>产品 SuperAdmin</summary>
    public const string SuperAdmin = "SYS_ADMIN";

    /// <summary>产品 Admin</summary>
    public const string Admin = "SYS_MANAGER";

    /// <summary>产品 Manager（业务全量 + 管普通员工）</summary>
    public const string Manager = "SYS_BIZ_MANAGER";

    public static bool IsSuperAdmin(IEnumerable<string>? roleCodes) =>
        Contains(roleCodes, SuperAdmin);

    public static bool IsAdminRole(IEnumerable<string>? roleCodes) =>
        Contains(roleCodes, Admin);

    public static bool IsBizManagerRole(IEnumerable<string>? roleCodes) =>
        Contains(roleCodes, Manager);

    public static bool IsAnyManagementRole(IEnumerable<string>? roleCodes) =>
        IsSuperAdmin(roleCodes) || IsAdminRole(roleCodes) || IsBizManagerRole(roleCodes);

    public static bool IsManagementRoleCode(string? roleCode) =>
        string.Equals(roleCode, SuperAdmin, StringComparison.OrdinalIgnoreCase)
        || string.Equals(roleCode, Admin, StringComparison.OrdinalIgnoreCase)
        || string.Equals(roleCode, Manager, StringComparison.OrdinalIgnoreCase);

    /// <summary>目标账号是否含管理角色（Manager 不可维护）。</summary>
    public static bool TargetHasManagementRole(IEnumerable<string>? roleCodes) =>
        IsAnyManagementRole(roleCodes);

    public static bool TargetIsSuperAdmin(IEnumerable<string>? roleCodes) =>
        IsSuperAdmin(roleCodes);

    private static bool Contains(IEnumerable<string>? roleCodes, string code) =>
        roleCodes != null && roleCodes.Any(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase));
}
