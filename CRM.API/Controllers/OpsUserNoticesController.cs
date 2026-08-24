using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

/// <summary>运维 · 消息通知管理（仅 SuperAdmin）。</summary>
[ApiController]
[Route("api/v1/ops/user-notices")]
[Authorize]
public class OpsUserNoticesController : ControllerBase
{
    private readonly ISysUserNoticeService _service;
    private readonly IRbacService _rbacService;
    private readonly ILogger<OpsUserNoticesController> _logger;

    public OpsUserNoticesController(
        ISysUserNoticeService service,
        IRbacService rbacService,
        ILogger<OpsUserNoticesController> logger)
    {
        _service = service;
        _rbacService = rbacService;
        _logger = logger;
    }

    private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private async Task<bool> IsSuperAdminAsync()
    {
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid)) return false;
        var summary = await _rbacService.GetUserPermissionSummaryAsync(uid);
        return summary?.IsSysAdmin == true;
    }

    [HttpGet("recipients")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SysUserNoticeRecipientDto>>>> Recipients(CancellationToken ct)
    {
        if (!await IsSuperAdminAsync())
            return StatusCode(403, ApiResponse<IReadOnlyList<SysUserNoticeRecipientDto>>.Fail("仅 SuperAdmin 可操作", 403));

        var list = await _service.ListRecipientsAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<SysUserNoticeRecipientDto>>.Ok(list));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<SysUserNoticeAdminPagedDto>>> List(
        [FromQuery] bool? isUrgent,
        [FromQuery] bool? isRead,
        [FromQuery] string? recipientUserId,
        [FromQuery] string? keyword,
        [FromQuery] DateTime? sendFrom,
        [FromQuery] DateTime? sendTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!await IsSuperAdminAsync())
            return StatusCode(403, ApiResponse<SysUserNoticeAdminPagedDto>.Fail("仅 SuperAdmin 可操作", 403));

        var dto = await _service.AdminListAsync(new SysUserNoticeAdminQuery
        {
            IsUrgent = isUrgent,
            IsRead = isRead,
            RecipientUserId = recipientUserId,
            Keyword = keyword,
            SendFrom = sendFrom,
            SendTo = sendTo,
            Page = page,
            PageSize = pageSize
        }, ct);
        return Ok(ApiResponse<SysUserNoticeAdminPagedDto>.Ok(dto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SysUserNoticeDetailDto>>> Get(string id, CancellationToken ct)
    {
        if (!await IsSuperAdminAsync())
            return StatusCode(403, ApiResponse<SysUserNoticeDetailDto>.Fail("仅 SuperAdmin 可操作", 403));

        var dto = await _service.AdminGetAsync(id, ct);
        if (dto == null)
            return NotFound(ApiResponse<SysUserNoticeDetailDto>.Fail("通知不存在", 404));
        return Ok(ApiResponse<SysUserNoticeDetailDto>.Ok(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SysUserNoticeDetailDto>>> Send(
        [FromBody] SysUserNoticeSendRequest? body,
        CancellationToken ct)
    {
        if (!await IsSuperAdminAsync())
            return StatusCode(403, ApiResponse<SysUserNoticeDetailDto>.Fail("仅 SuperAdmin 可操作", 403));
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<SysUserNoticeDetailDto>.Fail("未登录", 401));

        try
        {
            var dto = await _service.AdminSendAsync(body ?? new SysUserNoticeSendRequest(), uid, ct);
            return Ok(ApiResponse<SysUserNoticeDetailDto>.Ok(dto, "已发送"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SysUserNoticeDetailDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送系统通知失败");
            return StatusCode(500, ApiResponse<SysUserNoticeDetailDto>.Fail("发送失败", 500));
        }
    }
}
