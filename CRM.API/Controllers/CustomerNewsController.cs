using System.Security.Claims;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.API.Services;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/customers/{customerId}/news")]
[Authorize]
[RequirePermission("customer.read")]
public class CustomerNewsController : ControllerBase
{
    readonly ICustomerNewsMonitorService _service;
    readonly IServiceScopeFactory _scopeFactory;
    readonly ILogger<CustomerNewsController> _logger;

    public CustomerNewsController(
        ICustomerNewsMonitorService service,
        IServiceScopeFactory scopeFactory,
        ILogger<CustomerNewsController> logger)
    {
        _service = service;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<CustomerNewsListDto>>> List(
        string customerId,
        CancellationToken cancellationToken)
    {
        var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<CustomerNewsListDto>.Fail("未登录", 401));

        try
        {
            var dto = await _service.ListAsync(customerId, uid, cancellationToken);
            dto.IsRunning = CustomerNewsManualRunGate.IsRunning(customerId);
            return Ok(ApiResponse<CustomerNewsListDto>.Ok(dto, "ok"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<CustomerNewsListDto>.Fail("客户不存在", 404));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<CustomerNewsListDto>.Fail(ex.Message, 403));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取客户新闻列表失败 customerId={CustomerId}", customerId);
            return StatusCode(500, ApiResponse<CustomerNewsListDto>.Fail("读取失败", 500));
        }
    }

    [HttpGet("latest-run")]
    public async Task<ActionResult<ApiResponse<CustomerNewsLatestRunDto>>> LatestRun(
        string customerId,
        CancellationToken cancellationToken)
    {
        var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<CustomerNewsLatestRunDto>.Fail("未登录", 401));

        try
        {
            var dto = await _service.GetLatestRunAsync(customerId, uid, cancellationToken);
            dto.IsRunning = CustomerNewsManualRunGate.IsRunning(customerId);
            return Ok(ApiResponse<CustomerNewsLatestRunDto>.Ok(dto, "ok"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<CustomerNewsLatestRunDto>.Fail("客户不存在", 404));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<CustomerNewsLatestRunDto>.Fail(ex.Message, 403));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取客户新闻最近一次抓取失败 customerId={CustomerId}", customerId);
            return StatusCode(500, ApiResponse<CustomerNewsLatestRunDto>.Fail("读取失败", 500));
        }
    }

    [HttpGet("{briefingId}")]
    public async Task<ActionResult<ApiResponse<CustomerNewsDetailDto>>> GetById(
        string customerId,
        string briefingId,
        CancellationToken cancellationToken)
    {
        var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<CustomerNewsDetailDto>.Fail("未登录", 401));

        try
        {
            var dto = await _service.GetByIdAsync(customerId, briefingId, uid, cancellationToken);
            if (dto == null)
                return NotFound(ApiResponse<CustomerNewsDetailDto>.Fail("没有该次简报", 404));
            return Ok(ApiResponse<CustomerNewsDetailDto>.Ok(dto, "ok"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<CustomerNewsDetailDto>.Fail("客户不存在", 404));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<CustomerNewsDetailDto>.Fail(ex.Message, 403));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取客户新闻失败 customerId={CustomerId} id={Id}", customerId, briefingId);
            return StatusCode(500, ApiResponse<CustomerNewsDetailDto>.Fail("读取失败", 500));
        }
    }

    [HttpPost("run")]
    public async Task<ActionResult<ApiResponse<CustomerNewsRunResultDto>>> Run(
        string customerId,
        CancellationToken cancellationToken)
    {
        var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<CustomerNewsRunResultDto>.Fail("未登录", 401));

        try
        {
            var list = await _service.ListAsync(customerId, uid, cancellationToken);
            if (!list.CanFetch)
                return StatusCode(403, ApiResponse<CustomerNewsRunResultDto>.Fail("无权抓取该客户新闻动态", 403));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<CustomerNewsRunResultDto>.Fail("客户不存在", 404));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<CustomerNewsRunResultDto>.Fail(ex.Message, 403));
        }

        var acceptedAt = DateTime.UtcNow;
        if (!CustomerNewsManualRunGate.TryBegin(customerId))
        {
            return Ok(ApiResponse<CustomerNewsRunResultDto>.Ok(new CustomerNewsRunResultDto
            {
                Ran = false,
                Success = true,
                Pending = true,
                Message = "正在抓取，请稍候",
                AcceptedAt = acceptedAt
            }, "正在抓取，请稍候"));
        }

        var cid = customerId;
        var actor = uid;
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var svc = scope.ServiceProvider.GetRequiredService<ICustomerNewsMonitorService>();
                await svc.RunAsync(cid, actor, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "后台抓取客户新闻失败 customerId={CustomerId}", customerId);
            }
            finally
            {
                CustomerNewsManualRunGate.End(cid);
            }
        });

        return Ok(ApiResponse<CustomerNewsRunResultDto>.Ok(new CustomerNewsRunResultDto
        {
            Ran = true,
            Success = true,
            Pending = true,
            Message = "已开始抓取",
            AcceptedAt = acceptedAt
        }, "已开始抓取"));
    }

    [HttpDelete("{briefingId}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        string customerId,
        string briefingId,
        CancellationToken cancellationToken)
    {
        var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<object>.Fail("未登录", 401));

        if (CustomerNewsManualRunGate.IsRunning(customerId))
            return StatusCode(409, ApiResponse<object>.Fail("正在抓取，请稍候后再删除", 409));

        try
        {
            await _service.DeleteAsync(customerId, briefingId, uid, cancellationToken);
            return Ok(ApiResponse<object>.Ok(new { }, "已删除"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(
                string.IsNullOrWhiteSpace(ex.Message) ? "没有该次简报" : ex.Message,
                404));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<object>.Fail(ex.Message, 403));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除客户新闻失败 customerId={CustomerId} id={Id}", customerId, briefingId);
            return StatusCode(500, ApiResponse<object>.Fail("删除失败", 500));
        }
    }
}
