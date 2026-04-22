using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/operation-logs")]
[Authorize]
public class OperationLogsController : ControllerBase
{
    private readonly IOperationLogQueryService _queryService;
    private readonly ILogger<OperationLogsController> _logger;

    public OperationLogsController(IOperationLogQueryService queryService, ILogger<OperationLogsController> logger)
    {
        _queryService = queryService;
        _logger = logger;
    }

    /// <summary>分页查询 log_operation。</summary>
    [HttpGet]
    [RequirePermission("rbac.manage")]
    public async Task<ActionResult<ApiResponse<OperationLogPagedResult>>> List(
        [FromQuery] string? bizType,
        [FromQuery] string? actionType,
        [FromQuery] string? recordCode,
        [FromQuery] string? operatorUserName,
        [FromQuery] DateTime? operationTimeFrom,
        [FromQuery] DateTime? operationTimeTo,
        [FromQuery] string? reason,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var q = new OperationLogQuery
            {
                BizType = bizType,
                ActionType = actionType,
                RecordCode = recordCode,
                OperatorUserName = operatorUserName,
                OperationTimeFrom = operationTimeFrom,
                OperationTimeTo = operationTimeTo,
                Reason = reason,
                Page = page,
                PageSize = pageSize
            };
            var data = await _queryService.QueryAsync(q, cancellationToken);
            return Ok(ApiResponse<OperationLogPagedResult>.Ok(data, "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "查询操作日志失败");
            return StatusCode(500, ApiResponse<OperationLogPagedResult>.Fail("查询失败"));
        }
    }
}
