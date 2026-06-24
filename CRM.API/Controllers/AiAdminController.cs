using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/ai/admin")]
[Route("api/v1/ai/mgmt")]
[Authorize]
public class AiAdminController : ControllerBase
{
    private readonly IAiAdminService _adminService;
    private readonly ILogger<AiAdminController> _logger;

    public AiAdminController(IAiAdminService adminService, ILogger<AiAdminController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    [HttpGet("providers")]
    [RequirePermission("biz.ai.admin")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AiProviderAdminDto>>>> ListProviders(
        CancellationToken cancellationToken)
    {
        var list = await _adminService.ListProvidersAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AiProviderAdminDto>>.Ok(list, "ok"));
    }

    [HttpPut("providers/{id}")]
    [RequirePermission("biz.ai.admin")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateProvider(
        string id,
        [FromBody] AiProviderAdminDto dto,
        CancellationToken cancellationToken)
    {
        dto.Id = id;
        try
        {
            await _adminService.UpdateProviderAsync(dto, cancellationToken);
            return Ok(ApiResponse<object>.Ok(null, "已保存"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新 AI 厂商失败 id={Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("保存失败"));
        }
    }

    [HttpGet("templates")]
    [RequirePermission("biz.ai.admin")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AiPromptTemplateAdminDto>>>> ListTemplates(
        CancellationToken cancellationToken)
    {
        var list = await _adminService.ListTemplatesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AiPromptTemplateAdminDto>>.Ok(list, "ok"));
    }

    [HttpPut("templates/{id}")]
    [RequirePermission("biz.ai.admin")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateTemplate(
        string id,
        [FromBody] AiPromptTemplateAdminDto dto,
        CancellationToken cancellationToken)
    {
        dto.Id = id;
        try
        {
            await _adminService.UpdateTemplateAsync(dto, cancellationToken);
            return Ok(ApiResponse<object>.Ok(null, "已保存"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新 AI 模板失败 id={Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("保存失败"));
        }
    }

    [HttpGet("scenarios")]
    [RequirePermission("biz.ai.admin")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AiScenarioAdminDto>>>> ListScenarios(
        CancellationToken cancellationToken)
    {
        var list = await _adminService.ListScenariosAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AiScenarioAdminDto>>.Ok(list, "ok"));
    }

    [HttpPut("scenarios/{id}")]
    [RequirePermission("biz.ai.admin")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateScenario(
        string id,
        [FromBody] AiScenarioAdminDto dto,
        CancellationToken cancellationToken)
    {
        dto.Id = id;
        try
        {
            await _adminService.UpdateScenarioAsync(dto, cancellationToken);
            return Ok(ApiResponse<object>.Ok(null, "已保存"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新 AI 场景失败 id={Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("保存失败"));
        }
    }

    [HttpGet("logs")]
    [RequirePermission("biz.ai.admin")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AiInvocationLogListItemDto>>>> ListLogs(
        [FromQuery] int take = 50,
        [FromQuery] string? scenarioCode = null,
        CancellationToken cancellationToken = default)
    {
        var list = await _adminService.ListInvocationLogsAsync(take, scenarioCode, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AiInvocationLogListItemDto>>.Ok(list, "ok"));
    }

    [HttpGet("usage")]
    [RequirePermission("biz.ai.admin")]
    public async Task<ActionResult<ApiResponse<AiUsageSummaryDto>>> GetUsage(
        CancellationToken cancellationToken)
    {
        var summary = await _adminService.GetUsageSummaryAsync(cancellationToken);
        return Ok(ApiResponse<AiUsageSummaryDto>.Ok(summary, "ok"));
    }
}
