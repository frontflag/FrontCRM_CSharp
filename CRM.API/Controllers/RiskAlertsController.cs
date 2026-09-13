using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.API.Utilities;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/risk-alerts")]
[Authorize]
public class RiskAlertsController : ControllerBase
{
    readonly IRiskAlertService _service;
    readonly IRbacService _rbac;
    readonly ILogger<RiskAlertsController> _logger;

    public RiskAlertsController(
        IRiskAlertService service,
        IRbacService rbac,
        ILogger<RiskAlertsController> logger)
    {
        _service = service;
        _rbac = rbac;
        _logger = logger;
    }

    string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<RiskAlertDashboardDto>>> GetDashboard(
        CancellationToken cancellationToken)
    {
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<RiskAlertDashboardDto>.Fail("未登录", 401));

        try
        {
            var dto = await _service.GetDashboardAsync(uid, cancellationToken);
            await ApplyMaskAsync(dto);
            return Ok(ApiResponse<RiskAlertDashboardDto>.Ok(dto, "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取风险预警失败");
            return StatusCode(500, ApiResponse<RiskAlertDashboardDto>.Fail("读取失败", 500));
        }
    }

    async Task ApplyMaskAsync(RiskAlertDashboardDto dto)
    {
        var mask511 = await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbac, User);
        var mask521 = await SaleMaskHttp.ShouldMaskSale521Async(_rbac, User);
        foreach (var item in dto.Items)
        {
            if (item.Code is RiskAlertItemCodes.InventoryAmount or RiskAlertItemCodes.StockAge)
            {
                if (!mask511) continue;
                item.Masked = true;
                item.ActualUsd = null;
                item.SubjectName = item.SubjectName is null ? null : "—";
                continue;
            }

            if (!mask521) continue;
            item.Masked = true;
            item.ActualUsd = null;
            if (item.Code == RiskAlertItemCodes.CustomerReceivable)
                item.SubjectName = "—";
        }
    }
}
