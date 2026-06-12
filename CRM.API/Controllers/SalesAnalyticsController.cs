using CRM.API.Authorization;
using CRM.API.Models.DTOs;
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
    private readonly IRbacService _rbacService;

    public SalesAnalyticsController(
        ISalesAnalyticsService service,
        ISalesAnalyticsReconciliationService reconciliation,
        IRbacService rbacService)
    {
        _service = service;
        _reconciliation = reconciliation;
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
