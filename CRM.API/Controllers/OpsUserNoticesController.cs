using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Document;
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
    [RequestSizeLimit(80 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 80 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<SysUserNoticeDetailDto>>> Send(
        [FromForm] string? recipientUserId,
        [FromForm] bool isUrgent,
        [FromForm] string? title,
        [FromForm] string? body,
        [FromForm] IFormFileCollection? files,
        CancellationToken ct)
    {
        if (!await IsSuperAdminAsync())
            return StatusCode(403, ApiResponse<SysUserNoticeDetailDto>.Fail("仅 SuperAdmin 可操作", 403));
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<SysUserNoticeDetailDto>.Fail("未登录", 401));

        var images = new List<DocumentUploadFile>();
        try
        {
            if (files != null)
            {
                foreach (var f in files)
                {
                    if (f == null || f.Length == 0) continue;
                    var stream = new MemoryStream();
                    await f.CopyToAsync(stream, ct);
                    stream.Position = 0;
                    images.Add(new DocumentUploadFile
                    {
                        Stream = stream,
                        FileName = f.FileName ?? "image",
                        ContentType = f.ContentType
                    });
                }
            }

            var dto = await _service.AdminSendAsync(new SysUserNoticeSendRequest
            {
                RecipientUserId = recipientUserId ?? string.Empty,
                IsUrgent = isUrgent,
                Title = title ?? string.Empty,
                Body = body ?? string.Empty
            }, uid, images, ct);
            return Ok(ApiResponse<SysUserNoticeDetailDto>.Ok(dto, "已发送"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SysUserNoticeDetailDto>.Fail(ex.Message, 400));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<SysUserNoticeDetailDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送系统通知失败");
            return StatusCode(500, ApiResponse<SysUserNoticeDetailDto>.Fail("发送失败", 500));
        }
        finally
        {
            foreach (var img in images)
                img.Stream?.Dispose();
        }
    }
}
