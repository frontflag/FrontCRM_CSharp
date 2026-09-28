using System.Security.Claims;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/ai-assistant")]
[Authorize]
public class AiAssistantController : ControllerBase
{
    private readonly IAiAssistantService _assistantService;
    private readonly IAiDataQueryService _dataQueryService;
    private readonly IRbacService _rbacService;
    private readonly ILogger<AiAssistantController> _logger;

    public AiAssistantController(
        IAiAssistantService assistantService,
        IAiDataQueryService dataQueryService,
        IRbacService rbacService,
        ILogger<AiAssistantController> logger)
    {
        _assistantService = assistantService;
        _dataQueryService = dataQueryService;
        _rbacService = rbacService;
        _logger = logger;
    }

    [HttpPost("route")]
    public async Task<ActionResult<ApiResponse<AiSkillRouteDto>>> Route(
        [FromBody] RouteAiSkillRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(ApiResponse<AiSkillRouteDto>.Fail("未登录", 401));

        var text = (request?.Text ?? string.Empty).Trim();
        if (text.Length == 0)
            return BadRequest(ApiResponse<AiSkillRouteDto>.Fail("请输入文字"));

        try
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
            var allowed = new List<string>
            {
                // 反馈 / 培训教材 / 操作手册：任意已登录用户可用
                AiAssistantSkills.Feedback,
                AiAssistantSkills.Handbook,
                AiAssistantSkills.Ops
            };
            if (AllowsBiz(summary, AiAssistantPermissionCodes.DataQuery))
                allowed.Add(AiAssistantSkills.Data);

            var skill = await _assistantService.RouteSkillAsync(text, allowed, cancellationToken);
            return Ok(ApiResponse<AiSkillRouteDto>.Ok(new AiSkillRouteDto { Skill = skill }));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AiSkillRouteDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI skill route failed");
            return StatusCode(500, ApiResponse<AiSkillRouteDto>.Fail($"技能判断失败: {ex.Message}", 500));
        }
    }

    [HttpPost("data-query")]
    [RequirePermission(AiAssistantPermissionCodes.DataQuery)]
    public async Task<ActionResult<ApiResponse<AiDataQueryResponse>>> DataQuery(
        [FromBody] AiDataQueryRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(ApiResponse<AiDataQueryResponse>.Fail("未登录", 401));

        var question = (request?.Question ?? string.Empty).Trim();
        if (question.Length == 0)
            return BadRequest(ApiResponse<AiDataQueryResponse>.Fail("请输入文字"));
        if (question.Length > 500)
            return BadRequest(ApiResponse<AiDataQueryResponse>.Fail("这一句请控制在 500 字以内"));

        try
        {
            var data = await _dataQueryService.AskAsync(userId, question, request?.State, cancellationToken);
            return Ok(ApiResponse<AiDataQueryResponse>.Ok(data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI data query failed");
            return StatusCode(500, ApiResponse<AiDataQueryResponse>.Fail($"查询失败: {ex.Message}", 500));
        }
    }

    private static bool AllowsBiz(UserPermissionSummaryDto summary, string code)
    {
        if (summary.IsSysAdmin || summary.HasBizDataBypass)
            return true;
        return summary.PermissionCodes.Any(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase));
    }

    [HttpPost("sessions")]
    public async Task<ActionResult<ApiResponse<AiAssistantSessionDto>>> CreateSession(
        [FromBody] CreateAiAssistantSessionRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(ApiResponse<AiAssistantSessionDto>.Fail("未登录", 401));

        try
        {
            var dto = await _assistantService.CreateSessionAsync(request ?? new CreateAiAssistantSessionRequest(), userId, cancellationToken);
            return Ok(ApiResponse<AiAssistantSessionDto>.Ok(dto));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<AiAssistantSessionDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create AI assistant session failed");
            return StatusCode(500, ApiResponse<AiAssistantSessionDto>.Fail($"创建会话失败: {ex.Message}", 500));
        }
    }

    [HttpPost("sessions/{id}/messages")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<AiAssistantChatTurnDto>>> SendMessage(
        string id,
        [FromBody] SendAiAssistantMessageRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(ApiResponse<AiAssistantChatTurnDto>.Fail("未登录", 401));

        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(ApiResponse<AiAssistantChatTurnDto>.Fail("sessionId 不能为空"));

        try
        {
            var dto = await _assistantService.SendMessageAsync(
                id.Trim(),
                request ?? new SendAiAssistantMessageRequest(),
                userId,
                cancellationToken);
            return Ok(ApiResponse<AiAssistantChatTurnDto>.Ok(dto));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<AiAssistantChatTurnDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AiAssistantChatTurnDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI assistant message failed session={SessionId}", id);
            return StatusCode(500, ApiResponse<AiAssistantChatTurnDto>.Fail($"发送失败: {ex.Message}", 500));
        }
    }
}
