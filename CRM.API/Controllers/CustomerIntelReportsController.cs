using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.API.Utilities;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/customer-intel-reports")]
public class CustomerIntelReportsController : ControllerBase
{
    private readonly ICustomerIntelReportService _service;
    private readonly ILogger<CustomerIntelReportsController> _logger;

    public CustomerIntelReportsController(
        ICustomerIntelReportService service,
        ILogger<CustomerIntelReportsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("investigate")]
    [RequirePermission("biz.ai.customer_intel.lookup")]
    public async Task<ActionResult<ApiResponse<CustomerIntelInvestigateResultDto>>> Investigate(
        [FromBody] CustomerIntelInvestigateRequest? body,
        CancellationToken ct)
    {
        if (body == null)
            return BadRequest(ApiResponse<CustomerIntelInvestigateResultDto>.Fail("请求体为空", 400));
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var result = await _service.InvestigateAsync(body, userId, ct);
            return Ok(ApiResponse<CustomerIntelInvestigateResultDto>.Ok(result, "ok"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<CustomerIntelInvestigateResultDto>.Fail(ex.Message, 400));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<CustomerIntelInvestigateResultDto>.Fail(ex.Message, 403));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<CustomerIntelInvestigateResultDto>.Fail(ex.Message, 400));
        }
        catch (DbUpdateException ex)
        {
            var detail = ApiExceptionMessages.FormatWithDatabaseInner(ex);
            _logger.LogError(ex, "客户情报报告写入失败");
            return StatusCode(500, ApiResponse<CustomerIntelInvestigateResultDto>.Fail(
                $"调查报告保存失败，请确认已执行 scripts/ai_customer_intel_lookup_postgresql.sql。详情: {detail}", 500));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "客户情报调查失败");
            return StatusCode(500, ApiResponse<CustomerIntelInvestigateResultDto>.Fail(ex.Message, 500));
        }
    }

    [HttpGet("latest")]
    [RequirePermission("biz.ai.customer_intel.lookup")]
    public async Task<ActionResult<ApiResponse<CustomerIntelReportDetailDto>>> GetLatestByQuery(
        [FromQuery] string companyName,
        [FromQuery] string? creditCode,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            return BadRequest(ApiResponse<CustomerIntelReportDetailDto>.Fail("companyName 不能为空", 400));
        var result = await _service.GetLatestByQueryAsync(companyName.Trim(), creditCode?.Trim(), ct);
        if (result == null)
            return Ok(ApiResponse<CustomerIntelReportDetailDto>.Ok(null!, "暂无报告"));
        return Ok(ApiResponse<CustomerIntelReportDetailDto>.Ok(result, "ok"));
    }

    [HttpGet("{id}")]
    [RequirePermission("biz.ai.customer_intel.lookup")]
    public async Task<ActionResult<ApiResponse<CustomerIntelReportDetailDto>>> GetById(string id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        if (result == null)
            return NotFound(ApiResponse<CustomerIntelReportDetailDto>.Fail("报告不存在", 404));
        return Ok(ApiResponse<CustomerIntelReportDetailDto>.Ok(result, "ok"));
    }
}
