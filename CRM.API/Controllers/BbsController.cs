using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Bbs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/bbs")]
[Authorize]
public class BbsController : ControllerBase
{
    private readonly IBbsService _service;
    private readonly IRbacService _rbacService;

    public BbsController(IBbsService service, IRbacService rbacService)
    {
        _service = service;
        _rbacService = rbacService;
    }

    private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private async Task<(string UserId, bool IsModerator)?> TryGetActorAsync()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId)) return null;
        var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
        var isMod = summary.IsSysAdmin
            || summary.IsSysManager
            || (summary.PermissionCodes?.Any(c =>
                string.Equals(c, BbsPermissionCodes.Moderate, StringComparison.OrdinalIgnoreCase)) ?? false);
        return (userId, isMod);
    }

    [HttpGet("subjects/top")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BbsSubjectListItemDto>>>> GetTop(CancellationToken ct)
    {
        var actor = await TryGetActorAsync();
        if (actor == null)
            return Unauthorized(ApiResponse<IReadOnlyList<BbsSubjectListItemDto>>.Fail("未登录", 401));
        var list = await _service.GetTopSubjectsAsync(actor.Value.UserId, actor.Value.IsModerator, ct);
        return Ok(ApiResponse<IReadOnlyList<BbsSubjectListItemDto>>.Ok(list));
    }

    [HttpGet("subjects")]
    public async Task<ActionResult<ApiResponse<BbsSubjectPagedDto>>> List(
        [FromQuery] int? type,
        [FromQuery] int? status,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var actor = await TryGetActorAsync();
        if (actor == null)
            return Unauthorized(ApiResponse<BbsSubjectPagedDto>.Fail("未登录", 401));
        var dto = await _service.QuerySubjectsAsync(new BbsSubjectQuery
        {
            Type = type,
            Status = status,
            Keyword = keyword,
            Page = page,
            PageSize = pageSize
        }, actor.Value.UserId, actor.Value.IsModerator, ct);
        return Ok(ApiResponse<BbsSubjectPagedDto>.Ok(dto));
    }

    [HttpGet("subjects/{id}")]
    public async Task<ActionResult<ApiResponse<BbsSubjectDetailDto>>> Detail(string id, CancellationToken ct)
    {
        var actor = await TryGetActorAsync();
        if (actor == null)
            return Unauthorized(ApiResponse<BbsSubjectDetailDto>.Fail("未登录", 401));
        var dto = await _service.GetSubjectDetailAsync(id, actor.Value.UserId, actor.Value.IsModerator, ct);
        if (dto == null)
            return NotFound(ApiResponse<BbsSubjectDetailDto>.Fail("没有找到主题", 404));
        return Ok(ApiResponse<BbsSubjectDetailDto>.Ok(dto));
    }

    [HttpPost("subjects")]
    public async Task<ActionResult<ApiResponse<BbsSubjectDetailDto>>> Create(
        [FromBody] BbsSubjectCreateRequest request,
        CancellationToken ct)
    {
        try
        {
            var actor = await TryGetActorAsync();
            if (actor == null)
                return Unauthorized(ApiResponse<BbsSubjectDetailDto>.Fail("未登录", 401));
            var dto = await _service.CreateSubjectAsync(request, actor.Value.UserId, actor.Value.IsModerator, ct);
            return Ok(ApiResponse<BbsSubjectDetailDto>.Ok(dto));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<BbsSubjectDetailDto>.Fail(ex.Message, 400));
        }
    }

    [HttpPut("subjects/{id}")]
    public async Task<ActionResult<ApiResponse<BbsSubjectDetailDto>>> Update(
        string id,
        [FromBody] BbsSubjectUpdateRequest request,
        CancellationToken ct)
    {
        try
        {
            var actor = await TryGetActorAsync();
            if (actor == null)
                return Unauthorized(ApiResponse<BbsSubjectDetailDto>.Fail("未登录", 401));
            var dto = await _service.UpdateSubjectAsync(id, request, actor.Value.UserId, actor.Value.IsModerator, ct);
            return Ok(ApiResponse<BbsSubjectDetailDto>.Ok(dto));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<BbsSubjectDetailDto>.Fail(ex.Message, 404));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<BbsSubjectDetailDto>.Fail(ex.Message, 403));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<BbsSubjectDetailDto>.Fail(ex.Message, 400));
        }
    }

    [HttpPost("subjects/{id}/close")]
    public async Task<ActionResult<ApiResponse<object>>> Close(string id, CancellationToken ct)
        => await RunAsync(id, (s, u, m, c) => _service.CloseSubjectAsync(s, u, m, c), ct);

    [HttpPost("subjects/{id}/open")]
    public async Task<ActionResult<ApiResponse<object>>> Open(string id, CancellationToken ct)
        => await RunAsync(id, (s, u, m, c) => _service.OpenSubjectAsync(s, u, m, c), ct);

    [HttpPost("subjects/{id}/top")]
    public async Task<ActionResult<ApiResponse<object>>> SetTop(string id, CancellationToken ct)
        => await RunAsync(id, (s, u, m, c) => _service.SetTopAsync(s, u, m, c), ct);

    [HttpPost("subjects/{id}/untop")]
    public async Task<ActionResult<ApiResponse<object>>> Untop(string id, CancellationToken ct)
        => await RunAsync(id, (s, u, m, c) => _service.CancelTopAsync(s, u, m, c), ct);

    [HttpDelete("subjects/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteSubject(string id, CancellationToken ct)
        => await RunAsync(id, (s, u, m, c) => _service.DeleteSubjectAsync(s, u, m, c), ct);

    [HttpGet("subjects/{id}/replies")]
    public async Task<ActionResult<ApiResponse<BbsReplyPagedDto>>> Replies(
        string id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        try
        {
            var actor = await TryGetActorAsync();
            if (actor == null)
                return Unauthorized(ApiResponse<BbsReplyPagedDto>.Fail("未登录", 401));
            var dto = await _service.GetRepliesAsync(id, page, pageSize, actor.Value.UserId, actor.Value.IsModerator, ct);
            return Ok(ApiResponse<BbsReplyPagedDto>.Ok(dto));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<BbsReplyPagedDto>.Fail(ex.Message, 404));
        }
    }

    [HttpPost("subjects/{id}/replies")]
    public async Task<ActionResult<ApiResponse<BbsReplyDto>>> AddReply(
        string id,
        [FromBody] BbsReplyCreateRequest request,
        CancellationToken ct)
    {
        try
        {
            var actor = await TryGetActorAsync();
            if (actor == null)
                return Unauthorized(ApiResponse<BbsReplyDto>.Fail("未登录", 401));
            var dto = await _service.AddReplyAsync(id, request, actor.Value.UserId, actor.Value.IsModerator, ct);
            return Ok(ApiResponse<BbsReplyDto>.Ok(dto));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<BbsReplyDto>.Fail(ex.Message, 404));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<BbsReplyDto>.Fail(ex.Message, 400));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<BbsReplyDto>.Fail(ex.Message, 400));
        }
    }

    [HttpDelete("replies/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteReply(string id, CancellationToken ct)
    {
        try
        {
            var actor = await TryGetActorAsync();
            if (actor == null)
                return Unauthorized(ApiResponse<object>.Fail("未登录", 401));
            await _service.DeleteReplyAsync(id, actor.Value.UserId, actor.Value.IsModerator, ct);
            return Ok(ApiResponse<object>.Ok(new { }));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<object>.Fail(ex.Message, 403));
        }
    }

    private async Task<ActionResult<ApiResponse<object>>> RunAsync(
        string id,
        Func<string, string, bool, CancellationToken, Task> action,
        CancellationToken ct)
    {
        try
        {
            var actor = await TryGetActorAsync();
            if (actor == null)
                return Unauthorized(ApiResponse<object>.Fail("未登录", 401));
            await action(id, actor.Value.UserId, actor.Value.IsModerator, ct);
            return Ok(ApiResponse<object>.Ok(new { }));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<object>.Fail(ex.Message, 403));
        }
    }
}
