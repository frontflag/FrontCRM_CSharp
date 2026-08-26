using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/report-params")]
public class ReportParamsController : ControllerBase
{
    private readonly IReportParamsService _service;
    private readonly ILogger<ReportParamsController> _logger;

    public ReportParamsController(IReportParamsService service, ILogger<ReportParamsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>打印页读取生效版本；任意登录用户可读，不要求参数管理权限。</summary>
    [HttpGet("effective-style-version")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<ReportParamsStyleVersionDto>>> GetEffectiveStyleVersion(
        CancellationToken ct)
    {
        try
        {
            var version = await _service.GetStyleVersionAsync(ct);
            return Ok(ApiResponse<ReportParamsStyleVersionDto>.Ok(
                new ReportParamsStyleVersionDto { StyleVersion = version },
                "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取报表生效样式版本失败");
            return StatusCode(500, ApiResponse<ReportParamsStyleVersionDto>.Fail("读取失败", 500));
        }
    }

    [HttpGet("style-version")]
    [RequirePermission("system.params.report.global.read")]
    public async Task<ActionResult<ApiResponse<ReportParamsStyleVersionDto>>> GetStyleVersion(CancellationToken ct)
    {
        try
        {
            var version = await _service.GetStyleVersionAsync(ct);
            return Ok(ApiResponse<ReportParamsStyleVersionDto>.Ok(
                new ReportParamsStyleVersionDto { StyleVersion = version },
                "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取报表样式版本失败");
            return StatusCode(500, ApiResponse<ReportParamsStyleVersionDto>.Fail("读取失败", 500));
        }
    }

    [HttpPut("style-version")]
    [RequirePermission("system.params.report.global.write")]
    public async Task<ActionResult<ApiResponse<ReportParamsStyleVersionDto>>> SetStyleVersion(
        [FromBody] SetReportParamsStyleVersionRequest? body,
        CancellationToken ct)
    {
        if (body == null)
            return BadRequest(ApiResponse<ReportParamsStyleVersionDto>.Fail("请求体为空", 400));

        try
        {
            var version = await _service.SetStyleVersionAsync(body.StyleVersion ?? string.Empty, ct);
            return Ok(ApiResponse<ReportParamsStyleVersionDto>.Ok(
                new ReportParamsStyleVersionDto { StyleVersion = version },
                "已保存"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<ReportParamsStyleVersionDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存报表样式版本失败");
            return StatusCode(500, ApiResponse<ReportParamsStyleVersionDto>.Fail("保存失败", 500));
        }
    }
}
