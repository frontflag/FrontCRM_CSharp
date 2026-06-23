using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Rbac;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public sealed class FinanceAnalyticsService : IFinanceAnalyticsService
{
    private readonly IRbacService _rbacService;
    private readonly IDataPermissionService _dataPermission;
    private readonly IRepository<RbacDepartment> _departmentRepo;
    private readonly IRepository<RbacUserDepartment> _userDepartmentRepo;
    private readonly IFinanceExchangeRateService _exchangeRateService;
    private readonly IFinanceAnalyticsQuery _query;

    public FinanceAnalyticsService(
        IRbacService rbacService,
        IDataPermissionService dataPermission,
        IRepository<RbacDepartment> departmentRepo,
        IRepository<RbacUserDepartment> userDepartmentRepo,
        IFinanceExchangeRateService exchangeRateService,
        IFinanceAnalyticsQuery query)
    {
        _rbacService = rbacService;
        _dataPermission = dataPermission;
        _departmentRepo = departmentRepo;
        _userDepartmentRepo = userDepartmentRepo;
        _exchangeRateService = exchangeRateService;
        _query = query;
    }

    public async Task<(bool Ok, string? Error, FinanceAnalyticsResolvedScope? Scope)> ResolveScopeAsync(
        string userId,
        FinanceAnalyticsQueryParams query,
        CancellationToken cancellationToken = default)
    {
        var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
        if (!FinanceAnalyticsScopeValidator.CanAccessPage(summary))
            return (false, "当前账号无财务分析数据范围，无法访问财务看板", null);

        var departments = (await _departmentRepo.GetAllAsync()).ToList();
        var userDepartments = (await _userDepartmentRepo.GetAllAsync()).ToList();
        var lensUserIds = await FinanceAnalyticsScopeValidator.BuildSalesPurchaseLensUserIdsAsync(
            _dataPermission, summary, cancellationToken);

        var validation = FinanceAnalyticsScopeValidator.Validate(
            summary,
            query.ViewLevel,
            query.DepartmentId,
            query.OwnerUserId,
            departments,
            userDepartments,
            lensUserIds);

        if (!validation.Ok)
            return (false, validation.Error, null);

        var primaryDept = departments.FirstOrDefault(d => d.Id == summary.PrimaryDepartmentId);
        var allowedLevels = FinanceAnalyticsScopeValidator.GetAllowedViewLevels(summary);
        var accessMode = FinanceAnalyticsScopeValidator.IsSalesPurchaseOnlyMode(summary)
            ? FinanceAnalyticsAccessModes.SalesPurchaseOnly
            : FinanceAnalyticsAccessModes.Finance;
        var companyVisible = allowedLevels.Contains(SalesAnalyticsViewLevels.Company, StringComparer.OrdinalIgnoreCase);
        var maskAmounts = ShouldMaskAmounts(summary);
        var rates = await _exchangeRateService.GetCurrentAsync(cancellationToken);

        var dateTo = query.DateTo.HasValue
            ? SalesAnalyticsDateFilter.ToUtcDateStart(query.DateTo.Value)
            : SalesAnalyticsDateFilter.ToUtcDateStart(DateTime.UtcNow);
        var dateFrom = query.DateFrom.HasValue
            ? SalesAnalyticsDateFilter.ToUtcDateStart(query.DateFrom.Value)
            : SalesAnalyticsDateFilter.ToUtcDateStart(dateTo.AddMonths(-5));

        var scopeContext = new FinanceAnalyticsScopeContextDto
        {
            FinanceDataScope = summary.FinanceDataScope,
            SaleDataScope = summary.SaleDataScope,
            PurchaseDataScope = summary.PurchaseDataScope,
            AccessMode = accessMode,
            ViewLevel = validation.ViewLevel,
            ScopeLabel = FinanceAnalyticsScopeValidator.BuildScopeLabel(summary, primaryDept?.DepartmentName, companyVisible),
            PrimaryDepartmentId = summary.PrimaryDepartmentId,
            PrimaryDepartmentName = primaryDept?.DepartmentName,
            AllowedViewLevels = allowedLevels,
            AllowedDepartments = FinanceAnalyticsScopeValidator.BuildAllowedDepartments(summary, departments),
            DataFiltered = !summary.IsSysAdmin && accessMode == FinanceAnalyticsAccessModes.SalesPurchaseOnly,
            MaskAmounts = maskAmounts,
            ExchangeRateHint = "美元折算按查询日财务参数汇率",
            ResolvedOwnerUserId = validation.OwnerUserId,
            ResolvedDepartmentId = validation.DepartmentId
        };

        return (true, null, new FinanceAnalyticsResolvedScope
        {
            Summary = summary,
            ScopeContext = scopeContext,
            AccessMode = accessMode,
            ViewLevel = validation.ViewLevel,
            DepartmentId = validation.DepartmentId,
            OwnerUserId = validation.OwnerUserId,
            DateFrom = dateFrom,
            DateTo = dateTo,
            GroupBy = NormalizeGroupBy(query.GroupBy),
            MaskAmounts = maskAmounts,
            SalesPurchaseLensUserIds = lensUserIds,
            UsdToCny = rates.UsdToCny,
            UsdToHkd = rates.UsdToHkd,
            UsdToEur = rates.UsdToEur
        });
    }

    public Task<FinanceAnalyticsDashboardDto> GetDashboardAsync(
        FinanceAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default) =>
        _query.GetDashboardAsync(scope, cancellationToken);

    public Task<IReadOnlyList<FinanceAnalyticsTrendPointDto>> GetTrendsAsync(
        FinanceAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default) =>
        _query.GetTrendsAsync(scope, cancellationToken);

    public Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        FinanceAnalyticsResolvedScope scope,
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
        if (SaleSensitiveFieldMask521.ShouldMask(summary)) return true;
        if (summary.PermissionCodes == null) return true;

        return !summary.PermissionCodes.Contains("purchase.amount.read", StringComparer.OrdinalIgnoreCase)
               && !summary.PermissionCodes.Contains("sales.amount.read", StringComparer.OrdinalIgnoreCase)
               && !summary.PermissionCodes.Contains("finance-payment.read", StringComparer.OrdinalIgnoreCase)
               && !summary.PermissionCodes.Contains("finance-receipt.read", StringComparer.OrdinalIgnoreCase);
    }
}
