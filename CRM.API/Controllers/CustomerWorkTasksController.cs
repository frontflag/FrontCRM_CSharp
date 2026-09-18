using System.Security.Claims;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/customers/{customerId}/work-tasks")]
[Authorize]
[RequirePermission("customer.read")]
public class CustomerWorkTasksController : ControllerBase
{
    private readonly IWorkCalendarService _service;

    public CustomerWorkTasksController(IWorkCalendarService service)
    {
        _service = service;
    }

    private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet("month")]
    public async Task<ActionResult<ApiResponse<CustomerWorkTaskMonthDto>>> Month(
        string customerId,
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] bool includeCancelled = false,
        CancellationToken cancellationToken = default)
    {
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<CustomerWorkTaskMonthDto>.Fail("未登录", 401));
        try
        {
            var dto = await _service.GetCustomerMonthAsync(
                uid, customerId, year, month, includeCancelled, cancellationToken);
            return Ok(ApiResponse<CustomerWorkTaskMonthDto>.Ok(dto));
        }
        catch (Exception ex) when (IsClientError(ex))
        {
            return MapError<CustomerWorkTaskMonthDto>(ex);
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedCustomerWorkTasksDto>>> List(
        string customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] bool includeCancelled = false,
        CancellationToken cancellationToken = default)
    {
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<PagedCustomerWorkTasksDto>.Fail("未登录", 401));
        try
        {
            var dto = await _service.ListCustomerTasksAsync(
                uid, customerId, page, pageSize, startDate, includeCancelled, cancellationToken);
            return Ok(ApiResponse<PagedCustomerWorkTasksDto>.Ok(dto));
        }
        catch (Exception ex) when (IsClientError(ex))
        {
            return MapError<PagedCustomerWorkTasksDto>(ex);
        }
    }

    [HttpGet("{taskId}")]
    public async Task<ActionResult<ApiResponse<CustomerWorkTaskItemDto>>> Get(
        string customerId,
        string taskId,
        CancellationToken cancellationToken = default)
    {
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<CustomerWorkTaskItemDto>.Fail("未登录", 401));
        try
        {
            var dto = await _service.GetCustomerTaskAsync(uid, customerId, taskId, cancellationToken);
            return Ok(ApiResponse<CustomerWorkTaskItemDto>.Ok(dto));
        }
        catch (Exception ex) when (IsClientError(ex))
        {
            return MapError<CustomerWorkTaskItemDto>(ex);
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
