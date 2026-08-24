using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

/// <summary>当前用户 · 系统通知（抽屉「系统消息」）。</summary>
[ApiController]
[Route("api/v1/me/user-notices")]
[Authorize]
public class MeUserNoticesController : ControllerBase
{
    private readonly ISysUserNoticeService _service;
    private readonly ILogger<MeUserNoticesController> _logger;

    public MeUserNoticesController(
        ISysUserNoticeService service,
        ILogger<MeUserNoticesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private bool IsImpersonating =>
        !string.IsNullOrWhiteSpace(User.FindFirst(ImpersonationClaimTypes.Impersonator)?.Value);

    [HttpGet("unread-summary")]
    public async Task<ActionResult<ApiResponse<SysUserNoticeUnreadSummaryDto>>> UnreadSummary(CancellationToken ct)
    {
        if (IsImpersonating)
            return Ok(ApiResponse<SysUserNoticeUnreadSummaryDto>.Ok(new SysUserNoticeUnreadSummaryDto()));

        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<SysUserNoticeUnreadSummaryDto>.Fail("未登录", 401));

        var dto = await _service.GetUnreadSummaryAsync(uid, ct);
        return Ok(ApiResponse<SysUserNoticeUnreadSummaryDto>.Ok(dto));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SysUserNoticeMeListItemDto>>>> List(CancellationToken ct)
    {
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<IReadOnlyList<SysUserNoticeMeListItemDto>>.Fail("未登录", 401));

        var list = await _service.ListMineAsync(uid, ct);
        return Ok(ApiResponse<IReadOnlyList<SysUserNoticeMeListItemDto>>.Ok(list));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SysUserNoticeDetailDto>>> Get(string id, CancellationToken ct)
    {
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<SysUserNoticeDetailDto>.Fail("未登录", 401));

        var dto = await _service.GetMineAsync(id, uid, ct);
        if (dto == null)
            return NotFound(ApiResponse<SysUserNoticeDetailDto>.Fail("通知不存在", 404));
        return Ok(ApiResponse<SysUserNoticeDetailDto>.Ok(dto));
    }

    [HttpPost("read-all")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllRead(CancellationToken ct)
    {
        if (IsImpersonating)
            return Ok(ApiResponse<object>.Ok(new { }, "模拟登录不记已读"));

        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<object>.Fail("未登录", 401));

        try
        {
            await _service.MarkAllReadAsync(uid, ct);
            return Ok(ApiResponse<object>.Ok(new { }, "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "全部标记系统通知已读失败");
            return StatusCode(500, ApiResponse<object>.Fail("标记失败", 500));
        }
    }

    [HttpPost("{id}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkRead(string id, CancellationToken ct)
    {
        if (IsImpersonating)
            return Ok(ApiResponse<object>.Ok(new { }, "模拟登录不记已读"));

        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<object>.Fail("未登录", 401));

        try
        {
            await _service.MarkReadAsync(id, uid, ct);
            return Ok(ApiResponse<object>.Ok(new { }, "ok"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记系统通知已读失败 {Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("标记失败", 500));
        }
    }
}
