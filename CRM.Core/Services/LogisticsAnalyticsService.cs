using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Rbac;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public sealed class LogisticsAnalyticsService : ILogisticsAnalyticsService
{
    private readonly IRbacService _rbacService;
    private readonly IDataPermissionService _dataPermission;
    private readonly IRepository<RbacDepartment> _departmentRepo;
    private readonly IRepository<RbacUserDepartment> _userDepartmentRepo;
    private readonly ILogisticsAnalyticsQuery _query;

    public LogisticsAnalyticsService(
        IRbacService rbacService,
        IDataPermissionService dataPermission,
        IRepository<RbacDepartment> departmentRepo,
        IRepository<RbacUserDepartment> userDepartmentRepo,
        ILogisticsAnalyticsQuery query)
    {
        _rbacService = rbacService;
        _dataPermission = dataPermission;
        _departmentRepo = departmentRepo;
        _userDepartmentRepo = userDepartmentRepo;
        _query = query;
    }

    public async Task<(bool Ok, string? Error, LogisticsAnalyticsResolvedScope? Scope)> ResolveScopeAsync(
        string userId,
        LogisticsAnalyticsQueryParams query,
        CancellationToken cancellationToken = default)
    {
        var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
        if (!LogisticsAnalyticsScopeValidator.CanAccessPage(summary))
            return (false, "当前账号无库存分析数据范围，无法访问物流看板", null);

        var departments = (await _departmentRepo.GetAllAsync()).ToList();
        var userDepartments = (await _userDepartmentRepo.GetAllAsync()).ToList();
        var lensUserIds = await LogisticsAnalyticsScopeValidator.BuildSalesPurchaseLensUserIdsAsync(
            _dataPermission, summary, cancellationToken);

        var validation = LogisticsAnalyticsScopeValidator.Validate(
            summary,
            query.ViewLevel,
            query.DepartmentId,
            query.OwnerUserId,
            departments,
            userDepartments,
            lensUserIds);

        if (!validation.Ok)
            return (false, validation.Error, null);

        var inventoryType = NormalizeInventoryType(query.InventoryType);
        if (!string.IsNullOrWhiteSpace(query.MatrixSubject)
            && !LogisticsAnalyticsMatrixSubjects.IsValid(query.MatrixSubject))
            return (false, "matrixSubject 无效", null);

        var primaryDept = departments.FirstOrDefault(d => d.Id == summary.PrimaryDepartmentId);
        var allowedLevels = LogisticsAnalyticsScopeValidator.GetAllowedViewLevels(summary);
        var accessMode = LogisticsAnalyticsScopeValidator.IsSalesPurchaseOnlyMode(summary)
            ? LogisticsAnalyticsAccessModes.SalesPurchaseOnly
            : LogisticsAnalyticsAccessModes.Logistics;
        var companyVisible = allowedLevels.Contains(SalesAnalyticsViewLevels.Company, StringComparer.OrdinalIgnoreCase);
        var maskAmounts = ShouldMaskAmounts(summary);

        var dateTo = query.DateTo.HasValue
            ? SalesAnalyticsDateFilter.ToUtcDateStart(query.DateTo.Value)
            : SalesAnalyticsDateFilter.ToUtcDateStart(DateTime.UtcNow);
        var dateFrom = query.DateFrom.HasValue
            ? SalesAnalyticsDateFilter.ToUtcDateStart(query.DateFrom.Value)
            : SalesAnalyticsDateFilter.ToUtcDateStart(dateTo.AddMonths(-5));

        var scopeContext = new LogisticsAnalyticsScopeContextDto
        {
            LogisticsDataScope = summary.LogisticsDataScope,
            SaleDataScope = summary.SaleDataScope,
            PurchaseDataScope = summary.PurchaseDataScope,
            AccessMode = accessMode,
            ViewLevel = validation.ViewLevel,
            ScopeLabel = LogisticsAnalyticsScopeValidator.BuildScopeLabel(summary, primaryDept?.DepartmentName, companyVisible),
            InventoryType = inventoryType,
            PrimaryDepartmentId = summary.PrimaryDepartmentId,
            PrimaryDepartmentName = primaryDept?.DepartmentName,
            AllowedViewLevels = allowedLevels,
            AllowedDepartments = LogisticsAnalyticsScopeValidator.BuildAllowedDepartments(summary, departments),
            DataFiltered = !summary.IsSysAdmin && accessMode == LogisticsAnalyticsAccessModes.SalesPurchaseOnly,
            MaskAmounts = maskAmounts,
            ResolvedOwnerUserId = validation.OwnerUserId,
            ResolvedDepartmentId = validation.DepartmentId
        };

        return (true, null, new LogisticsAnalyticsResolvedScope
        {
            Summary = summary,
            ScopeContext = scopeContext,
            AccessMode = accessMode,
            ViewLevel = validation.ViewLevel,
            DepartmentId = validation.DepartmentId,
            OwnerUserId = validation.OwnerUserId,
            InventoryType = inventoryType,
            MatrixSubject = string.IsNullOrWhiteSpace(query.MatrixSubject) ? null : query.MatrixSubject.Trim().ToLowerInvariant(),
            DateFrom = dateFrom,
            DateTo = dateTo,
            GroupBy = NormalizeGroupBy(query.GroupBy),
            WarehouseId = string.IsNullOrWhiteSpace(query.WarehouseId) ? null : query.WarehouseId.Trim(),
            MaskAmounts = maskAmounts,
            SalesPurchaseLensUserIds = lensUserIds
        });
    }

    public Task<LogisticsAnalyticsDashboardDto> GetDashboardAsync(
        LogisticsAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default) =>
        _query.GetDashboardAsync(scope, cancellationToken);

    public Task<IReadOnlyList<LogisticsAnalyticsTrendPointDto>> GetTrendsAsync(
        LogisticsAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default) =>
        _query.GetTrendsAsync(scope, cancellationToken);

    public Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        LogisticsAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default) =>
        _query.GetBreakdownsAsync(scope, cancellationToken);

    public Task<LogisticsAnalyticsCustomerMatrixDto> GetCustomerMatrixAsync(
        LogisticsAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scope.MatrixSubject)
            || !LogisticsAnalyticsMatrixSubjects.IsValid(scope.MatrixSubject))
            throw new InvalidOperationException("customer-matrix 需要有效的 matrixSubject");
        return _query.GetCustomerMatrixAsync(scope, cancellationToken);
    }

    private static string NormalizeInventoryType(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "customerorder" => LogisticsAnalyticsInventoryTypes.CustomerOrder,
            "purchasestock" => LogisticsAnalyticsInventoryTypes.PurchaseStock,
            "all" => LogisticsAnalyticsInventoryTypes.All,
            _ => LogisticsAnalyticsInventoryTypes.All
        };

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
        if (summary.PermissionCodes == null) return true;
        return !summary.PermissionCodes.Contains("purchase.amount.read", StringComparer.OrdinalIgnoreCase);
    }
}
