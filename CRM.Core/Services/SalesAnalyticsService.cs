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
    private readonly ISalesAnalyticsQuery _query;

    public SalesAnalyticsService(
        IRbacService rbacService,
        IDataPermissionService dataPermission,
        IRepository<RbacDepartment> departmentRepo,
        IRepository<RbacUserDepartment> userDepartmentRepo,
        ISalesAnalyticsQuery query)
    {
        _rbacService = rbacService;
        _dataPermission = dataPermission;
        _departmentRepo = departmentRepo;
        _userDepartmentRepo = userDepartmentRepo;
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

        var scopeContext = new SalesAnalyticsScopeContextDto
        {
            SaleDataScope = summary.SaleDataScope,
            ViewLevel = validation.ViewLevel,
            ScopeLabel = SalesAnalyticsScopeValidator.BuildScopeLabel(summary, primaryDept?.DepartmentName, companyVisible),
            PrimaryDepartmentId = summary.PrimaryDepartmentId,
            PrimaryDepartmentName = primaryDept?.DepartmentName,
            AllowedViewLevels = allowedLevels,
            AllowedDepartments = SalesAnalyticsScopeValidator.BuildAllowedDepartments(summary, departments),
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

    private static string NormalizeGroupBy(string? groupBy) =>
        groupBy?.Trim().ToLowerInvariant() switch
        {
            "day" => "day",
            "week" => "week",
            _ => "month"
        };
}
