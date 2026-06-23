using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[RequireAnyPermission("analytics-logistics.read", "inventory.read")]
[ApiController]
[Route("api/v1/analytics/logistics")]
public class LogisticsAnalyticsController : ControllerBase
{
    private readonly ILogisticsAnalyticsService _service;

    public LogisticsAnalyticsController(ILogisticsAnalyticsService service)
    {
        _service = service;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? ownerUserId,
        [FromQuery] string? inventoryType,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] string? warehouseId,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(
            viewLevel, departmentId, ownerUserId, inventoryType, null, dateFrom, dateTo, null, warehouseId, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var data = await _service.GetDashboardAsync(scope!, cancellationToken);
        return Ok(ApiResponse<LogisticsAnalyticsDashboardDto>.Ok(data));
    }

    [HttpGet("trends")]
    public async Task<IActionResult> GetTrends(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? ownerUserId,
        [FromQuery] string? inventoryType,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] string? groupBy,
        [FromQuery] string? warehouseId,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(
            viewLevel, departmentId, ownerUserId, inventoryType, null, dateFrom, dateTo, groupBy, warehouseId, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var data = await _service.GetTrendsAsync(scope!, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<LogisticsAnalyticsTrendPointDto>>.Ok(data));
    }

    [HttpGet("breakdowns")]
    public async Task<IActionResult> GetBreakdowns(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? ownerUserId,
        [FromQuery] string? inventoryType,
        [FromQuery] string? dateTo,
        [FromQuery] string? warehouseId,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(
            viewLevel, departmentId, ownerUserId, inventoryType, null, null, dateTo, null, warehouseId, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var data = await _service.GetBreakdownsAsync(scope!, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>>.Ok(data));
    }

    [HttpGet("customer-matrix")]
    public async Task<IActionResult> GetCustomerMatrix(
        [FromQuery] string matrixSubject,
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? ownerUserId,
        [FromQuery] string? inventoryType,
        [FromQuery] string? dateTo,
        [FromQuery] string? warehouseId,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(
            viewLevel, departmentId, ownerUserId, inventoryType, matrixSubject, null, dateTo, null, warehouseId, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var data = await _service.GetCustomerMatrixAsync(scope!, cancellationToken);
        return Ok(ApiResponse<LogisticsAnalyticsCustomerMatrixDto>.Ok(data));
    }

    private async Task<(bool Ok, string? Error, LogisticsAnalyticsResolvedScope? Scope)> ResolveAsync(
        string? viewLevel,
        string? departmentId,
        string? ownerUserId,
        string? inventoryType,
        string? matrixSubject,
        string? dateFrom,
        string? dateTo,
        string? groupBy,
        string? warehouseId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return (false, "未登录或登录态失效", null);

        var query = new LogisticsAnalyticsQueryParams
        {
            ViewLevel = string.IsNullOrWhiteSpace(viewLevel) ? SalesAnalyticsViewLevels.Company : viewLevel.Trim(),
            DepartmentId = departmentId,
            OwnerUserId = ownerUserId,
            InventoryType = string.IsNullOrWhiteSpace(inventoryType) ? LogisticsAnalyticsInventoryTypes.All : inventoryType.Trim(),
            MatrixSubject = matrixSubject,
            DateFrom = ParseQueryDate(dateFrom),
            DateTo = ParseQueryDate(dateTo),
            GroupBy = string.IsNullOrWhiteSpace(groupBy) ? "month" : groupBy.Trim(),
            WarehouseId = warehouseId
        };

        return await _service.ResolveScopeAsync(userId, query, cancellationToken);
    }

    private static DateTime? ParseQueryDate(string? value) =>
        DateTime.TryParse(value, out var d) ? SalesAnalyticsDateFilter.ToUtcDateStart(d) : null;

    private IActionResult Forbidden(string? message) =>
        new ObjectResult(ApiResponse<object>.Fail(message ?? "无权访问", 403)) { StatusCode = 403 };
}
