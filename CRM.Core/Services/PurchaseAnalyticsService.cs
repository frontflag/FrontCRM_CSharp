using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Rbac;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public sealed class PurchaseAnalyticsService : IPurchaseAnalyticsService
{
    private readonly IRbacService _rbacService;
    private readonly IDataPermissionService _dataPermission;
    private readonly IRepository<RbacDepartment> _departmentRepo;
    private readonly IRepository<RbacUserDepartment> _userDepartmentRepo;
    private readonly IRepository<Models.User> _userRepo;
    private readonly IPurchaseAnalyticsQuery _query;

    public PurchaseAnalyticsService(
        IRbacService rbacService,
        IDataPermissionService dataPermission,
        IRepository<RbacDepartment> departmentRepo,
        IRepository<RbacUserDepartment> userDepartmentRepo,
        IRepository<Models.User> userRepo,
        IPurchaseAnalyticsQuery query)
    {
        _rbacService = rbacService;
        _dataPermission = dataPermission;
        _departmentRepo = departmentRepo;
        _userDepartmentRepo = userDepartmentRepo;
        _userRepo = userRepo;
        _query = query;
    }

    public async Task<(bool Ok, string? Error, PurchaseAnalyticsResolvedScope? Scope)> ResolveScopeAsync(
        string userId,
        PurchaseAnalyticsQueryParams query,
        CancellationToken cancellationToken = default)
    {
        var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
        if (!PurchaseAnalyticsScopeValidator.CanAccessPage(summary))
            return (false, "当前账号无采购数据范围，无法访问采购看板", null);

        var departments = (await _departmentRepo.GetAllAsync()).ToList();
        var userDepartments = (await _userDepartmentRepo.GetAllAsync()).ToList();
        var allowedUserIds = await PurchaseAnalyticsScopeValidator.BuildAllowedPurchaseUserIdsAsync(
            _dataPermission, summary, cancellationToken);

        var validation = PurchaseAnalyticsScopeValidator.Validate(
            summary,
            query.ViewLevel,
            query.DepartmentId,
            query.PurchaseUserId,
            departments,
            userDepartments,
            allowedUserIds);

        if (!validation.Ok)
            return (false, validation.Error, null);

        var primaryDept = departments.FirstOrDefault(d => d.Id == summary.PrimaryDepartmentId);
        var allowedLevels = PurchaseAnalyticsScopeValidator.GetAllowedViewLevels(summary);
        var companyVisible = allowedLevels.Contains(SalesAnalyticsViewLevels.Company, StringComparer.OrdinalIgnoreCase);
        var maskAmounts = ShouldMaskAmounts(summary);

        var dateTo = query.DateTo.HasValue
            ? PurchaseAnalyticsDateFilter.ToUtcDateStart(query.DateTo.Value)
            : PurchaseAnalyticsDateFilter.ToUtcDateStart(DateTime.UtcNow);
        var dateFrom = query.DateFrom.HasValue
            ? PurchaseAnalyticsDateFilter.ToUtcDateStart(query.DateFrom.Value)
            : PurchaseAnalyticsDateFilter.ToUtcDateStart(dateTo.AddMonths(-5));

        var groupBy = NormalizeGroupBy(query.GroupBy);
        var allowedPurchaseUsers = await BuildAllowedPurchaseUsersAsync(summary, allowedUserIds, cancellationToken);
        var canSelectPurchaseUser = CanSelectPurchaseUser(summary);

        var scopeContext = new PurchaseAnalyticsScopeContextDto
        {
            PurchaseDataScope = summary.PurchaseDataScope,
            ViewLevel = validation.ViewLevel,
            ScopeLabel = PurchaseAnalyticsScopeValidator.BuildScopeLabel(summary, primaryDept?.DepartmentName, companyVisible),
            PrimaryDepartmentId = summary.PrimaryDepartmentId,
            PrimaryDepartmentName = primaryDept?.DepartmentName,
            AllowedViewLevels = allowedLevels,
            AllowedDepartments = PurchaseAnalyticsScopeValidator.BuildAllowedDepartments(summary, departments),
            AllowedPurchaseUsers = allowedPurchaseUsers,
            CanSelectPurchaseUser = canSelectPurchaseUser,
            DataFiltered = !summary.IsSysAdmin && summary.PurchaseDataScope != 0,
            MaskAmounts = maskAmounts,
            ResolvedPurchaseUserId = validation.PurchaseUserId,
            ResolvedDepartmentId = validation.DepartmentId
        };

        return (true, null, new PurchaseAnalyticsResolvedScope
        {
            Summary = summary,
            ScopeContext = scopeContext,
            ViewLevel = validation.ViewLevel,
            DepartmentId = validation.DepartmentId,
            PurchaseUserId = validation.PurchaseUserId,
            DateFrom = dateFrom,
            DateTo = dateTo,
            GroupBy = groupBy,
            MaskAmounts = maskAmounts
        });
    }

    public Task<PurchaseAnalyticsDashboardDto> GetDashboardAsync(
        PurchaseAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default) =>
        _query.GetDashboardAsync(scope, cancellationToken);

    public Task<IReadOnlyList<PurchaseAnalyticsTrendPointDto>> GetTrendsAsync(
        PurchaseAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default) =>
        _query.GetTrendsAsync(scope, cancellationToken);

    public Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        PurchaseAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default) =>
        _query.GetBreakdownsAsync(scope, cancellationToken);

    private static string NormalizeGroupBy(string? groupBy) =>
        groupBy?.Trim().ToLowerInvariant() switch
        {
            "day" => "day",
            "week" => "week",
            _ => "month"
        };

    private static bool ShouldMaskAmounts(UserPermissionSummaryDto summary)
    {
        if (summary.IsSysAdmin) return false;
        if (PurchaseSensitiveFieldMask511.ShouldMask(summary)) return true;
        return !HasPermission(summary, "purchase.amount.read");
    }

    private static bool HasPermission(UserPermissionSummaryDto summary, string code)
    {
        if (summary.PermissionCodes == null) return false;
        return summary.PermissionCodes.Contains(code, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<IReadOnlyList<PurchaseAnalyticsPurchaseUserOptionDto>> BuildAllowedPurchaseUsersAsync(
        UserPermissionSummaryDto summary,
        HashSet<string> allowedUserIds,
        CancellationToken cancellationToken)
    {
        if (!CanSelectPurchaseUser(summary))
            return Array.Empty<PurchaseAnalyticsPurchaseUserOptionDto>();

        cancellationToken.ThrowIfCancellationRequested();

        HashSet<string> ids;
        if (allowedUserIds.Count > 0)
        {
            ids = allowedUserIds;
        }
        else if (summary.IsSysAdmin || summary.PurchaseDataScope == 0)
        {
            ids = (await _userRepo.GetAllAsync())
                .Where(u => u.Status != UserAccountStatus.Disabled)
                .Select(u => u.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { summary.UserId };
        }

        return (await _userRepo.GetAllAsync())
            .Where(u => ids.Contains(u.Id) && u.Status != UserAccountStatus.Disabled)
            .OrderBy(u => u.RealName ?? u.UserName)
            .Select(u => new PurchaseAnalyticsPurchaseUserOptionDto
            {
                Id = u.Id,
                Name = u.RealName ?? u.UserName ?? u.Id
            })
            .ToList();
    }

    private static bool CanSelectPurchaseUser(UserPermissionSummaryDto summary)
    {
        if (BusinessDepartmentRules.UsePurchaseOrderAssistorOnlyScope(summary))
            return false;
        if (summary.IsSysAdmin || summary.PurchaseDataScope is 0 or 2 or 3)
            return true;
        return false;
    }
}
