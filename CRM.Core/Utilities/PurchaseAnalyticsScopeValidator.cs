using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Rbac;

namespace CRM.Core.Utilities;

/// <summary>采购看板 viewLevel / departmentId / purchaseUserId 服务端校验（与 <see cref="DataPermissionService.ApplyPurchaseOrderDataScopeAsync"/> 配合）。</summary>
public static class PurchaseAnalyticsScopeValidator
{
    public const string UnassignedDepartmentId = SalesAnalyticsScopeValidator.UnassignedDepartmentId;

    public static bool CanAccessPage(UserPermissionSummaryDto summary)
    {
        if (summary.IsSysAdmin) return true;
        if (BusinessDepartmentRules.UsePurchaseOrderAssistorOnlyScope(summary)) return true;
        if (summary.PurchaseDataScope == 4) return false;
        return summary.PurchaseDataScope is 0 or 1 or 2 or 3;
    }

    public static IReadOnlyList<string> GetAllowedViewLevels(UserPermissionSummaryDto summary)
    {
        if (summary.IsSysAdmin || summary.PurchaseDataScope == 0)
            return new[] { SalesAnalyticsViewLevels.Company, SalesAnalyticsViewLevels.Department, SalesAnalyticsViewLevels.Personal };

        // 跟单助理：数据池仅为 assistor=自己，只开放个人透镜。
        if (BusinessDepartmentRules.UsePurchaseOrderAssistorOnlyScope(summary))
            return new[] { SalesAnalyticsViewLevels.Personal };

        // Scope 1/2/3：三层透镜均可切换；数据仍由 ApplyPurchaseOrderDataScopeAsync 封顶。
        // Scope≠0 时公司 Tab 文案为「可见范围」（权限内汇总，非全集团）。
        return summary.PurchaseDataScope switch
        {
            1 or 2 or 3 => new[]
            {
                SalesAnalyticsViewLevels.Company,
                SalesAnalyticsViewLevels.Department,
                SalesAnalyticsViewLevels.Personal
            },
            _ => Array.Empty<string>()
        };
    }

    public static string GetDefaultViewLevel(UserPermissionSummaryDto summary)
    {
        var allowed = GetAllowedViewLevels(summary);
        if (allowed.Count == 0) return SalesAnalyticsViewLevels.Personal;
        if (summary.IsSysAdmin || summary.PurchaseDataScope == 0) return SalesAnalyticsViewLevels.Company;
        if (summary.PurchaseDataScope == 3) return SalesAnalyticsViewLevels.Department;
        if (summary.PurchaseDataScope == 2) return SalesAnalyticsViewLevels.Department;
        return SalesAnalyticsViewLevels.Personal;
    }

    public static string BuildScopeLabel(
        UserPermissionSummaryDto summary,
        string? primaryDepartmentName,
        bool companyTabVisible)
    {
        if (summary.IsSysAdmin || summary.PurchaseDataScope == 0)
            return companyTabVisible ? "全公司" : "可见范围";

        if (BusinessDepartmentRules.UsePurchaseOrderAssistorOnlyScope(summary))
            return "跟单可见范围";

        var dept = string.IsNullOrWhiteSpace(primaryDepartmentName) ? "本部门" : primaryDepartmentName.Trim();
        return summary.PurchaseDataScope switch
        {
            3 => $"{dept}及下级",
            2 => dept,
            1 => "本人",
            _ => "无采购数据"
        };
    }

    public sealed class ValidationResult
    {
        public bool Ok { get; init; }
        public string? Error { get; init; }
        public string ViewLevel { get; init; } = SalesAnalyticsViewLevels.Personal;
        public string? DepartmentId { get; init; }
        public string? PurchaseUserId { get; init; }
    }

    public static ValidationResult Validate(
        UserPermissionSummaryDto summary,
        string? viewLevel,
        string? departmentId,
        string? purchaseUserId,
        IReadOnlyList<RbacDepartment> departments,
        IReadOnlyList<RbacUserDepartment> userDepartments,
        HashSet<string> allowedUserIds)
    {
        var allowedLevels = GetAllowedViewLevels(summary);
        if (allowedLevels.Count == 0)
            return new ValidationResult { Ok = false, Error = "当前账号无采购数据范围" };

        // 前端默认常传 company；无公司视角时回落到账号默认层，避免首屏报错。
        var level = string.IsNullOrWhiteSpace(viewLevel)
            ? GetDefaultViewLevel(summary)
            : viewLevel.Trim().ToLowerInvariant();

        if (!allowedLevels.Contains(level, StringComparer.OrdinalIgnoreCase))
        {
            level = GetDefaultViewLevel(summary);
            if (!allowedLevels.Contains(level, StringComparer.OrdinalIgnoreCase))
                return new ValidationResult { Ok = false, Error = $"viewLevel={level} 超出当前数据范围" };
        }

        string? resolvedDept = null;
        string? resolvedUser = null;

        if (level == SalesAnalyticsViewLevels.Personal)
        {
            if (summary.PurchaseDataScope == 1 || BusinessDepartmentRules.UsePurchaseOrderAssistorOnlyScope(summary))
                resolvedUser = summary.UserId;
            else if (!string.IsNullOrWhiteSpace(purchaseUserId))
            {
                var uid = purchaseUserId.Trim();
                if (allowedUserIds.Count > 0 && !allowedUserIds.Contains(uid))
                    return new ValidationResult { Ok = false, Error = "无权查看该采购员数据" };
                resolvedUser = uid;
            }
        }
        else if (level == SalesAnalyticsViewLevels.Department)
        {
            if (summary.PurchaseDataScope == 2)
                resolvedDept = summary.PrimaryDepartmentId;
            else if (!string.IsNullOrWhiteSpace(departmentId))
            {
                var did = departmentId.Trim();
                if (!IsDepartmentAllowed(summary, did, departments))
                    return new ValidationResult { Ok = false, Error = "无权查看该部门数据" };
                resolvedDept = did;
            }
            else if (summary.PurchaseDataScope == 3 || summary.IsSysAdmin || summary.PurchaseDataScope == 0)
                resolvedDept = summary.PrimaryDepartmentId;
            else
                resolvedDept = summary.PrimaryDepartmentId;
        }

        return new ValidationResult
        {
            Ok = true,
            ViewLevel = level,
            DepartmentId = resolvedDept,
            PurchaseUserId = resolvedUser
        };
    }

    public static IReadOnlyList<SalesAnalyticsDepartmentOptionDto> BuildAllowedDepartments(
        UserPermissionSummaryDto summary,
        IReadOnlyList<RbacDepartment> departments) =>
        SalesAnalyticsScopeValidator.BuildAllowedDepartments(
            MapPurchaseScopeToSalesScope(summary),
            departments);

    private static UserPermissionSummaryDto MapPurchaseScopeToSalesScope(UserPermissionSummaryDto summary) =>
        new()
        {
            UserId = summary.UserId,
            IsSysAdmin = summary.IsSysAdmin,
            SaleDataScope = summary.PurchaseDataScope,
            PrimaryDepartmentId = summary.PrimaryDepartmentId
        };

    private static bool IsDepartmentAllowed(
        UserPermissionSummaryDto summary,
        string departmentId,
        IReadOnlyList<RbacDepartment> departments)
    {
        if (string.Equals(departmentId, UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase))
            return summary.IsSysAdmin || summary.PurchaseDataScope is 0 or 3;

        if (summary.IsSysAdmin || summary.PurchaseDataScope == 0)
            return departments.Any(d => d.Id == departmentId && d.Status == 1);

        if (summary.PurchaseDataScope == 3 && !string.IsNullOrWhiteSpace(summary.PrimaryDepartmentId))
        {
            var current = departments.FirstOrDefault(d => d.Id == summary.PrimaryDepartmentId);
            if (current == null) return false;
            var target = departments.FirstOrDefault(d => d.Id == departmentId);
            if (target == null) return false;
            if (target.Id == current.Id) return true;
            var prefix = string.IsNullOrWhiteSpace(current.Path) ? null : current.Path + "/";
            return prefix != null && !string.IsNullOrWhiteSpace(target.Path)
                   && target.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(departmentId, summary.PrimaryDepartmentId, StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<HashSet<string>> BuildAllowedPurchaseUserIdsAsync(
        IDataPermissionService dataPermission,
        UserPermissionSummaryDto summary,
        CancellationToken cancellationToken = default)
    {
        if (summary.IsSysAdmin || summary.PurchaseDataScope == 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (summary.PurchaseDataScope == 1 || BusinessDepartmentRules.UsePurchaseOrderAssistorOnlyScope(summary))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { summary.UserId };

        return await dataPermission.GetAllowedUserIdsForDataScopeAsync(
            summary,
            includeChildren: summary.PurchaseDataScope == 3,
            cancellationToken);
    }
}
