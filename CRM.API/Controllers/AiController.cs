using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiOrchestrator _orchestrator;
    private readonly IAiAdminService _adminService;
    private readonly IAiEntityParseLogService _entityParseLogService;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IAiOrchestrator orchestrator,
        IAiAdminService adminService,
        IAiEntityParseLogService entityParseLogService,
        ILogger<AiController> logger)
    {
        _orchestrator = orchestrator;
        _adminService = adminService;
        _entityParseLogService = entityParseLogService;
        _logger = logger;
    }

    /// <summary>按场景调用 AI（非流式）。权限与限流由 Orchestrator 处理。</summary>
    [HttpPost("invoke")]
    public async Task<ActionResult<ApiResponse<AiInvokeResultDto>>> Invoke(
        [FromBody] AiInvokeRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ScenarioCode))
            return BadRequest(ApiResponse<AiInvokeResultDto>.Fail("scenarioCode 不能为空"));

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        try
        {
            var result = await _orchestrator.InvokeAsync(request, userId, cancellationToken);
            var msg = result.FromCache ? "命中缓存" : "调用成功";
            return Ok(ApiResponse<AiInvokeResultDto>.Ok(result, msg));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "AI invoke rejected scenario={Scenario}", request.ScenarioCode);
            return BadRequest(ApiResponse<AiInvokeResultDto>.Fail(ex.Message));
        }
        catch (DbUpdateException ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            _logger.LogError(ex, "AI invoke DB save failed scenario={Scenario}", request.ScenarioCode);
            return StatusCode(500, ApiResponse<AiInvokeResultDto>.Fail(
                $"AI 调用日志/缓存写入失败，请确认已执行 AI 模块数据库脚本。详情: {detail}", 500));
        }
        catch (OperationCanceledException ex) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "AI invoke canceled (client/proxy timeout) scenario={Scenario}", request.ScenarioCode);
            return StatusCode(504, ApiResponse<AiInvokeResultDto>.Fail(
                "AI 调用超时：网关或浏览器在结果返回前断开连接。请让运维将 Nginx proxy_read_timeout 调整为至少 300s（见 scripts/nginx-ai-invoke-timeout.snippet.conf）。", 504));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI invoke failed scenario={Scenario}", request.ScenarioCode);
            return StatusCode(500, ApiResponse<AiInvokeResultDto>.Fail($"调用失败: {ex.Message}", 500));
        }
    }

    /// <summary>当前用户可调用的 AI 场景列表。</summary>
    [HttpGet("scenarios")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AiScenarioListItemDto>>>> ListScenarios(
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var list = await _adminService.ListInvokableScenariosForUserAsync(userId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AiScenarioListItemDto>>.Ok(list, "ok"));
    }

    /// <summary>entity.parse.* 用户在确认弹窗编辑后上报 confirmed_fields_json。</summary>
    [HttpPost("entity-parse-logs/{id}/confirm")]
    public async Task<ActionResult<ApiResponse<object>>> ConfirmEntityParseLog(
        string id,
        [FromBody] AiEntityParseLogConfirmDto body,
        CancellationToken cancellationToken)
    {
        if (body.ConfirmedFields.ValueKind != System.Text.Json.JsonValueKind.Object)
            return BadRequest(ApiResponse<object>.Fail("confirmedFields 必须为 JSON 对象"));

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        try
        {
            await _entityParseLogService.ConfirmAsync(id, userId ?? string.Empty, body.ConfirmedFields, cancellationToken);
            return Ok(ApiResponse<object>.Ok(new { id }, "已记录确认结果"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>entity.parse.* 用户保存成功后回写 saved_biz_id。</summary>
    [HttpPost("entity-parse-logs/{id}/saved")]
    public async Task<ActionResult<ApiResponse<object>>> MarkEntityParseLogSaved(
        string id,
        [FromBody] AiEntityParseLogSavedDto body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body?.SavedBizId))
            return BadRequest(ApiResponse<object>.Fail("savedBizId 不能为空"));

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        try
        {
            await _entityParseLogService.MarkSavedAsync(id, userId ?? string.Empty, body.SavedBizId.Trim(), cancellationToken);
            return Ok(ApiResponse<object>.Ok(new { id }, "已记录保存结果"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
