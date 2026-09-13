using System.Security.Claims;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/risk-alert-params")]
[Authorize]
public class RiskAlertParamsController : ControllerBase
{
    readonly IRiskAlertService _service;
    readonly IRbacService _rbac;
    readonly ILogger<RiskAlertParamsController> _logger;

    public RiskAlertParamsController(
        IRiskAlertService service,
        IRbacService rbac,
        ILogger<RiskAlertParamsController> logger)
    {
        _service = service;
        _rbac = rbac;
        _logger = logger;
    }

    string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet]
    [RequirePermission(SystemPermissionCodes.ParamsRiskAlertRead)]
    public async Task<ActionResult<ApiResponse<RiskAlertSettingsDto>>> Get(CancellationToken cancellationToken)
    {
        if (!await CanMaintainAsync())
            return StatusCode(403, ApiResponse<RiskAlertSettingsDto>.Fail("仅系统管理员或平台管理员可查看预警参数", 403));

        try
        {
            var dto = await _service.GetSettingsAsync(cancellationToken);
            return Ok(ApiResponse<RiskAlertSettingsDto>.Ok(dto, "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取预警参数失败");
            return StatusCode(500, ApiResponse<RiskAlertSettingsDto>.Fail("读取失败", 500));
        }
    }

    [HttpPut]
    [RequirePermission(SystemPermissionCodes.ParamsRiskAlertWrite)]
    public async Task<ActionResult<ApiResponse<RiskAlertSettingsDto>>> Put(
        [FromBody] RiskAlertSettingsPutRequest? body,
        CancellationToken cancellationToken)
    {
        if (body == null)
            return BadRequest(ApiResponse<RiskAlertSettingsDto>.Fail("请求体为空", 400));
        if (!await CanMaintainAsync())
            return StatusCode(403, ApiResponse<RiskAlertSettingsDto>.Fail("仅系统管理员或平台管理员可设置预警参数", 403));

        try
        {
            var dto = await _service.PutSettingsAsync(body, cancellationToken);
            return Ok(ApiResponse<RiskAlertSettingsDto>.Ok(dto, "已保存"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<RiskAlertSettingsDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存预警参数失败");
            return StatusCode(500, ApiResponse<RiskAlertSettingsDto>.Fail("保存失败", 500));
        }
    }

    async Task<bool> CanMaintainAsync()
    {
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return false;
        var summary = await _rbac.GetUserPermissionSummaryAsync(uid);
        return summary.CanForceDelete;
    }
}
