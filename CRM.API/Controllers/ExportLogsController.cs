using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Dtos;
using CRM.Core.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/export-logs")]
[Authorize]
public class ExportLogsController : ControllerBase
{
    private readonly IOperationLogQueryService _queryService;
    private readonly ILogger<ExportLogsController> _logger;

    public ExportLogsController(IOperationLogQueryService queryService, ILogger<ExportLogsController> logger)
    {
        _queryService = queryService;
        _logger = logger;
    }

    [HttpGet("kinds")]
    [RequirePermission("system.logs.export.read")]
    public ActionResult<ApiResponse<IReadOnlyList<ExportKindOptionDto>>> Kinds()
    {
        var items = ExportKindCatalog.All
            .Select(x => new ExportKindOptionDto { Kind = x.Kind, Name = x.BusinessTypeName })
            .ToList();
        return Ok(ApiResponse<IReadOnlyList<ExportKindOptionDto>>.Ok(items, "ok"));
    }

    [HttpGet]
    [RequirePermission("system.logs.export.read")]
    public async Task<ActionResult<ApiResponse<ExportLogPagedResult>>> List(
        [FromQuery] string? exportKind,
        [FromQuery] string? operatorUserName,
        [FromQuery] DateTime? operationTimeFrom,
        [FromQuery] DateTime? operationTimeTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await _queryService.QueryExportLogsAsync(
                new ExportLogQuery
                {
                    ExportKind = exportKind,
                    OperatorUserName = operatorUserName,
                    OperationTimeFrom = operationTimeFrom,
                    OperationTimeTo = operationTimeTo,
                    Page = page,
                    PageSize = pageSize
                },
                cancellationToken);
            return Ok(ApiResponse<ExportLogPagedResult>.Ok(data, "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "查询导出日志失败");
            return StatusCode(500, ApiResponse<ExportLogPagedResult>.Fail("查询失败"));
        }
    }
}
