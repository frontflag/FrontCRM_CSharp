using System.Security.Claims;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class WorkCalendarController : ControllerBase
{
    private readonly IWorkCalendarService _service;

    public WorkCalendarController(IWorkCalendarService service)
    {
        _service = service;
    }

    private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private bool IsImpersonating =>
        !string.IsNullOrWhiteSpace(User.FindFirst(ImpersonationClaimTypes.Impersonator)?.Value);

    [HttpGet("work-calendar/month")]
    public async Task<ActionResult<ApiResponse<WorkCalendarMonthDto>>> Month(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<WorkCalendarMonthDto>.Fail("未登录", 401));
        try
        {
            var dto = await _service.GetMonthAsync(uid, year, month, cancellationToken);
            return Ok(ApiResponse<WorkCalendarMonthDto>.Ok(dto));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<WorkCalendarMonthDto>.Fail(ex.Message, 400));
        }
    }

    [HttpGet("work-calendar/day")]
    public async Task<ActionResult<ApiResponse<WorkCalendarDayDto>>> Day(
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<WorkCalendarDayDto>.Fail("未登录", 401));
        var dto = await _service.GetDayAsync(uid, date, cancellationToken);
        return Ok(ApiResponse<WorkCalendarDayDto>.Ok(dto));
    }

    [HttpGet("work-tasks/assignees")]
    [RequirePermission(WorkTaskPermissionCodes.Write)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkTaskAssigneeOptionDto>>>> Assignees(
        CancellationToken cancellationToken)
    {
        var list = await _service.ListAssigneesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<WorkTaskAssigneeOptionDto>>.Ok(list));
    }

    [HttpPost("work-tasks")]
    [RequirePermission(WorkTaskPermissionCodes.Write)]
    public async Task<ActionResult<ApiResponse<WorkTaskDetailDto>>> Create(
        [FromBody] WorkTaskCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (IsImpersonating)
            return StatusCode(403, ApiResponse<WorkTaskDetailDto>.Fail("模拟登录不能改任务", 403));
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<WorkTaskDetailDto>.Fail("未登录", 401));
        try
        {
            var dto = await _service.CreateTaskAsync(uid, request, cancellationToken);
            return Ok(ApiResponse<WorkTaskDetailDto>.Ok(dto));
        }
        catch (Exception ex) when (IsClientError(ex))
        {
            return MapError<WorkTaskDetailDto>(ex);
        }
    }

    [HttpGet("work-tasks/{id}")]
    [RequirePermission(WorkTaskPermissionCodes.Read)]
    public async Task<ActionResult<ApiResponse<WorkTaskDetailDto>>> Get(
        string id,
        CancellationToken cancellationToken)
    {
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<WorkTaskDetailDto>.Fail("未登录", 401));
        try
        {
            var dto = await _service.GetTaskAsync(uid, id, cancellationToken);
            return Ok(ApiResponse<WorkTaskDetailDto>.Ok(dto));
        }
        catch (Exception ex) when (IsClientError(ex))
        {
            return MapError<WorkTaskDetailDto>(ex);
        }
    }

    [HttpPatch("work-tasks/{id}")]
    [RequirePermission(WorkTaskPermissionCodes.Write)]
    public async Task<ActionResult<ApiResponse<WorkTaskDetailDto>>> Patch(
        string id,
        [FromBody] WorkTaskPatchRequest request,
        CancellationToken cancellationToken)
    {
        if (IsImpersonating)
            return StatusCode(403, ApiResponse<WorkTaskDetailDto>.Fail("模拟登录不能改任务", 403));
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<WorkTaskDetailDto>.Fail("未登录", 401));
        try
        {
            var dto = await _service.PatchTaskAsync(uid, id, request, cancellationToken);
            return Ok(ApiResponse<WorkTaskDetailDto>.Ok(dto));
        }
        catch (Exception ex) when (IsClientError(ex))
        {
            return MapError<WorkTaskDetailDto>(ex);
        }
    }

    [HttpPost("work-tasks/{id}/start")]
    [RequirePermission(WorkTaskPermissionCodes.Write)]
    public Task<ActionResult<ApiResponse<WorkTaskDetailDto>>> Start(string id, CancellationToken cancellationToken) =>
        RunWrite(id, (uid, ct) => _service.StartTaskAsync(uid, id, ct), cancellationToken);

    [HttpPost("work-tasks/{id}/complete")]
    [RequirePermission(WorkTaskPermissionCodes.Write)]
    public Task<ActionResult<ApiResponse<WorkTaskDetailDto>>> Complete(string id, CancellationToken cancellationToken) =>
        RunWrite(id, (uid, ct) => _service.CompleteTaskAsync(uid, id, ct), cancellationToken);

    [HttpPost("work-tasks/{id}/cancel")]
    [RequirePermission(WorkTaskPermissionCodes.Write)]
    public Task<ActionResult<ApiResponse<WorkTaskDetailDto>>> Cancel(string id, CancellationToken cancellationToken) =>
        RunWrite(id, (uid, ct) => _service.CancelTaskAsync(uid, id, ct), cancellationToken);

    [HttpPost("work-tasks/{id}/delete")]
    [RequirePermission(WorkTaskPermissionCodes.Write)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id, CancellationToken cancellationToken)
    {
        if (IsImpersonating)
            return StatusCode(403, ApiResponse<object>.Fail("模拟登录不能改任务", 403));
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<object>.Fail("未登录", 401));
        try
        {
            await _service.SoftDeleteTaskAsync(uid, id, cancellationToken);
            return Ok(ApiResponse<object>.Ok(new { }, "已删除"));
        }
        catch (Exception ex) when (IsClientError(ex))
        {
            return MapError<object>(ex);
        }
    }

    private async Task<ActionResult<ApiResponse<WorkTaskDetailDto>>> RunWrite(
        string id,
        Func<string, CancellationToken, Task<WorkTaskDetailDto>> action,
        CancellationToken cancellationToken)
    {
        if (IsImpersonating)
            return StatusCode(403, ApiResponse<WorkTaskDetailDto>.Fail("模拟登录不能改任务", 403));
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<WorkTaskDetailDto>.Fail("未登录", 401));
        try
        {
            var dto = await action(uid, cancellationToken);
            return Ok(ApiResponse<WorkTaskDetailDto>.Ok(dto));
        }
        catch (Exception ex) when (IsClientError(ex))
        {
            return MapError<WorkTaskDetailDto>(ex);
        }
    }

    private static bool IsClientError(Exception ex) =>
        ex is ArgumentException or InvalidOperationException or KeyNotFoundException or UnauthorizedAccessException;

    private ActionResult<ApiResponse<T>> MapError<T>(Exception ex)
    {
        var code = ex switch
        {
            UnauthorizedAccessException => 403,
            KeyNotFoundException => 404,
            _ => 400
        };
        return StatusCode(code, ApiResponse<T>.Fail(ex.Message, code));
    }
}
