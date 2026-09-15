using System.Security.Claims;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/dashboard/ops")]
public class DashboardOpsOverviewController : ControllerBase
{
    private readonly IDashboardOpsOverviewQuery _query;
    private readonly IRbacService _rbacService;
    private readonly ILogger<DashboardOpsOverviewController> _logger;

    public DashboardOpsOverviewController(
        IDashboardOpsOverviewQuery query,
        IRbacService rbacService,
        ILogger<DashboardOpsOverviewController> logger)
    {
        _query = query;
        _rbacService = rbacService;
        _logger = logger;
    }

    [HttpGet("logistics")]
    [RequireAnyPermission(
        "analytics-logistics.read",
        "inventory.read",
        "purchase-order.read",
        "sales-order.read")]
    public async Task<IActionResult> GetLogistics(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var range = ResolveRange(dateFrom, dateTo);
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var data = await _query.GetLogisticsAsync(userId, range, cancellationToken);
            return Ok(ApiResponse<DashboardLogisticsOverviewDto>.Ok(data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "控制台物流总览失败");
            return StatusCode(500, ApiResponse<DashboardLogisticsOverviewDto>.Fail(ex.Message, 500));
        }
    }

    [HttpGet("finance-write-offs")]
    [RequireAnyPermission(
        "analytics-finance.read",
        "finance-payment.read",
        "finance-receipt.read",
        "finance-purchase-invoice.read",
        "finance-sell-invoice.read")]
    public async Task<IActionResult> GetFinanceWriteOffs(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var range = ResolveRange(dateFrom, dateTo);
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var flags = new DashboardFinanceWriteOffFlags();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var summary = await _rbacService.GetUserPermissionSummaryAsync(userId.Trim());
                flags.IncludePurchaseInvoice = HasCode(summary, "finance-purchase-invoice.read");
                flags.IncludeReceivable = HasCode(summary, "finance-receipt.read");
                flags.IncludeSellInvoice = HasCode(summary, "finance-sell-invoice.read");
            }

            var data = await _query.GetFinanceWriteOffsAsync(userId, range, flags, cancellationToken);
            return Ok(ApiResponse<DashboardFinanceWriteOffOverviewDto>.Ok(data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "控制台财务核销总览失败");
            return StatusCode(500, ApiResponse<DashboardFinanceWriteOffOverviewDto>.Fail(ex.Message, 500));
        }
    }

    private static DashboardOpsDateRange ResolveRange(DateTime? dateFrom, DateTime? dateTo)
    {
        var to = (dateTo ?? DateTime.UtcNow).Date;
        var from = (dateFrom ?? to.AddDays(-29)).Date;
        if (to < from)
            (from, to) = (to, from);
        return new DashboardOpsDateRange { DateFrom = from, DateTo = to };
    }

    private static bool HasCode(UserPermissionSummaryDto summary, string code) =>
        summary.IsSysAdmin
        || summary.IsSysManager
        || summary.HasBizDataBypass
        || summary.PermissionCodes.Any(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase));
}
