using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Rbac;

namespace CRM.Core.Utilities;

/// <summary>销售看板 viewLevel / departmentId / salesUserId 服务端校验（与 <see cref="DataPermissionService.ApplySellOrderDataScopeAsync"/> 配合）。</summary>
public static class SalesAnalyticsScopeValidator
{
    public const string UnassignedDepartmentId = "__unassigned__";

    public static bool CanAccessPage(UserPermissionSummaryDto summary)
    {
        if (summary.HasBizDataBypass) return true;
        if (BusinessDepartmentRules.UseSellOrderAssistorOnlyScope(summary)) return true;
        if (summary.SaleDataScope == 4) return false;
        return true;
    }

    public static IReadOnlyList<string> GetAllowedViewLevels(UserPermissionSummaryDto summary)
    {
        if (summary.HasBizDataBypass || summary.SaleDataScope == 0)
            return new[] { SalesAnalyticsViewLevels.Company, SalesAnalyticsViewLevels.Department, SalesAnalyticsViewLevels.Personal };

        if (BusinessDepartmentRules.UseSellOrderAssistorOnlyScope(summary))
            return new[] { SalesAnalyticsViewLevels.Personal };

        return summary.SaleDataScope switch
        {
            3 => new[] { SalesAnalyticsViewLevels.Company, SalesAnalyticsViewLevels.Department, SalesAnalyticsViewLevels.Personal },
            2 => new[] { SalesAnalyticsViewLevels.Department, SalesAnalyticsViewLevels.Personal },
            1 => new[] { SalesAnalyticsViewLevels.Personal },
            _ => Array.Empty<string>()
        };
    }

    public static string GetDefaultViewLevel(UserPermissionSummaryDto summary)
    {
        var allowed = GetAllowedViewLevels(summary);
        if (allowed.Count == 0) return SalesAnalyticsViewLevels.Personal;
        if (summary.HasBizDataBypass || summary.SaleDataScope == 0) return SalesAnalyticsViewLevels.Company;
        if (summary.SaleDataScope == 3) return SalesAnalyticsViewLevels.Department;
        if (summary.SaleDataScope == 2) return SalesAnalyticsViewLevels.Department;
        return SalesAnalyticsViewLevels.Personal;
    }

    public static string BuildScopeLabel(
        UserPermissionSummaryDto summary,
        string? primaryDepartmentName,
        bool companyTabVisible)
    {
        if (summary.HasBizDataBypass || summary.SaleDataScope == 0)
            return companyTabVisible ? "全公司" : "可见范围";

        if (BusinessDepartmentRules.UseSellOrderAssistorOnlyScope(summary))
            return "跟单可见范围";

        var dept = string.IsNullOrWhiteSpace(primaryDepartmentName) ? "本部门" : primaryDepartmentName.Trim();
        return summary.SaleDataScope switch
        {
            3 => $"{dept}及下级",
            2 => dept,
            1 => "本人",
            _ => "无销售数据"
        };
    }

    public sealed class ValidationResult
    {
        public bool Ok { get; init; }
        public string? Error { get; init; }
        public string ViewLevel { get; init; } = SalesAnalyticsViewLevels.Personal;
        public string? DepartmentId { get; init; }
        public string? SalesUserId { get; init; }
    }

    public static ValidationResult Validate(
        UserPermissionSummaryDto summary,
        string? viewLevel,
        string? departmentId,
        string? salesUserId,
        IReadOnlyList<RbacDepartment> departments,
        IReadOnlyList<RbacUserDepartment> userDepartments,
        HashSet<string> allowedUserIds)
    {
        var allowedLevels = GetAllowedViewLevels(summary);
        if (allowedLevels.Count == 0)
            return new ValidationResult { Ok = false, Error = "当前账号无销售数据范围" };

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
            if (summary.SaleDataScope == 1 || BusinessDepartmentRules.UseSellOrderAssistorOnlyScope(summary))
                resolvedUser = summary.UserId;
            else if (!string.IsNullOrWhiteSpace(salesUserId))
            {
                var uid = salesUserId.Trim();
                // 全公司 / 超管：allowedUserIds 为空表示不限制业务员；仅在有明确白名单时校验
                if (allowedUserIds.Count > 0 && !allowedUserIds.Contains(uid))
                    return new ValidationResult { Ok = false, Error = "无权查看该业务员数据" };
                resolvedUser = uid;
            }
        }
        else if (level == SalesAnalyticsViewLevels.Department)
        {
            if (summary.SaleDataScope == 2)
                resolvedDept = summary.PrimaryDepartmentId;
            else if (!string.IsNullOrWhiteSpace(departmentId))
            {
                var did = departmentId.Trim();
                if (!IsDepartmentAllowed(summary, did, departments))
                    return new ValidationResult { Ok = false, Error = "无权查看该部门数据" };
                resolvedDept = did;
            }
            else if (summary.SaleDataScope == 3 || summary.HasBizDataBypass || summary.SaleDataScope == 0)
                resolvedDept = summary.PrimaryDepartmentId;
            else
                resolvedDept = summary.PrimaryDepartmentId;
        }

        return new ValidationResult
        {
            Ok = true,
            ViewLevel = level,
            DepartmentId = resolvedDept,
            SalesUserId = resolvedUser
        };
    }

    public static IReadOnlyList<SalesAnalyticsDepartmentOptionDto> BuildAllowedDepartments(
        UserPermissionSummaryDto summary,
        IReadOnlyList<RbacDepartment> departments)
    {
        if (summary.HasBizDataBypass || summary.SaleDataScope == 0)
        {
            return departments
                .Where(d => d.Status == 1)
                .OrderBy(d => d.Path)
                .Select(d => new SalesAnalyticsDepartmentOptionDto { Id = d.Id, Name = d.DepartmentName })
                .ToList();
        }

        if (summary.SaleDataScope != 3 || string.IsNullOrWhiteSpace(summary.PrimaryDepartmentId))
            return Array.Empty<SalesAnalyticsDepartmentOptionDto>();

        var current = departments.FirstOrDefault(d => d.Id == summary.PrimaryDepartmentId);
        if (current == null) return Array.Empty<SalesAnalyticsDepartmentOptionDto>();

        var prefix = string.IsNullOrWhiteSpace(current.Path) ? null : current.Path + "/";
        var list = new List<SalesAnalyticsDepartmentOptionDto> { new() { Id = current.Id, Name = current.DepartmentName } };
        foreach (var d in departments.Where(d => d.Status == 1))
        {
            if (d.Id == current.Id) continue;
            if (prefix != null && !string.IsNullOrWhiteSpace(d.Path) && d.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                list.Add(new SalesAnalyticsDepartmentOptionDto { Id = d.Id, Name = d.DepartmentName });
        }

        list.Add(new SalesAnalyticsDepartmentOptionDto { Id = UnassignedDepartmentId, Name = "未分配部门" });
        return list.OrderBy(x => x.Name).ToList();
    }

    private static bool IsDepartmentAllowed(
        UserPermissionSummaryDto summary,
        string departmentId,
        IReadOnlyList<RbacDepartment> departments)
    {
        if (string.Equals(departmentId, UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase))
            return summary.HasBizDataBypass || summary.SaleDataScope is 0 or 3;

        if (summary.HasBizDataBypass || summary.SaleDataScope == 0)
            return departments.Any(d => d.Id == departmentId && d.Status == 1);

        if (summary.SaleDataScope == 3 && !string.IsNullOrWhiteSpace(summary.PrimaryDepartmentId))
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

    public static async Task<HashSet<string>> BuildAllowedSalesUserIdsAsync(
        IDataPermissionService dataPermission,
        UserPermissionSummaryDto summary,
        CancellationToken cancellationToken = default)
    {
        if (summary.HasBizDataBypass || summary.SaleDataScope == 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (summary.SaleDataScope == 1 || BusinessDepartmentRules.UseSellOrderAssistorOnlyScope(summary))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { summary.UserId };

        return await dataPermission.GetAllowedUserIdsForDataScopeAsync(
            summary,
            includeChildren: summary.SaleDataScope == 3,
            cancellationToken);
    }
}
