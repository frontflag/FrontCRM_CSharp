using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

/// <summary>当前用户 · 桌面系统通告。</summary>
[ApiController]
[Route("api/v1/me/dashboard-notices")]
[Authorize]
public class MeDashboardNoticesController : ControllerBase
{
    private readonly IDashboardNoticeService _service;
    private readonly ILogger<MeDashboardNoticesController> _logger;

    public MeDashboardNoticesController(
        IDashboardNoticeService service,
        ILogger<MeDashboardNoticesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private bool IsImpersonating =>
        !string.IsNullOrWhiteSpace(User.FindFirst(ImpersonationClaimTypes.Impersonator)?.Value);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DashboardNoticeItemDto>>>> List(CancellationToken ct)
    {
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<IReadOnlyList<DashboardNoticeItemDto>>.Fail("未登录", 401));

        var list = await _service.ListAsync(uid, ct);
        return Ok(ApiResponse<IReadOnlyList<DashboardNoticeItemDto>>.Ok(list));
    }

    [HttpPost("bbs/{id}/dismiss")]
    public async Task<ActionResult<ApiResponse<object>>> DismissBbs(string id, CancellationToken ct)
    {
        if (IsImpersonating)
            return Ok(ApiResponse<object>.Ok(new { recorded = false }, "模拟登录不记已读"));

        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<object>.Fail("未登录", 401));

        try
        {
            await _service.DismissBbsAsync(id, uid, ct);
            return Ok(ApiResponse<object>.Ok(new { recorded = true }, "ok"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "系统通告消失失败 {Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("操作失败", 500));
        }
    }
}
