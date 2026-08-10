using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

/// <summary>运维 · 系统公告管理（仅 SuperAdmin）。</summary>
[ApiController]
[Route("api/v1/ops/announcements")]
[Authorize]
public class OpsAnnouncementsController : ControllerBase
{
    private readonly ISysAnnouncementService _service;
    private readonly IRbacService _rbacService;
    private readonly ILogger<OpsAnnouncementsController> _logger;

    public OpsAnnouncementsController(
        ISysAnnouncementService service,
        IRbacService rbacService,
        ILogger<OpsAnnouncementsController> logger)
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

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SysAnnouncementAdminListItemDto>>>> List(
        [FromQuery] string? status,
        [FromQuery] string? type,
        CancellationToken ct)
    {
        if (!await IsSuperAdminAsync())
            return StatusCode(403, ApiResponse<IReadOnlyList<SysAnnouncementAdminListItemDto>>.Fail("仅 SuperAdmin 可操作", 403));

        var list = await _service.AdminListAsync(status, type, ct);
        return Ok(ApiResponse<IReadOnlyList<SysAnnouncementAdminListItemDto>>.Ok(list));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SysAnnouncementDetailDto>>> Get(string id, CancellationToken ct)
    {
        if (!await IsSuperAdminAsync())
            return StatusCode(403, ApiResponse<SysAnnouncementDetailDto>.Fail("仅 SuperAdmin 可操作", 403));

        var dto = await _service.AdminGetAsync(id, ct);
        if (dto == null)
            return NotFound(ApiResponse<SysAnnouncementDetailDto>.Fail("公告不存在", 404));
        return Ok(ApiResponse<SysAnnouncementDetailDto>.Ok(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SysAnnouncementDetailDto>>> Create(
        [FromBody] SysAnnouncementUpsertRequest? body,
        CancellationToken ct)
    {
        if (!await IsSuperAdminAsync())
            return StatusCode(403, ApiResponse<SysAnnouncementDetailDto>.Fail("仅 SuperAdmin 可操作", 403));
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<SysAnnouncementDetailDto>.Fail("未登录", 401));

        try
        {
            var dto = await _service.AdminCreateAsync(body ?? new SysAnnouncementUpsertRequest(), uid, ct);
            return Ok(ApiResponse<SysAnnouncementDetailDto>.Ok(dto));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SysAnnouncementDetailDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建系统公告失败");
            return StatusCode(500, ApiResponse<SysAnnouncementDetailDto>.Fail("创建失败", 500));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<SysAnnouncementDetailDto>>> Update(
        string id,
        [FromBody] SysAnnouncementUpsertRequest? body,
        CancellationToken ct)
    {
        if (!await IsSuperAdminAsync())
            return StatusCode(403, ApiResponse<SysAnnouncementDetailDto>.Fail("仅 SuperAdmin 可操作", 403));
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<SysAnnouncementDetailDto>.Fail("未登录", 401));

        try
        {
            var dto = await _service.AdminUpdateAsync(id, body ?? new SysAnnouncementUpsertRequest(), uid, ct);
            return Ok(ApiResponse<SysAnnouncementDetailDto>.Ok(dto));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SysAnnouncementDetailDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新系统公告失败 {Id}", id);
            return StatusCode(500, ApiResponse<SysAnnouncementDetailDto>.Fail("更新失败", 500));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id, CancellationToken ct)
    {
        if (!await IsSuperAdminAsync())
            return StatusCode(403, ApiResponse<object>.Fail("仅 SuperAdmin 可操作", 403));

        try
        {
            await _service.AdminDeleteAsync(id, ct);
            return Ok(ApiResponse<object>.Ok(new { }, "已删除"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除系统公告失败 {Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("删除失败", 500));
        }
    }

    [HttpPost("{id}/publish")]
    public async Task<ActionResult<ApiResponse<SysAnnouncementDetailDto>>> Publish(string id, CancellationToken ct)
    {
        if (!await IsSuperAdminAsync())
            return StatusCode(403, ApiResponse<SysAnnouncementDetailDto>.Fail("仅 SuperAdmin 可操作", 403));
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<SysAnnouncementDetailDto>.Fail("未登录", 401));

        try
        {
            var dto = await _service.AdminPublishAsync(id, uid, ct);
            return Ok(ApiResponse<SysAnnouncementDetailDto>.Ok(dto, "已发布"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SysAnnouncementDetailDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布系统公告失败 {Id}", id);
            return StatusCode(500, ApiResponse<SysAnnouncementDetailDto>.Fail("发布失败", 500));
        }
    }
}
