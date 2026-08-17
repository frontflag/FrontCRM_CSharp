using CRM.Core.Interfaces;

namespace CRM.Core.Utilities;

/// <summary>
/// 到货通知「编辑到货信息」：系统/平台管理员、业务经理，或主部门物流总监/经理（只读部门除外）。
/// </summary>
public static class ArrivalNoticeArrivalInfoAccessRules
{
    /// <summary>与部门身份约定：6=物流。</summary>
    public const short LogisticsIdentityType = 6;

    public static bool IsManagementEditor(UserPermissionSummaryDto summary) =>
        summary.IsSysAdmin || summary.IsSysManager || summary.IsBizManager;

    /// <summary>主部门身份为物流，且组织角色为部门总监或部门经理。</summary>
    public static bool IsLogisticsDepartmentLead(UserPermissionSummaryDto summary)
    {
        if (summary.IdentityType != LogisticsIdentityType)
            return false;
        if (summary.RoleCodes == null || summary.RoleCodes.Count == 0)
            return false;

        return summary.RoleCodes.Any(c =>
        {
            var code = c?.Trim();
            return string.Equals(code, "DEPT_DIRECTOR", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(code, "DEPT_MANAGER", StringComparison.OrdinalIgnoreCase);
        });
    }

    public static bool CanEdit(UserPermissionSummaryDto summary)
    {
        if (IsManagementEditor(summary))
            return true;
        if (!IsLogisticsDepartmentLead(summary))
            return false;
        return summary.LogisticsDataAccess != 1;
    }
}
