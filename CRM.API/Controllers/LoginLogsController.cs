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
    private readonly ILogger<LoginLogsController> _logger;

    public LoginLogsController(ILoginLogQueryService queryService, ILogger<LoginLogsController> logger)
    {
        _queryService = queryService;
        _logger = logger;
    }

    [HttpGet]
    [RequirePermission("rbac.manage")]
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
            var q = new LoginLogQuery
            {
                LoginAtFrom = loginAtFrom,
                LoginAtTo = loginAtTo,
                UserId = userId,
                Page = page,
                PageSize = pageSize
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
