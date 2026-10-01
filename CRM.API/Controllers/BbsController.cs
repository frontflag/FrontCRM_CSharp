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

    private async Task<(BbsActorContext Actor, bool CanAssignModerator)?> TryGetActorAsync(CancellationToken ct = default)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId)) return null;
        var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
        var isGlobal = summary.IsSysAdmin
            || summary.IsSysManager
            || (summary.PermissionCodes?.Any(c =>
                string.Equals(c, BbsPermissionCodes.Moderate, StringComparison.OrdinalIgnoreCase)) ?? false);
        var moderated = await _service.GetModeratedTypesAsync(userId, ct);
        var actor = new BbsActorContext
        {
            UserId = userId,
            IsSysAdmin = summary.IsSysAdmin,
            IsGlobalModerator = isGlobal,
            ModeratedTypes = moderated
        };
        var canAssign = summary.IsSysAdmin || summary.IsSysManager;
        return (actor, canAssign);
    }

    [HttpGet("board-moderators")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BbsBoardModeratorDto>>>> ListBoardModerators(
        CancellationToken ct)
    {
        var actor = await TryGetActorAsync(ct);
        if (actor == null)
            return Unauthorized(ApiResponse<IReadOnlyList<BbsBoardModeratorDto>>.Fail("未登录", 401));
        var list = await _service.ListBoardModeratorsAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<BbsBoardModeratorDto>>.Ok(list));
    }

    [HttpPost("board-moderators")]
    public async Task<ActionResult<ApiResponse<BbsBoardModeratorDto>>> CreateBoard(
        [FromBody] BbsBoardCreateRequest? request,
        CancellationToken ct)
    {
        try
        {
            var actor = await TryGetActorAsync(ct);
            if (actor == null)
                return Unauthorized(ApiResponse<BbsBoardModeratorDto>.Fail("未登录", 401));
            if (!actor.Value.CanAssignModerator)
                return StatusCode(403, ApiResponse<BbsBoardModeratorDto>.Fail("仅系统管理员可新建板块", 403));
            var dto = await _service.CreateBoardAsync(
                request?.DisplayName ?? string.Empty,
                actor.Value.Actor.UserId,
                ct);
            return Ok(ApiResponse<BbsBoardModeratorDto>.Ok(dto));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<BbsBoardModeratorDto>.Fail(ex.Message, 400));
        }
    }

    [HttpPut("board-moderators/sort")]
    public async Task<ActionResult<ApiResponse<object>>> ReorderBoards(
        [FromBody] BbsBoardReorderRequest? request,
        CancellationToken ct)
    {
        try
        {
            var actor = await TryGetActorAsync(ct);
            if (actor == null)
                return Unauthorized(ApiResponse<object>.Fail("未登录", 401));
            if (!actor.Value.CanAssignModerator)
                return StatusCode(403, ApiResponse<object>.Fail("仅系统管理员可调整板块顺序", 403));
            await _service.ReorderBoardsAsync(
                request?.OrderedTypes ?? [],
                actor.Value.Actor.UserId,
                ct);
            return Ok(ApiResponse<object>.Ok(new { }, "顺序已保存"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    [HttpPut("board-moderators/{type:int}")]
    public async Task<ActionResult<ApiResponse<BbsBoardModeratorDto>>> SetBoardModerator(
        int type,
        [FromBody] BbsBoardModeratorSetRequest? request,
        CancellationToken ct)
    {
        try
        {
            var actor = await TryGetActorAsync(ct);
            if (actor == null)
                return Unauthorized(ApiResponse<BbsBoardModeratorDto>.Fail("未登录", 401));
            if (!actor.Value.CanAssignModerator)
                return StatusCode(403, ApiResponse<BbsBoardModeratorDto>.Fail("仅系统管理员可设置版主", 403));
            var dto = await _service.SetBoardModeratorAsync(
                type,
                request?.UserId,
                request?.DisplayName,
                request?.SortOrder,
                actor.Value.Actor.UserId,
                ct);
            return Ok(ApiResponse<BbsBoardModeratorDto>.Ok(dto));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<BbsBoardModeratorDto>.Fail(ex.Message, 400));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<BbsBoardModeratorDto>.Fail(ex.Message, 400));
        }
    }

    [HttpDelete("board-moderators/{type:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteBoard(int type, CancellationToken ct)
    {
        try
        {
            var actor = await TryGetActorAsync(ct);
            if (actor == null)
                return Unauthorized(ApiResponse<object>.Fail("未登录", 401));
            if (!actor.Value.CanAssignModerator)
                return StatusCode(403, ApiResponse<object>.Fail("仅系统管理员可删除板块", 403));
            await _service.DeleteBoardAsync(type, actor.Value.Actor.UserId, ct);
            return Ok(ApiResponse<object>.Ok(new { }, "板块已删除"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    [HttpGet("subjects/top")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BbsSubjectListItemDto>>>> GetTop(CancellationToken ct)
    {
        var actor = await TryGetActorAsync(ct);
        if (actor == null)
            return Unauthorized(ApiResponse<IReadOnlyList<BbsSubjectListItemDto>>.Fail("未登录", 401));
        var list = await _service.GetTopSubjectsAsync(actor.Value.Actor, ct);
        return Ok(ApiResponse<IReadOnlyList<BbsSubjectListItemDto>>.Ok(list));
    }

    [HttpGet("board-stats")]
    public async Task<ActionResult<ApiResponse<BbsBoardStatsDto>>> BoardStats(CancellationToken ct)
    {
        var actor = await TryGetActorAsync(ct);
        if (actor == null)
            return Unauthorized(ApiResponse<BbsBoardStatsDto>.Fail("未登录", 401));
        var dto = await _service.GetBoardStatsAsync(ct);
        return Ok(ApiResponse<BbsBoardStatsDto>.Ok(dto));
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
        var actor = await TryGetActorAsync(ct);
        if (actor == null)
            return Unauthorized(ApiResponse<BbsSubjectPagedDto>.Fail("未登录", 401));
        var dto = await _service.QuerySubjectsAsync(new BbsSubjectQuery
        {
            Type = type,
            Status = status,
            Keyword = keyword,
            Page = page,
            PageSize = pageSize
        }, actor.Value.Actor, ct);
        return Ok(ApiResponse<BbsSubjectPagedDto>.Ok(dto));
    }

    [HttpGet("subjects/{id}")]
    public async Task<ActionResult<ApiResponse<BbsSubjectDetailDto>>> Detail(string id, CancellationToken ct)
    {
        var actor = await TryGetActorAsync(ct);
        if (actor == null)
            return Unauthorized(ApiResponse<BbsSubjectDetailDto>.Fail("未登录", 401));
        var dto = await _service.GetSubjectDetailAsync(id, actor.Value.Actor, ct);
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
            var actor = await TryGetActorAsync(ct);
            if (actor == null)
                return Unauthorized(ApiResponse<BbsSubjectDetailDto>.Fail("未登录", 401));
            var dto = await _service.CreateSubjectAsync(request, actor.Value.Actor, ct);
            return Ok(ApiResponse<BbsSubjectDetailDto>.Ok(dto));
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

    [HttpPut("subjects/{id}")]
    public async Task<ActionResult<ApiResponse<BbsSubjectDetailDto>>> Update(
        string id,
        [FromBody] BbsSubjectUpdateRequest request,
        CancellationToken ct)
    {
        try
        {
            var actor = await TryGetActorAsync(ct);
            if (actor == null)
                return Unauthorized(ApiResponse<BbsSubjectDetailDto>.Fail("未登录", 401));
            var dto = await _service.UpdateSubjectAsync(id, request, actor.Value.Actor, ct);
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
        => await RunAsync(id, (s, a, c) => _service.CloseSubjectAsync(s, a, c), ct);

    [HttpPost("subjects/{id}/open")]
    public async Task<ActionResult<ApiResponse<object>>> Open(string id, CancellationToken ct)
        => await RunAsync(id, (s, a, c) => _service.OpenSubjectAsync(s, a, c), ct);

    [HttpPost("subjects/{id}/top")]
    public async Task<ActionResult<ApiResponse<object>>> SetTop(string id, CancellationToken ct)
        => await RunAsync(id, (s, a, c) => _service.SetTopAsync(s, a, c), ct);

    [HttpPost("subjects/{id}/untop")]
    public async Task<ActionResult<ApiResponse<object>>> Untop(string id, CancellationToken ct)
        => await RunAsync(id, (s, a, c) => _service.CancelTopAsync(s, a, c), ct);

    [HttpDelete("subjects/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteSubject(string id, CancellationToken ct)
        => await RunAsync(id, (s, a, c) => _service.DeleteSubjectAsync(s, a, c), ct);

    [HttpGet("subjects/{id}/replies")]
    public async Task<ActionResult<ApiResponse<BbsReplyPagedDto>>> Replies(
        string id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        try
        {
            var actor = await TryGetActorAsync(ct);
            if (actor == null)
                return Unauthorized(ApiResponse<BbsReplyPagedDto>.Fail("未登录", 401));
            var dto = await _service.GetRepliesAsync(id, page, pageSize, actor.Value.Actor, ct);
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
            var actor = await TryGetActorAsync(ct);
            if (actor == null)
                return Unauthorized(ApiResponse<BbsReplyDto>.Fail("未登录", 401));
            var dto = await _service.AddReplyAsync(id, request, actor.Value.Actor, ct);
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
            var actor = await TryGetActorAsync(ct);
            if (actor == null)
                return Unauthorized(ApiResponse<object>.Fail("未登录", 401));
            await _service.DeleteReplyAsync(id, actor.Value.Actor, ct);
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

    [HttpGet("subjects/{id}/media")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BbsMediaItemDto>>>> ListMedia(string id, CancellationToken ct)
    {
        try
        {
            var actor = await TryGetActorAsync(ct);
            if (actor == null)
                return Unauthorized(ApiResponse<IReadOnlyList<BbsMediaItemDto>>.Fail("未登录", 401));
            var list = await _service.ListSubjectMediaAsync(id, ct);
            return Ok(ApiResponse<IReadOnlyList<BbsMediaItemDto>>.Ok(list));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<IReadOnlyList<BbsMediaItemDto>>.Fail(ex.Message, 404));
        }
    }

    [HttpPost("subjects/{id}/media")]
    [RequestSizeLimit(60 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 60 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BbsMediaItemDto>>>> UploadMedia(
        string id,
        [FromForm] IFormFileCollection? files,
        CancellationToken ct)
    {
        try
        {
            var actor = await TryGetActorAsync(ct);
            if (actor == null)
                return Unauthorized(ApiResponse<IReadOnlyList<BbsMediaItemDto>>.Fail("未登录", 401));

            var list = new List<BbsMediaUploadFile>();
            if (files != null)
            {
                foreach (var f in files)
                {
                    if (f.Length <= 0) continue;
                    var stream = new MemoryStream();
                    await f.CopyToAsync(stream, ct);
                    stream.Position = 0;
                    list.Add(new BbsMediaUploadFile
                    {
                        Stream = stream,
                        FileName = f.FileName ?? "file",
                        ContentType = f.ContentType,
                        Length = f.Length
                    });
                }
            }

            var saved = await _service.UploadSubjectMediaAsync(id, list, actor.Value.Actor, ct);
            return Ok(ApiResponse<IReadOnlyList<BbsMediaItemDto>>.Ok(saved, "上传成功"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<IReadOnlyList<BbsMediaItemDto>>.Fail(ex.Message, 404));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<IReadOnlyList<BbsMediaItemDto>>.Fail(ex.Message, 403));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<IReadOnlyList<BbsMediaItemDto>>.Fail(ex.Message, 400));
        }
    }

    [HttpDelete("media/{documentId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteMedia(string documentId, CancellationToken ct)
    {
        try
        {
            var actor = await TryGetActorAsync(ct);
            if (actor == null)
                return Unauthorized(ApiResponse<object>.Fail("未登录", 401));
            await _service.DeleteSubjectMediaAsync(documentId, actor.Value.Actor, ct);
            return Ok(ApiResponse<object>.Ok(new { }, "已删除"));
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

    [HttpPost("subjects/{id}/reaction")]
    public async Task<ActionResult<ApiResponse<BbsReactionResultDto>>> SubjectReaction(
        string id,
        [FromBody] BbsReactionRequest request,
        CancellationToken ct)
    {
        try
        {
            var actor = await TryGetActorAsync(ct);
            if (actor == null)
                return Unauthorized(ApiResponse<BbsReactionResultDto>.Fail("未登录", 401));
            var dto = await _service.SetSubjectReactionAsync(id, request.Value, actor.Value.Actor.UserId, ct);
            return Ok(ApiResponse<BbsReactionResultDto>.Ok(dto));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<BbsReactionResultDto>.Fail(ex.Message, 404));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<BbsReactionResultDto>.Fail(ex.Message, 400));
        }
    }

    private async Task<ActionResult<ApiResponse<object>>> RunAsync(
        string id,
        Func<string, BbsActorContext, CancellationToken, Task> action,
        CancellationToken ct)
    {
        try
        {
            var actor = await TryGetActorAsync(ct);
            if (actor == null)
                return Unauthorized(ApiResponse<object>.Fail("未登录", 401));
            await action(id, actor.Value.Actor, ct);
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
