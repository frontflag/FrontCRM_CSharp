using CRM.Core.Interfaces;

namespace CRM.Core.Utilities;

/// <summary>
/// 需求参考页入口与客户明文集合：不裁作业页数据范围，仅决定谁能进页、哪些行可看客户。
/// </summary>
public static class RfqItemReferenceAccessRules
{
    public static bool HasOrgRole(UserPermissionSummaryDto summary, params string[] roleCodes)
    {
        if (summary.RoleCodes == null || summary.RoleCodes.Count == 0 || roleCodes.Length == 0)
            return false;

        foreach (var code in roleCodes)
        {
            if (string.IsNullOrWhiteSpace(code))
                continue;
            if (summary.RoleCodes.Any(c =>
                    string.Equals(c?.Trim(), code.Trim(), StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 销售 / 采购 / 采购运营，或兼任采购部门；系统管理角色与业务数据 bypass 便于运维对照。
    /// 功能码 <c>rfq.read</c> 由路由/API 另检。
    /// </summary>
    public static bool CanEnterPage(UserPermissionSummaryDto? summary)
    {
        if (summary == null)
            return false;
        if (summary.HasBizDataBypass || summary.IsSysAdmin || summary.IsSysManager || summary.IsBizManager)
            return true;
        if (summary.IdentityType is 1 or 2 or 3)
            return true;
        return summary.BelongsToPurchaseDept;
    }

    /// <summary>销售身份按业务员匹配集合打码客户；采购侧（含兼任）不按业务员打码。</summary>
    public static bool NeedsSalespersonCustomerMask(UserPermissionSummaryDto summary)
    {
        if (summary.HasBizDataBypass || summary.IsSysAdmin)
            return false;
        return summary.IdentityType == 1;
    }

    /// <summary>销售经理 / 总监：客户明文用组织子树（<c>GetAllowedUserIds(includeChildren: true)</c>）。</summary>
    public static bool UsesOrgSubtreeCustomerReveal(UserPermissionSummaryDto summary) =>
        NeedsSalespersonCustomerMask(summary) &&
        HasOrgRole(summary, "DEPT_MANAGER", "DEPT_DIRECTOR");

    public static bool CanRevealCustomerOnRow(
        UserPermissionSummaryDto summary,
        string? salesUserId,
        IReadOnlySet<string>? orgRevealUserIds)
    {
        if (!NeedsSalespersonCustomerMask(summary))
            return true;
        if (string.IsNullOrWhiteSpace(salesUserId))
            return false;

        var sid = salesUserId.Trim();
        if (UsesOrgSubtreeCustomerReveal(summary))
            return orgRevealUserIds != null && orgRevealUserIds.Contains(sid);

        var uid = summary.UserId?.Trim();
        return !string.IsNullOrEmpty(uid) &&
               string.Equals(uid, sid, StringComparison.OrdinalIgnoreCase);
    }
}
