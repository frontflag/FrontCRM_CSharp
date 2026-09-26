using System.Security.Claims;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/ai-assistant")]
[Authorize]
public class AiAssistantController : ControllerBase
{
    private readonly IAiAssistantService _assistantService;
    private readonly IRbacService _rbacService;
    private readonly ILogger<AiAssistantController> _logger;

    public AiAssistantController(
        IAiAssistantService assistantService,
        IRbacService rbacService,
        ILogger<AiAssistantController> logger)
    {
        _assistantService = assistantService;
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
            var allowed = new List<string>();
            if (AllowsBiz(summary, AiAssistantPermissionCodes.Submit))
                allowed.Add(AiAssistantSkills.Feedback);
            if (AllowsBiz(summary, KbHandbookCodes.AskPermission))
                allowed.Add(AiAssistantSkills.Handbook);
            if (allowed.Count == 0)
                return StatusCode(403, ApiResponse<AiSkillRouteDto>.Fail("无权限使用 AI 交互", 403));

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

    private static bool AllowsBiz(UserPermissionSummaryDto summary, string code)
    {
        if (summary.IsSysAdmin || summary.HasBizDataBypass)
            return true;
        return summary.PermissionCodes.Any(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase));
    }

    [HttpPost("sessions")]
    [RequirePermission(AiAssistantPermissionCodes.Submit)]
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
    [RequirePermission(AiAssistantPermissionCodes.Submit)]
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
