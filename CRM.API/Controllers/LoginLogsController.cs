using System.Security.Claims;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/login-logs")]
[Authorize]
public class LoginLogsController : ControllerBase
{
    private readonly ILoginLogQueryService _queryService;
    private readonly IRbacService _rbacService;
    private readonly ILogger<LoginLogsController> _logger;

    public LoginLogsController(
        ILoginLogQueryService queryService,
        IRbacService rbacService,
        ILogger<LoginLogsController> logger)
    {
        _queryService = queryService;
        _rbacService = rbacService;
        _logger = logger;
    }

    [HttpGet]
    [RequirePermission("system.logs.login.read")]
    public async Task<ActionResult<ApiResponse<LoginLogPagedResult>>> List(
        [FromQuery] DateTime? loginAtFrom,
        [FromQuery] DateTime? loginAtTo,
        [FromQuery] string? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var summary = string.IsNullOrWhiteSpace(currentUserId)
                ? null
                : await _rbacService.GetUserPermissionSummaryAsync(currentUserId);

            var q = new LoginLogQuery
            {
                LoginAtFrom = loginAtFrom,
                LoginAtTo = loginAtTo,
                UserId = userId,
                Page = page,
                PageSize = pageSize,
                ViewerIsSysAdmin = summary?.IsSysAdmin == true
            };
            var data = await _queryService.QueryAsync(q, cancellationToken);
            return Ok(ApiResponse<LoginLogPagedResult>.Ok(data, "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "查询登录日志失败");
            return StatusCode(500, ApiResponse<LoginLogPagedResult>.Fail("查询失败"));
        }
    }
}
