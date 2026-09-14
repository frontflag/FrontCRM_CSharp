using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.API.Services;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/industry-news")]
[Authorize]
public class IndustryNewsController : ControllerBase
{
    readonly IIndustryNewsService _service;
    readonly IRbacService _rbac;
    readonly IServiceScopeFactory _scopeFactory;
    readonly ILogger<IndustryNewsController> _logger;

    public IndustryNewsController(
        IIndustryNewsService service,
        IRbacService rbac,
        IServiceScopeFactory scopeFactory,
        ILogger<IndustryNewsController> logger)
    {
        _service = service;
        _rbac = rbac;
        _scopeFactory = scopeFactory;
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

        var latest = await _service.GetLatestAsync(cancellationToken);
        if (!IndustryNewsManualRunGate.TryBegin())
        {
            return Ok(ApiResponse<IndustryNewsRunResultDto>.Ok(new IndustryNewsRunResultDto
            {
                Ran = false,
                Success = true,
                Pending = true,
                Message = "正在生成，请稍候",
                Latest = latest
            }, "正在生成，请稍候"));
        }

        var runForce = force;
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var svc = scope.ServiceProvider.GetRequiredService<IIndustryNewsService>();
                await svc.RunForTodayAsync(runForce, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "后台生成行业新闻失败");
            }
            finally
            {
                IndustryNewsManualRunGate.End();
            }
        });

        return Ok(ApiResponse<IndustryNewsRunResultDto>.Ok(new IndustryNewsRunResultDto
        {
            Ran = true,
            Success = true,
            Pending = true,
            Message = "已开始生成",
            Latest = latest
        }, "已开始生成"));
    }
}
