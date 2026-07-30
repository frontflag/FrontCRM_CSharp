using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[RequireAnyPermission("analytics-sales.read", "sales-order.read")]
[ApiController]
[Route("api/v1/analytics/sales")]
public class SalesAnalyticsController : ControllerBase
{
    private readonly ISalesAnalyticsService _service;
    private readonly ISalesAnalyticsReconciliationService _reconciliation;
    private readonly ISalesOrderItemLineListQuery _orderItemLineListQuery;
    private readonly IRfqItemListQuery _rfqItemListQuery;
    private readonly IRbacService _rbacService;

    public SalesAnalyticsController(
        ISalesAnalyticsService service,
        ISalesAnalyticsReconciliationService reconciliation,
        ISalesOrderItemLineListQuery orderItemLineListQuery,
        IRfqItemListQuery rfqItemListQuery,
        IRbacService rbacService)
    {
        _service = service;
        _reconciliation = reconciliation;
        _orderItemLineListQuery = orderItemLineListQuery;
        _rfqItemListQuery = rfqItemListQuery;
        _rbacService = rbacService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? salesUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, salesUserId, dateFrom, dateTo, null, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var data = await _service.GetDashboardAsync(scope!, cancellationToken);
        return Ok(ApiResponse<SalesAnalyticsDashboardDto>.Ok(data));
    }

    [HttpGet("trends")]
    public async Task<IActionResult> GetTrends(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? salesUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] string? groupBy,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, salesUserId, dateFrom, dateTo, groupBy, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var data = await _service.GetTrendsAsync(scope!, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SalesAnalyticsTrendPointDto>>.Ok(data));
    }

    [HttpGet("breakdowns")]
    public async Task<IActionResult> GetBreakdowns(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? salesUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, salesUserId, dateFrom, dateTo, null, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var data = await _service.GetBreakdownsAsync(scope!, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>>.Ok(data));
    }

    /// <summary>客户维看板（成单/复购客户、类型等级行业、客户 Top10）。</summary>
    [HttpGet("customer")]
    public async Task<IActionResult> GetCustomer(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? salesUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, salesUserId, dateFrom, dateTo, null, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var data = await _service.GetCustomerAsync(scope!, cancellationToken);
        return Ok(ApiResponse<SalesAnalyticsCustomerDto>.Ok(data));
    }

    /// <summary>订单明细维看板（与明细列表看板同实现；dataset=reportApproved 成单口径）。</summary>
    [HttpGet("order-items/dashboard")]
    public async Task<IActionResult> GetOrderItemsDashboard(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? salesUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, salesUserId, dateFrom, dateTo, null, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var request = BuildOrderItemsRequest(scope!);
        var data = await _orderItemLineListQuery.GetListAnalyticsDashboardAsync(request, scope!.MaskAmounts, cancellationToken);
        return Ok(ApiResponse<SalesOrderItemListAnalyticsDashboardDto>.Ok(data));
    }

    [HttpGet("order-items/trends")]
    public async Task<IActionResult> GetOrderItemsTrends(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? salesUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] string? groupBy,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, salesUserId, dateFrom, dateTo, groupBy, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var request = BuildOrderItemsRequest(scope!);
        var data = await _orderItemLineListQuery.GetListAnalyticsTrendsAsync(
            request,
            scope!.GroupBy,
            scope.MaskAmounts,
            cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SalesOrderItemListAnalyticsTrendPointDto>>.Ok(data));
    }

    [HttpGet("order-items/breakdowns")]
    public async Task<IActionResult> GetOrderItemsBreakdowns(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? salesUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, salesUserId, dateFrom, dateTo, null, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var request = BuildOrderItemsRequest(scope!);
        var data = await _orderItemLineListQuery.GetListAnalyticsBreakdownsAsync(request, scope!.MaskAmounts, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>>.Ok(data));
    }

    [HttpGet("order-items/rankings")]
    public async Task<IActionResult> GetOrderItemsRankings(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? salesUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, salesUserId, dateFrom, dateTo, null, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var request = BuildOrderItemsRequest(scope!);
        var data = await _orderItemLineListQuery.GetListAnalyticsRankingsAsync(request, scope!.MaskAmounts, cancellationToken);
        return Ok(ApiResponse<SalesOrderItemListAnalyticsRankingsDto>.Ok(data));
    }

    /// <summary>需求明细维看板（与明细列表看板同实现；dataset=reportScope，排除主单已取消）。</summary>
    [HttpGet("rfq-items/dashboard")]
    public async Task<IActionResult> GetRfqItemsDashboard(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? salesUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, salesUserId, dateFrom, dateTo, null, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var (request, maskCustomerNames) = BuildRfqItemsRequest(scope!);
        var data = await _rfqItemListQuery.GetListAnalyticsDashboardAsync(request, maskCustomerNames, cancellationToken);
        return Ok(ApiResponse<RfqListAnalyticsDashboardDto>.Ok(data));
    }

    [HttpGet("rfq-items/trends")]
    public async Task<IActionResult> GetRfqItemsTrends(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? salesUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] string? groupBy,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, salesUserId, dateFrom, dateTo, groupBy, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var (request, _) = BuildRfqItemsRequest(scope!);
        var data = await _rfqItemListQuery.GetListAnalyticsTrendsAsync(request, scope!.GroupBy, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RfqListAnalyticsTrendPointDto>>.Ok(data));
    }

    [HttpGet("rfq-items/breakdowns")]
    public async Task<IActionResult> GetRfqItemsBreakdowns(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? salesUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, salesUserId, dateFrom, dateTo, null, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var (request, _) = BuildRfqItemsRequest(scope!);
        var data = await _rfqItemListQuery.GetListAnalyticsBreakdownsAsync(request, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>>.Ok(data));
    }

    [HttpGet("rfq-items/rankings")]
    public async Task<IActionResult> GetRfqItemsRankings(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? salesUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, salesUserId, dateFrom, dateTo, null, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var (request, maskCustomerNames) = BuildRfqItemsRequest(scope!);
        var data = await _rfqItemListQuery.GetListAnalyticsRankingsAsync(request, maskCustomerNames, cancellationToken);
        return Ok(ApiResponse<RfqItemListAnalyticsRankingsDto>.Ok(data));
    }

    private async Task<(bool Ok, string? Error, SalesAnalyticsResolvedScope? Scope)> ResolveAsync(
        string? viewLevel,
        string? departmentId,
        string? salesUserId,
        string? dateFrom,
        string? dateTo,
        string? groupBy,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return (false, "未登录或登录态失效", null);

        var query = new SalesAnalyticsQueryParams
        {
            ViewLevel = string.IsNullOrWhiteSpace(viewLevel) ? SalesAnalyticsViewLevels.Company : viewLevel.Trim(),
            DepartmentId = departmentId,
            SalesUserId = salesUserId,
            DateFrom = ParseQueryDate(dateFrom),
            DateTo = ParseQueryDate(dateTo),
            GroupBy = string.IsNullOrWhiteSpace(groupBy) ? "month" : groupBy.Trim()
        };

        return await _service.ResolveScopeAsync(userId, query, cancellationToken);
    }

    private static SellOrderItemLineQueryRequest BuildOrderItemsRequest(SalesAnalyticsResolvedScope scope)
    {
        var viewLevel = scope.ViewLevel?.Trim().ToLowerInvariant() ?? SalesAnalyticsViewLevels.Company;
        return new SellOrderItemLineQueryRequest
        {
            OrderCreateStart = scope.DateFrom,
            OrderCreateEnd = scope.DateTo,
            AnalyticsDataset = SalesOrderItemAnalyticsDatasets.ReportApproved,
            AnalyticsViewLevel = viewLevel,
            AnalyticsDepartmentId = viewLevel == SalesAnalyticsViewLevels.Department ? scope.DepartmentId : null,
            SalesUserId = viewLevel == SalesAnalyticsViewLevels.Personal ? scope.SalesUserId : null,
            CurrentUserId = scope.Summary.UserId
        };
    }

    private static (RFQItemQueryRequest Request, bool MaskCustomerNames) BuildRfqItemsRequest(
        SalesAnalyticsResolvedScope scope)
    {
        var viewLevel = scope.ViewLevel?.Trim().ToLowerInvariant() ?? SalesAnalyticsViewLevels.Company;
        var mask521 = SaleSensitiveFieldMask521.ShouldMask(scope.Summary);
        var canViewCustomer = !mask521 && (scope.Summary.IsSysAdmin
            || (scope.Summary.PermissionCodes?.Contains("customer.info.read") ?? false));

        var request = new RFQItemQueryRequest
        {
            StartDate = scope.DateFrom,
            EndDate = scope.DateTo,
            AnalyticsDataset = RfqItemAnalyticsDatasets.ReportScope,
            AnalyticsViewLevel = viewLevel,
            AnalyticsDepartmentId = viewLevel == SalesAnalyticsViewLevels.Department ? scope.DepartmentId : null,
            SalesUserId = viewLevel == SalesAnalyticsViewLevels.Personal ? scope.SalesUserId : null,
            CurrentUserId = scope.Summary.UserId,
            CanViewCustomerInList = canViewCustomer
        };

        return (request, !canViewCustomer);
    }

    /// <summary>看板指标与列表基线对账（SYS_ADMIN 或联调账号自用）。</summary>
    [HttpGet("reconcile")]
    public async Task<IActionResult> Reconcile(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? salesUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] string? targetUserId,
        CancellationToken cancellationToken = default)
    {
        var callerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(callerId))
            return Unauthorized(ApiResponse<object>.Fail("未登录或登录态失效", 401));

        var summary = await _rbacService.GetUserPermissionSummaryAsync(callerId);
        if (!summary.IsSysAdmin && string.IsNullOrWhiteSpace(targetUserId))
        {
            // 非超管仅允许对账自己的看板
        }
        else if (!summary.IsSysAdmin && !string.IsNullOrWhiteSpace(targetUserId)
                 && !string.Equals(targetUserId, callerId, StringComparison.OrdinalIgnoreCase))
        {
            return Forbidden("仅 SYS_ADMIN 可为其他用户对账");
        }

        var subjectId = !string.IsNullOrWhiteSpace(targetUserId) ? targetUserId.Trim() : callerId;
        var query = new SalesAnalyticsQueryParams
        {
            ViewLevel = string.IsNullOrWhiteSpace(viewLevel) ? SalesAnalyticsViewLevels.Personal : viewLevel.Trim(),
            DepartmentId = departmentId,
            SalesUserId = salesUserId,
            DateFrom = ParseQueryDate(dateFrom),
            DateTo = ParseQueryDate(dateTo)
        };

        try
        {
            var report = await _reconciliation.ReconcileAsync(subjectId, query, cancellationToken);
            return Ok(ApiResponse<SalesAnalyticsReconciliationReportDto>.Ok(report));
        }
        catch (InvalidOperationException ex)
        {
            return Forbidden(ex.Message);
        }
    }

    /// <summary>抽样账号批量对账（SYS_ADMIN）。</summary>
    [HttpGet("reconcile/samples")]
    public async Task<IActionResult> ReconcileSamples(
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken cancellationToken = default)
    {
        var callerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(callerId))
            return Unauthorized(ApiResponse<object>.Fail("未登录或登录态失效", 401));

        var summary = await _rbacService.GetUserPermissionSummaryAsync(callerId);
        if (!summary.IsSysAdmin)
            return Forbidden("仅 SYS_ADMIN 可执行批量对账");

        var reports = await _reconciliation.ReconcileSampleUsersAsync(
            callerId,
            ParseQueryDate(dateFrom),
            ParseQueryDate(dateTo),
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<SalesAnalyticsReconciliationReportDto>>.Ok(reports));
    }

    private static DateTime? ParseQueryDate(string? value) =>
        DateTime.TryParse(value, out var d) ? SalesAnalyticsDateFilter.ToUtcDateStart(d) : null;

    private IActionResult Forbidden(string? message) =>
        new ObjectResult(ApiResponse<object>.Fail(message ?? "无权访问", 403)) { StatusCode = 403 };
}
