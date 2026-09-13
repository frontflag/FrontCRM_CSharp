using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/industry-news")]
[Authorize]
public class IndustryNewsController : ControllerBase
{
    readonly IIndustryNewsService _service;
    readonly IRbacService _rbac;
    readonly ILogger<IndustryNewsController> _logger;

    public IndustryNewsController(
        IIndustryNewsService service,
        IRbacService rbac,
        ILogger<IndustryNewsController> logger)
    {
        _service = service;
        _rbac = rbac;
        _logger = logger;
    }

    [HttpGet("latest")]
    public async Task<ActionResult<ApiResponse<IndustryNewsLatestDto>>> GetLatest(
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(User.FindFirst(ClaimTypes.NameIdentifier)?.Value))
            return Unauthorized(ApiResponse<IndustryNewsLatestDto>.Fail("未登录", 401));

        try
        {
            var dto = await _service.GetLatestAsync(cancellationToken);
            return Ok(ApiResponse<IndustryNewsLatestDto>.Ok(dto, "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取行业新闻失败");
            return StatusCode(500, ApiResponse<IndustryNewsLatestDto>.Fail("读取失败", 500));
        }
    }

    [HttpPost("run")]
    public async Task<ActionResult<ApiResponse<IndustryNewsRunResultDto>>> Run(
        [FromQuery] bool force = true,
        CancellationToken cancellationToken = default)
    {
        var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<IndustryNewsRunResultDto>.Fail("未登录", 401));

        var summary = await _rbac.GetUserPermissionSummaryAsync(uid);
        if (!summary.IsSysAdmin)
            return StatusCode(403, ApiResponse<IndustryNewsRunResultDto>.Fail("仅系统管理员可立即刷新", 403));

        try
        {
            var dto = await _service.RunForTodayAsync(force, cancellationToken);
            return Ok(ApiResponse<IndustryNewsRunResultDto>.Ok(dto, dto.Success ? "ok" : dto.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "手动生成行业新闻失败");
            return StatusCode(500, ApiResponse<IndustryNewsRunResultDto>.Fail("生成失败", 500));
        }
    }
}
