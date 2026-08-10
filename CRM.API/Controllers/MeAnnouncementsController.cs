using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

/// <summary>当前用户 · 系统公告（历史 / 未读 / 已读）。</summary>
[ApiController]
[Route("api/v1/me/announcements")]
[Authorize]
public class MeAnnouncementsController : ControllerBase
{
    private readonly ISysAnnouncementService _service;
    private readonly ILogger<MeAnnouncementsController> _logger;

    public MeAnnouncementsController(
        ISysAnnouncementService service,
        ILogger<MeAnnouncementsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private bool IsImpersonating =>
        !string.IsNullOrWhiteSpace(User.FindFirst(ImpersonationClaimTypes.Impersonator)?.Value);

    [HttpGet("unread-summary")]
    public async Task<ActionResult<ApiResponse<SysAnnouncementUnreadSummaryDto>>> UnreadSummary(CancellationToken ct)
    {
        if (IsImpersonating)
            return Ok(ApiResponse<SysAnnouncementUnreadSummaryDto>.Ok(new SysAnnouncementUnreadSummaryDto { TotalUnread = 0 }));

        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<SysAnnouncementUnreadSummaryDto>.Fail("未登录", 401));

        var dto = await _service.GetUnreadSummaryAsync(uid, ct);
        return Ok(ApiResponse<SysAnnouncementUnreadSummaryDto>.Ok(dto));
    }

    [HttpGet("unread-preview")]
    public async Task<ActionResult<ApiResponse<SysAnnouncementUnreadPreviewDto>>> UnreadPreview(
        [FromQuery] int limit = 5,
        CancellationToken ct = default)
    {
        if (IsImpersonating)
        {
            return Ok(ApiResponse<SysAnnouncementUnreadPreviewDto>.Ok(new SysAnnouncementUnreadPreviewDto
            {
                TotalUnread = 0,
                Items = Array.Empty<SysAnnouncementDetailDto>()
            }));
        }

        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<SysAnnouncementUnreadPreviewDto>.Fail("未登录", 401));

        var dto = await _service.GetUnreadPreviewAsync(uid, limit, ct);
        return Ok(ApiResponse<SysAnnouncementUnreadPreviewDto>.Ok(dto));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SysAnnouncementHistoryItemDto>>>> History(
        CancellationToken ct)
    {
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<IReadOnlyList<SysAnnouncementHistoryItemDto>>.Fail("未登录", 401));

        // 模拟登录仍可浏览历史（只读），但不计入未读角标；列表 isRead 按被模拟用户真实已读状态展示
        var list = await _service.GetHistoryAsync(uid, ct);
        return Ok(ApiResponse<IReadOnlyList<SysAnnouncementHistoryItemDto>>.Ok(list));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SysAnnouncementDetailDto>>> Get(string id, CancellationToken ct)
    {
        var dto = await _service.GetPublishedAsync(id, ct);
        if (dto == null)
            return NotFound(ApiResponse<SysAnnouncementDetailDto>.Fail("公告不存在", 404));
        return Ok(ApiResponse<SysAnnouncementDetailDto>.Ok(dto));
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
            _logger.LogError(ex, "标记公告已读失败 {Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("标记失败", 500));
        }
    }
}
