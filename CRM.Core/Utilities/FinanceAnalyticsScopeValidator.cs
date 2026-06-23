using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Rbac;

namespace CRM.Core.Utilities;

/// <summary>财务看板 viewLevel / departmentId / ownerUserId 校验；支持财务岗（A 类）与仅销/采 Scope（B 类）。</summary>
public static class FinanceAnalyticsScopeValidator
{
    public static bool IsSalesPurchaseOnlyMode(UserPermissionSummaryDto summary)
    {
        if (summary.IsSysAdmin) return false;
        if (summary.FinanceDataScope is 0 or 1 or 2 or 3) return false;
        return summary.SaleDataScope is 1 or 2 or 3 || summary.PurchaseDataScope is 1 or 2 or 3;
    }

    public static bool CanAccessPage(UserPermissionSummaryDto summary)
    {
        if (summary.IsSysAdmin) return true;
        if (summary.FinanceDataScope is 0 or 1 or 2 or 3) return true;
        if (summary.SaleDataScope == 4 && summary.PurchaseDataScope == 4) return false;
        return summary.SaleDataScope is 1 or 2 or 3 || summary.PurchaseDataScope is 1 or 2 or 3;
    }

    public static IReadOnlyList<string> GetAllowedViewLevels(UserPermissionSummaryDto summary)
    {
        if (summary.IsSysAdmin)
            return AllThree();

        if (IsSalesPurchaseOnlyMode(summary))
        {
            var levels = new List<string> { SalesAnalyticsViewLevels.Personal };
            if (summary.SaleDataScope is 2 or 3 || summary.PurchaseDataScope is 2 or 3)
                levels.Insert(0, SalesAnalyticsViewLevels.Department);
            return levels;
        }

        return summary.FinanceDataScope switch
        {
            0 => AllThree(),
            3 => AllThree(),
            2 => new[] { SalesAnalyticsViewLevels.Department, SalesAnalyticsViewLevels.Personal },
            1 => new[] { SalesAnalyticsViewLevels.Personal },
            _ => Array.Empty<string>()
        };
    }

    public static string GetDefaultViewLevel(UserPermissionSummaryDto summary)
    {
        var allowed = GetAllowedViewLevels(summary);
        if (allowed.Count == 0) return SalesAnalyticsViewLevels.Personal;

        if (IsSalesPurchaseOnlyMode(summary))
        {
            if (allowed.Contains(SalesAnalyticsViewLevels.Department, StringComparer.OrdinalIgnoreCase)
                && (summary.SaleDataScope == 3 || summary.PurchaseDataScope == 3))
                return SalesAnalyticsViewLevels.Department;
            return SalesAnalyticsViewLevels.Personal;
        }

        if (summary.IsSysAdmin || summary.FinanceDataScope == 0) return SalesAnalyticsViewLevels.Company;
        if (summary.FinanceDataScope == 3) return SalesAnalyticsViewLevels.Department;
        if (summary.FinanceDataScope == 2) return SalesAnalyticsViewLevels.Department;
        return SalesAnalyticsViewLevels.Personal;
    }

    public static string BuildScopeLabel(
        UserPermissionSummaryDto summary,
        string? primaryDepartmentName,
        bool companyTabVisible)
    {
        if (IsSalesPurchaseOnlyMode(summary))
        {
            if (summary.SaleDataScope is 2 or 3 || summary.PurchaseDataScope is 2 or 3)
            {
                var dept = string.IsNullOrWhiteSpace(primaryDepartmentName) ? "本部门" : primaryDepartmentName.Trim();
                if (summary.SaleDataScope == 3 || summary.PurchaseDataScope == 3)
                    return $"{dept}及下级（销/采归属）";
                return $"{dept}（销/采归属）";
            }

            return "本人（销/采归属）";
        }

        if (summary.IsSysAdmin || summary.FinanceDataScope == 0)
            return companyTabVisible ? "全公司" : "可见范围";

        var deptName = string.IsNullOrWhiteSpace(primaryDepartmentName) ? "本部门" : primaryDepartmentName.Trim();
        return summary.FinanceDataScope switch
        {
            3 => $"{deptName}及下级",
            2 => deptName,
            1 => "本人",
            _ => "无财务数据"
        };
    }

    public sealed class ValidationResult
    {
        public bool Ok { get; init; }
        public string? Error { get; init; }
        public string ViewLevel { get; init; } = SalesAnalyticsViewLevels.Personal;
        public string? DepartmentId { get; init; }
        public string? OwnerUserId { get; init; }
    }

    public static ValidationResult Validate(
        UserPermissionSummaryDto summary,
        string? viewLevel,
        string? departmentId,
        string? ownerUserId,
        IReadOnlyList<RbacDepartment> departments,
        IReadOnlyList<RbacUserDepartment> userDepartments,
        HashSet<string> salesPurchaseLensUserIds)
    {
        var allowedLevels = GetAllowedViewLevels(summary);
        if (allowedLevels.Count == 0)
            return new ValidationResult { Ok = false, Error = "当前账号无财务分析数据范围" };

        var level = string.IsNullOrWhiteSpace(viewLevel)
            ? GetDefaultViewLevel(summary)
            : viewLevel.Trim().ToLowerInvariant();

        if (!allowedLevels.Contains(level, StringComparer.OrdinalIgnoreCase))
            return new ValidationResult { Ok = false, Error = $"viewLevel={level} 超出当前数据范围" };

        string? resolvedDept = null;
        string? resolvedUser = null;

        if (IsSalesPurchaseOnlyMode(summary))
        {
            if (level == SalesAnalyticsViewLevels.Personal)
                resolvedUser = summary.UserId;
            else if (level == SalesAnalyticsViewLevels.Department)
            {
                if (summary.SaleDataScope == 2 || summary.PurchaseDataScope == 2)
                    resolvedDept = summary.PrimaryDepartmentId;
                else if (!string.IsNullOrWhiteSpace(departmentId))
                {
                    var did = departmentId.Trim();
                    if (!IsDepartmentAllowedForSalesPurchase(summary, did, departments))
                        return new ValidationResult { Ok = false, Error = "无权查看该部门数据" };
                    resolvedDept = did;
                }
                else
                    resolvedDept = summary.PrimaryDepartmentId;
            }

            return new ValidationResult
            {
                Ok = true,
                ViewLevel = level,
                DepartmentId = resolvedDept,
                OwnerUserId = resolvedUser
            };
        }

        if (level == SalesAnalyticsViewLevels.Personal)
        {
            if (summary.FinanceDataScope == 1)
                resolvedUser = summary.UserId;
            else if (!string.IsNullOrWhiteSpace(ownerUserId))
            {
                var uid = ownerUserId.Trim();
                if (salesPurchaseLensUserIds.Count > 0 && !salesPurchaseLensUserIds.Contains(uid))
                    return new ValidationResult { Ok = false, Error = "无权查看该用户财务数据" };
                resolvedUser = uid;
            }
        }
        else if (level == SalesAnalyticsViewLevels.Department)
        {
            if (summary.FinanceDataScope == 2)
                resolvedDept = summary.PrimaryDepartmentId;
            else if (!string.IsNullOrWhiteSpace(departmentId))
            {
                var did = departmentId.Trim();
                if (!IsDepartmentAllowedForFinance(summary, did, departments))
                    return new ValidationResult { Ok = false, Error = "无权查看该部门数据" };
                resolvedDept = did;
            }
            else if (summary.FinanceDataScope is 0 or 3)
                resolvedDept = summary.PrimaryDepartmentId;
            else
                resolvedDept = summary.PrimaryDepartmentId;
        }

        return new ValidationResult
        {
            Ok = true,
            ViewLevel = level,
            DepartmentId = resolvedDept,
            OwnerUserId = resolvedUser
        };
    }

    public static IReadOnlyList<SalesAnalyticsDepartmentOptionDto> BuildAllowedDepartments(
        UserPermissionSummaryDto summary,
        IReadOnlyList<RbacDepartment> departments)
    {
        if (IsSalesPurchaseOnlyMode(summary))
        {
            if (summary.SaleDataScope is 2 or 3)
                return SalesAnalyticsScopeValidator.BuildAllowedDepartments(
                    MapSaleScope(summary), departments);
            return SalesAnalyticsScopeValidator.BuildAllowedDepartments(
                MapPurchaseScope(summary), departments);
        }

        return SalesAnalyticsScopeValidator.BuildAllowedDepartments(
            MapFinanceScopeToSalesScope(summary), departments);
    }

    public static async Task<HashSet<string>> BuildSalesPurchaseLensUserIdsAsync(
        IDataPermissionService dataPermission,
        UserPermissionSummaryDto summary,
        CancellationToken cancellationToken = default)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (summary.IsSysAdmin || summary.FinanceDataScope == 0)
            return ids;

        if (IsSalesPurchaseOnlyMode(summary))
        {
            if (summary.SaleDataScope == 1 || summary.PurchaseDataScope == 1)
                ids.Add(summary.UserId);

            if (summary.SaleDataScope is 2 or 3)
            {
                var saleIds = await dataPermission.GetAllowedUserIdsForDataScopeAsync(
                    MapSaleScope(summary),
                    includeChildren: summary.SaleDataScope == 3,
                    cancellationToken);
                foreach (var id in saleIds) ids.Add(id);
            }

            if (summary.PurchaseDataScope is 2 or 3)
            {
                var purchaseIds = await dataPermission.GetAllowedUserIdsForDataScopeAsync(
                    MapPurchaseScope(summary),
                    includeChildren: summary.PurchaseDataScope == 3,
                    cancellationToken);
                foreach (var id in purchaseIds) ids.Add(id);
            }

            return ids;
        }

        if (summary.FinanceDataScope == 1)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { summary.UserId };

        if (summary.FinanceDataScope is 2 or 3)
        {
            return await dataPermission.GetAllowedUserIdsForDataScopeAsync(
                MapFinanceScopeToSalesScope(summary),
                includeChildren: summary.FinanceDataScope == 3,
                cancellationToken);
        }

        return ids;
    }

    private static IReadOnlyList<string> AllThree() =>
        new[] { SalesAnalyticsViewLevels.Company, SalesAnalyticsViewLevels.Department, SalesAnalyticsViewLevels.Personal };

    private static UserPermissionSummaryDto MapFinanceScopeToSalesScope(UserPermissionSummaryDto summary) =>
        new()
        {
            UserId = summary.UserId,
            IsSysAdmin = summary.IsSysAdmin,
            SaleDataScope = summary.FinanceDataScope,
            PrimaryDepartmentId = summary.PrimaryDepartmentId
        };

    private static UserPermissionSummaryDto MapSaleScope(UserPermissionSummaryDto summary) =>
        new()
        {
            UserId = summary.UserId,
            IsSysAdmin = summary.IsSysAdmin,
            SaleDataScope = summary.SaleDataScope,
            PrimaryDepartmentId = summary.PrimaryDepartmentId
        };

    private static UserPermissionSummaryDto MapPurchaseScope(UserPermissionSummaryDto summary) =>
        new()
        {
            UserId = summary.UserId,
            IsSysAdmin = summary.IsSysAdmin,
            SaleDataScope = summary.PurchaseDataScope,
            PrimaryDepartmentId = summary.PrimaryDepartmentId
        };

    private static bool IsDepartmentAllowedForFinance(
        UserPermissionSummaryDto summary,
        string departmentId,
        IReadOnlyList<RbacDepartment> departments) =>
        SalesAnalyticsScopeValidatorIsDeptAllowed(MapFinanceScopeToSalesScope(summary), departmentId, departments);

    private static bool IsDepartmentAllowedForSalesPurchase(
        UserPermissionSummaryDto summary,
        string departmentId,
        IReadOnlyList<RbacDepartment> departments)
    {
        if (summary.SaleDataScope is 0 or 3)
            return SalesAnalyticsScopeValidatorIsDeptAllowed(MapSaleScope(summary), departmentId, departments);
        if (summary.PurchaseDataScope is 0 or 3)
            return SalesAnalyticsScopeValidatorIsDeptAllowed(MapPurchaseScope(summary), departmentId, departments);
        return string.Equals(departmentId, summary.PrimaryDepartmentId, StringComparison.OrdinalIgnoreCase);
    }

    private static bool SalesAnalyticsScopeValidatorIsDeptAllowed(
        UserPermissionSummaryDto mapped,
        string departmentId,
        IReadOnlyList<RbacDepartment> departments)
    {
        if (string.Equals(departmentId, SalesAnalyticsScopeValidator.UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase))
            return mapped.IsSysAdmin || mapped.SaleDataScope is 0 or 3;

        if (mapped.IsSysAdmin || mapped.SaleDataScope == 0)
            return departments.Any(d => d.Id == departmentId && d.Status == 1);

        if (mapped.SaleDataScope == 3 && !string.IsNullOrWhiteSpace(mapped.PrimaryDepartmentId))
        {
            var current = departments.FirstOrDefault(d => d.Id == mapped.PrimaryDepartmentId);
            if (current == null) return false;
            var target = departments.FirstOrDefault(d => d.Id == departmentId);
            if (target == null) return false;
            if (target.Id == current.Id) return true;
            var prefix = string.IsNullOrWhiteSpace(current.Path) ? null : current.Path + "/";
            return prefix != null && !string.IsNullOrWhiteSpace(target.Path)
                   && target.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(departmentId, mapped.PrimaryDepartmentId, StringComparison.OrdinalIgnoreCase);
    }
}
