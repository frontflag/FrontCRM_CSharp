using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Rbac;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public sealed class SalesAnalyticsService : ISalesAnalyticsService
{
    private readonly IRbacService _rbacService;
    private readonly IDataPermissionService _dataPermission;
    private readonly IRepository<RbacDepartment> _departmentRepo;
    private readonly IRepository<RbacUserDepartment> _userDepartmentRepo;
    private readonly IRepository<Models.User> _userRepo;
    private readonly ISalesAnalyticsQuery _query;

    public SalesAnalyticsService(
        IRbacService rbacService,
        IDataPermissionService dataPermission,
        IRepository<RbacDepartment> departmentRepo,
        IRepository<RbacUserDepartment> userDepartmentRepo,
        IRepository<Models.User> userRepo,
        ISalesAnalyticsQuery query)
    {
        _rbacService = rbacService;
        _dataPermission = dataPermission;
        _departmentRepo = departmentRepo;
        _userDepartmentRepo = userDepartmentRepo;
        _userRepo = userRepo;
        _query = query;
    }

    public async Task<(bool Ok, string? Error, SalesAnalyticsResolvedScope? Scope)> ResolveScopeAsync(
        string userId,
        SalesAnalyticsQueryParams query,
        CancellationToken cancellationToken = default)
    {
        var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
        if (!SalesAnalyticsScopeValidator.CanAccessPage(summary))
            return (false, "当前账号无销售数据范围，无法访问销售看板", null);

        var departments = (await _departmentRepo.GetAllAsync()).ToList();
        var userDepartments = (await _userDepartmentRepo.GetAllAsync()).ToList();
        var allowedUserIds = await SalesAnalyticsScopeValidator.BuildAllowedSalesUserIdsAsync(
            _dataPermission, summary, cancellationToken);

        var validation = SalesAnalyticsScopeValidator.Validate(
            summary,
            query.ViewLevel,
            query.DepartmentId,
            query.SalesUserId,
            departments,
            userDepartments,
            allowedUserIds);

        if (!validation.Ok)
            return (false, validation.Error, null);

        var primaryDept = departments.FirstOrDefault(d => d.Id == summary.PrimaryDepartmentId);
        var allowedLevels = SalesAnalyticsScopeValidator.GetAllowedViewLevels(summary);
        var companyVisible = allowedLevels.Contains(SalesAnalyticsViewLevels.Company, StringComparer.OrdinalIgnoreCase);
        var maskAmounts = SaleSensitiveFieldMask521.ShouldMask(summary);

        var dateTo = query.DateTo.HasValue
            ? SalesAnalyticsDateFilter.ToUtcDateStart(query.DateTo.Value)
            : SalesAnalyticsDateFilter.ToUtcDateStart(DateTime.UtcNow);
        var dateFrom = query.DateFrom.HasValue
            ? SalesAnalyticsDateFilter.ToUtcDateStart(query.DateFrom.Value)
            : SalesAnalyticsDateFilter.ToUtcDateStart(dateTo.AddMonths(-5));

        var groupBy = NormalizeGroupBy(query.GroupBy);

        var allowedSalesUsers = await BuildAllowedSalesUsersAsync(summary, allowedUserIds, cancellationToken);

        var scopeContext = new SalesAnalyticsScopeContextDto
        {
            SaleDataScope = summary.SaleDataScope,
            ViewLevel = validation.ViewLevel,
            ScopeLabel = SalesAnalyticsScopeValidator.BuildScopeLabel(summary, primaryDept?.DepartmentName, companyVisible),
            PrimaryDepartmentId = summary.PrimaryDepartmentId,
            PrimaryDepartmentName = primaryDept?.DepartmentName,
            AllowedViewLevels = allowedLevels,
            AllowedDepartments = SalesAnalyticsScopeValidator.BuildAllowedDepartments(summary, departments),
            AllowedSalesUsers = allowedSalesUsers,
            DataFiltered = !summary.IsSysAdmin && summary.SaleDataScope != 0,
            MaskAmounts = maskAmounts,
            ResolvedSalesUserId = validation.SalesUserId,
            ResolvedDepartmentId = validation.DepartmentId
        };

        return (true, null, new SalesAnalyticsResolvedScope
        {
            Summary = summary,
            ScopeContext = scopeContext,
            ViewLevel = validation.ViewLevel,
            DepartmentId = validation.DepartmentId,
            SalesUserId = validation.SalesUserId,
            DateFrom = dateFrom,
            DateTo = dateTo,
            GroupBy = groupBy,
            MaskAmounts = maskAmounts
        });
    }

    public Task<SalesAnalyticsDashboardDto> GetDashboardAsync(
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default) =>
        _query.GetDashboardAsync(scope, cancellationToken);

    public Task<IReadOnlyList<SalesAnalyticsTrendPointDto>> GetTrendsAsync(
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default) =>
        _query.GetTrendsAsync(scope, cancellationToken);

    public Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default) =>
        _query.GetBreakdownsAsync(scope, cancellationToken);

    public Task<SalesAnalyticsCustomerDto> GetCustomerAsync(
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default) =>
        _query.GetCustomerAsync(scope, cancellationToken);

    private static string NormalizeGroupBy(string? groupBy) =>
        groupBy?.Trim().ToLowerInvariant() switch
        {
            "day" => "day",
            "week" => "week",
            _ => "month"
        };

    private async Task<IReadOnlyList<SalesAnalyticsSalesUserOptionDto>> BuildAllowedSalesUsersAsync(
        UserPermissionSummaryDto summary,
        HashSet<string> allowedUserIds,
        CancellationToken cancellationToken)
    {
        if (summary.SaleDataScope == 1 || BusinessDepartmentRules.UseSellOrderAssistorOnlyScope(summary))
            return Array.Empty<SalesAnalyticsSalesUserOptionDto>();

        cancellationToken.ThrowIfCancellationRequested();

        HashSet<string> ids;
        if (allowedUserIds.Count > 0)
        {
            ids = allowedUserIds;
        }
        else if (summary.IsSysAdmin || summary.SaleDataScope == 0)
        {
            ids = (await _userRepo.GetAllAsync())
                .Where(u => u.Status == 1)
                .Select(u => u.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { summary.UserId };
        }

        return (await _userRepo.GetAllAsync())
            .Where(u => ids.Contains(u.Id))
            .OrderBy(u => u.RealName ?? u.UserName)
            .Select(u => new SalesAnalyticsSalesUserOptionDto
            {
                Id = u.Id,
                Name = u.RealName ?? u.UserName ?? u.Id
            })
            .ToList();
    }
}
