using System.Security.Claims;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/ai-assistant")]
[Authorize]
public class AiAssistantController : ControllerBase
{
    private readonly IAiAssistantService _assistantService;
    private readonly ILogger<AiAssistantController> _logger;

    public AiAssistantController(IAiAssistantService assistantService, ILogger<AiAssistantController> logger)
    {
        _assistantService = assistantService;
        _logger = logger;
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
