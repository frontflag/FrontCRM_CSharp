using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[RequireAnyPermission(
    "analytics-finance.read",
    "finance-payment.read",
    "finance-receipt.read",
    "finance-purchase-invoice.read",
    "finance-sell-invoice.read")]
[ApiController]
[Route("api/v1/analytics/finance")]
public class FinanceAnalyticsController : ControllerBase
{
    private readonly IFinanceAnalyticsService _service;

    public FinanceAnalyticsController(IFinanceAnalyticsService service)
    {
        _service = service;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? ownerUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(
            viewLevel, departmentId, ownerUserId, dateFrom, dateTo, null, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var data = await _service.GetDashboardAsync(scope!, cancellationToken);
        return Ok(ApiResponse<FinanceAnalyticsDashboardDto>.Ok(data));
    }

    [HttpGet("trends")]
    public async Task<IActionResult> GetTrends(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? ownerUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] string? groupBy,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(
            viewLevel, departmentId, ownerUserId, dateFrom, dateTo, groupBy, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var data = await _service.GetTrendsAsync(scope!, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<FinanceAnalyticsTrendPointDto>>.Ok(data));
    }

    [HttpGet("breakdowns")]
    public async Task<IActionResult> GetBreakdowns(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? ownerUserId,
        [FromQuery] string? dateTo,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(
            viewLevel, departmentId, ownerUserId, null, dateTo, null, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var data = await _service.GetBreakdownsAsync(scope!, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>>.Ok(data));
    }

    private async Task<(bool Ok, string? Error, FinanceAnalyticsResolvedScope? Scope)> ResolveAsync(
        string? viewLevel,
        string? departmentId,
        string? ownerUserId,
        string? dateFrom,
        string? dateTo,
        string? groupBy,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return (false, "未登录或登录态失效", null);

        var query = new FinanceAnalyticsQueryParams
        {
            ViewLevel = string.IsNullOrWhiteSpace(viewLevel) ? SalesAnalyticsViewLevels.Company : viewLevel.Trim(),
            DepartmentId = departmentId,
            OwnerUserId = ownerUserId,
            DateFrom = ParseQueryDate(dateFrom),
            DateTo = ParseQueryDate(dateTo),
            GroupBy = string.IsNullOrWhiteSpace(groupBy) ? "month" : groupBy.Trim()
        };

        return await _service.ResolveScopeAsync(userId, query, cancellationToken);
    }

    private static DateTime? ParseQueryDate(string? value) =>
        DateTime.TryParse(value, out var d) ? SalesAnalyticsDateFilter.ToUtcDateStart(d) : null;

    private IActionResult Forbidden(string? message) =>
        new ObjectResult(ApiResponse<object>.Fail(message ?? "无权访问", 403)) { StatusCode = 403 };
}
