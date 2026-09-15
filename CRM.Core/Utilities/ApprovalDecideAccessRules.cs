using CRM.Core.Interfaces;

namespace CRM.Core.Utilities;

/// <summary>
/// 待审批通过/拒绝：SuperAdmin 与产品 Admin（SYS_MANAGER）可审全部类型，不依赖各业务写权限。
/// </summary>
public static class ApprovalDecideAccessRules
{
    public static bool HasPlatformApproveBypass(UserPermissionSummaryDto? summary)
        => summary != null && (summary.IsSysAdmin || summary.IsSysManager);

    public static bool HasApprovePermission(UserPermissionSummaryDto? summary, string? permissionCode)
    {
        if (HasPlatformApproveBypass(summary))
            return true;
        if (summary?.PermissionCodes == null || string.IsNullOrWhiteSpace(permissionCode))
            return false;

        var code = permissionCode.Trim();
        return summary.PermissionCodes.Any(c =>
            string.Equals(c?.Trim(), code, StringComparison.OrdinalIgnoreCase));
    }
}
