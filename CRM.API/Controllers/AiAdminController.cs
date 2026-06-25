using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/ai/admin")]
[Route("api/v1/ai/mgmt")]
[Authorize]
public class AiAdminController : ControllerBase
{
    private readonly IAiAdminService _adminService;
    private readonly IAiEntityParseLogService _entityParseLogService;
    private readonly ILogger<AiAdminController> _logger;

    public AiAdminController(
        IAiAdminService adminService,
        IAiEntityParseLogService entityParseLogService,
        ILogger<AiAdminController> logger)
    {
        _adminService = adminService;
        _entityParseLogService = entityParseLogService;
        _logger = logger;
    }

    [HttpGet("providers")]
    [RequirePermission("biz.ai.admin")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AiProviderAdminDto>>>> ListProviders(
        CancellationToken cancellationToken)
    {
        var list = await _adminService.ListProvidersAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AiProviderAdminDto>>.Ok(list, "ok"));
    }

    [HttpPut("providers/{id}")]
    [RequirePermission("biz.ai.admin")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateProvider(
        string id,
        [FromBody] AiProviderAdminDto dto,
        CancellationToken cancellationToken)
    {
        dto.Id = id;
        try
        {
            await _adminService.UpdateProviderAsync(dto, cancellationToken);
            return Ok(ApiResponse<object>.Ok(null, "已保存"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新 AI 厂商失败 id={Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("保存失败"));
        }
    }

    [HttpGet("templates")]
    [RequirePermission("biz.ai.admin")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AiPromptTemplateAdminDto>>>> ListTemplates(
        CancellationToken cancellationToken)
    {
        var list = await _adminService.ListTemplatesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AiPromptTemplateAdminDto>>.Ok(list, "ok"));
    }

    [HttpPut("templates/{id}")]
    [RequirePermission("biz.ai.admin")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateTemplate(
        string id,
        [FromBody] AiPromptTemplateAdminDto dto,
        CancellationToken cancellationToken)
    {
        dto.Id = id;
        try
        {
            await _adminService.UpdateTemplateAsync(dto, cancellationToken);
            return Ok(ApiResponse<object>.Ok(null, "已保存"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新 AI 模板失败 id={Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("保存失败"));
        }
    }

    [HttpGet("scenarios")]
    [RequirePermission("biz.ai.admin")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AiScenarioAdminDto>>>> ListScenarios(
        CancellationToken cancellationToken)
    {
        try
        {
            var list = await _adminService.ListScenariosAsync(cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<AiScenarioAdminDto>>.Ok(list, "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "加载 AI 场景失败");
            var schemaMsg = TryMapAiScenarioSchemaError(ex);
            return StatusCode(500, ApiResponse<IReadOnlyList<AiScenarioAdminDto>>.Fail(schemaMsg ?? "加载场景失败"));
        }
    }

    [HttpPut("scenarios/{id}")]
    [RequirePermission("biz.ai.admin")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateScenario(
        string id,
        [FromBody] AiScenarioAdminDto dto,
        CancellationToken cancellationToken)
    {
        dto.Id = id;
        try
        {
            await _adminService.UpdateScenarioAsync(dto, cancellationToken);
            return Ok(ApiResponse<object>.Ok(null, "已保存"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新 AI 场景失败 id={Id}", id);
            var schemaMsg = TryMapAiScenarioSchemaError(ex);
            return StatusCode(500, ApiResponse<object>.Fail(schemaMsg ?? "保存失败"));
        }
    }

    private static string? TryMapAiScenarioSchemaError(Exception ex)
    {
        for (var cur = ex; cur != null; cur = cur.InnerException)
        {
            if (cur is PostgresException pg
                && pg.SqlState == PostgresErrorCodes.UndefinedColumn
                && pg.MessageText.Contains("enable_web_search", StringComparison.OrdinalIgnoreCase))
            {
                return "数据库缺少字段 enable_web_search。请在 PostgreSQL 执行 scripts/ai_scenario_enable_web_search_postgresql.sql（或 ai_material_intel_lookup_postgresql.sql 末尾 ALTER TABLE），然后刷新本页再保存。";
            }
        }

        return null;
    }

    [HttpGet("logs")]
    [RequirePermission("biz.ai.admin")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AiInvocationLogListItemDto>>>> ListLogs(
        [FromQuery] int take = 50,
        [FromQuery] string? scenarioCode = null,
        CancellationToken cancellationToken = default)
    {
        var list = await _adminService.ListInvocationLogsAsync(take, scenarioCode, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AiInvocationLogListItemDto>>.Ok(list, "ok"));
    }

    [HttpGet("usage")]
    [RequirePermission("biz.ai.admin")]
    public async Task<ActionResult<ApiResponse<AiUsageSummaryDto>>> GetUsage(
        CancellationToken cancellationToken)
    {
        var summary = await _adminService.GetUsageSummaryAsync(cancellationToken);
        return Ok(ApiResponse<AiUsageSummaryDto>.Ok(summary, "ok"));
    }

    [HttpGet("entity-parse-logs")]
    [RequirePermission("biz.ai.admin")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AiEntityParseLogListItemDto>>>> ListEntityParseLogs(
        [FromQuery] int take = 50,
        [FromQuery] string? scenarioCode = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? outcome = null,
        [FromQuery] string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var list = await _entityParseLogService.ListForAdminAsync(new AiEntityParseLogQueryDto
        {
            Take = take,
            ScenarioCode = scenarioCode,
            EntityType = entityType,
            Outcome = outcome,
            UserId = userId
        }, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AiEntityParseLogListItemDto>>.Ok(list, "ok"));
    }

    [HttpGet("entity-parse-logs/export")]
    [RequirePermission("biz.ai.admin")]
    public async Task<IActionResult> ExportEntityParseLogs(
        [FromQuery] int take = 500,
        [FromQuery] string? scenarioCode = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? outcome = null,
        [FromQuery] string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var bytes = await _entityParseLogService.ExportCsvAsync(new AiEntityParseLogQueryDto
        {
            Take = take,
            ScenarioCode = scenarioCode,
            EntityType = entityType,
            Outcome = outcome,
            UserId = userId
        }, cancellationToken);
        var fileName = $"ai_entity_parse_logs_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    [HttpGet("entity-parse-logs/{id}")]
    [RequirePermission("biz.ai.admin")]
    public async Task<ActionResult<ApiResponse<AiEntityParseLogDetailDto>>> GetEntityParseLogDetail(
        string id,
        CancellationToken cancellationToken)
    {
        var detail = await _entityParseLogService.GetDetailForAdminAsync(id, cancellationToken);
        if (detail == null)
            return NotFound(ApiResponse<AiEntityParseLogDetailDto>.Fail("记录不存在", 404));
        return Ok(ApiResponse<AiEntityParseLogDetailDto>.Ok(detail, "ok"));
    }

    [HttpPost("entity-parse-logs/purge")]
    [RequirePermission("biz.ai.admin")]
    public async Task<ActionResult<ApiResponse<object>>> PurgeEntityParseLogs(
        [FromQuery] int keepDays = 180,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await _entityParseLogService.PurgeOlderThanAsync(keepDays, cancellationToken);
            return Ok(ApiResponse<object>.Ok(new { deleted, keepDays }, $"已清理 {deleted} 条记录"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Purge entity parse logs failed keepDays={KeepDays}", keepDays);
            return StatusCode(500, ApiResponse<object>.Fail("清理失败"));
        }
    }
}
