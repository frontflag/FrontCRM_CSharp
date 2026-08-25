using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/customer-workspace")]
public sealed class CustomerWorkspaceController : ControllerBase
{
    private readonly ICustomerWorkspaceService _workspace;
    private readonly ILogger<CustomerWorkspaceController> _logger;

    public CustomerWorkspaceController(
        ICustomerWorkspaceService workspace,
        ILogger<CustomerWorkspaceController> logger)
    {
        _workspace = workspace;
        _logger = logger;
    }

    /// <summary>右栏「客户」页签摘要。按单据主键解析客户，不接受前端传入的客户 ID。</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get(
        [FromQuery] string? source,
        [FromQuery] string? id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(ApiResponse<object>.Fail("未登录", 401));

            var dto = await _workspace.GetAsync(source ?? string.Empty, id ?? string.Empty, userId);
            if (dto == null)
                return NotFound(ApiResponse<object>.Fail("单据不存在", 404));
            return Ok(ApiResponse<object>.Ok(dto, "获取客户摘要成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<object>.Fail(ex.Message, 403));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取客户摘要失败: {Source}/{Id}", source, id);
            return StatusCode(500, ApiResponse<object>.Fail($"获取客户摘要失败: {ex.Message}", 500));
        }
    }
}
