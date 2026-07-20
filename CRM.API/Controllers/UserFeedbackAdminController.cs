using System.Security.Claims;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/feedback/admin")]
[Authorize]
[RequirePermission(AiAssistantPermissionCodes.Admin)]
public class UserFeedbackAdminController : ControllerBase
{
    private readonly IUserFeedbackAdminService _adminService;
    private readonly ILogger<UserFeedbackAdminController> _logger;

    public UserFeedbackAdminController(
        IUserFeedbackAdminService adminService,
        ILogger<UserFeedbackAdminController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserFeedbackPagedResult>>> List(
        [FromQuery] string? category,
        [FromQuery] bool? needsHandling,
        [FromQuery] bool? isHandled,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _adminService.GetAdminListAsync(new UserFeedbackAdminQuery
        {
            Category = category,
            NeedsHandling = needsHandling,
            IsHandled = isHandled,
            Keyword = keyword,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
        return Ok(ApiResponse<UserFeedbackPagedResult>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserFeedbackDetailDto>>> Detail(
        string id,
        [FromQuery] bool includeMessages = true,
        CancellationToken cancellationToken = default)
    {
        var dto = await _adminService.GetAdminDetailAsync(id, includeMessages, cancellationToken);
        if (dto == null)
            return NotFound(ApiResponse<UserFeedbackDetailDto>.Fail("反馈不存在", 404));
        return Ok(ApiResponse<UserFeedbackDetailDto>.Ok(dto));
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<ApiResponse<UserFeedbackDetailDto>>> Patch(
        string id,
        [FromBody] PatchUserFeedbackRequest? request,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(ApiResponse<UserFeedbackDetailDto>.Fail("未登录", 401));

        try
        {
            var dto = await _adminService.PatchAdminAsync(
                id,
                request ?? new PatchUserFeedbackRequest(),
                userId,
                cancellationToken);
            return Ok(ApiResponse<UserFeedbackDetailDto>.Ok(dto));
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("保存失败", StringComparison.Ordinal))
        {
            _logger.LogError(ex, "Patch user feedback DB failed id={Id}", id);
            return BadRequest(ApiResponse<UserFeedbackDetailDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<UserFeedbackDetailDto>.Fail(ex.Message, 404));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<UserFeedbackDetailDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            _logger.LogError(ex, "Patch user feedback failed id={Id}", id);
            return StatusCode(500, ApiResponse<UserFeedbackDetailDto>.Fail($"更新失败: {detail}", 500));
        }
    }
}
