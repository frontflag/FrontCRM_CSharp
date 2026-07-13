using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.API.Utilities;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/vendor-intel-reports")]
public class VendorIntelReportsController : ControllerBase
{
    private readonly IVendorIntelReportService _service;
    private readonly ILogger<VendorIntelReportsController> _logger;

    public VendorIntelReportsController(
        IVendorIntelReportService service,
        ILogger<VendorIntelReportsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("investigate")]
    [RequirePermission("biz.ai.vendor_intel.lookup")]
    public async Task<ActionResult<ApiResponse<VendorIntelInvestigateResultDto>>> Investigate(
        [FromBody] VendorIntelInvestigateRequest? body,
        CancellationToken ct)
    {
        if (body == null)
            return BadRequest(ApiResponse<VendorIntelInvestigateResultDto>.Fail("请求体为空", 400));
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var result = await _service.InvestigateAsync(body, userId, ct);
            return Ok(ApiResponse<VendorIntelInvestigateResultDto>.Ok(result, "ok"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<VendorIntelInvestigateResultDto>.Fail(ex.Message, 400));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<VendorIntelInvestigateResultDto>.Fail(ex.Message, 403));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<VendorIntelInvestigateResultDto>.Fail(ex.Message, 400));
        }
        catch (DbUpdateException ex)
        {
            var detail = ApiExceptionMessages.FormatWithDatabaseInner(ex);
            _logger.LogError(ex, "供应商情报报告写入失败");
            return StatusCode(500, ApiResponse<VendorIntelInvestigateResultDto>.Fail(
                $"调查报告保存失败，请确认已执行 scripts/ai_vendor_intel_lookup_postgresql.sql。详情: {detail}", 500));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "供应商情报调查失败");
            return StatusCode(500, ApiResponse<VendorIntelInvestigateResultDto>.Fail(ex.Message, 500));
        }
    }

    [HttpGet("latest")]
    [RequirePermission("biz.ai.vendor_intel.lookup")]
    public async Task<ActionResult<ApiResponse<VendorIntelReportDetailDto>>> GetLatestByQuery(
        [FromQuery] string companyName,
        [FromQuery] string? creditCode,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            return BadRequest(ApiResponse<VendorIntelReportDetailDto>.Fail("companyName 不能为空", 400));
        var result = await _service.GetLatestByQueryAsync(companyName.Trim(), creditCode?.Trim(), ct);
        if (result == null)
            return Ok(ApiResponse<VendorIntelReportDetailDto>.Ok(null!, "暂无报告"));
        return Ok(ApiResponse<VendorIntelReportDetailDto>.Ok(result, "ok"));
    }

    [HttpGet("{id}")]
    [RequirePermission("biz.ai.vendor_intel.lookup")]
    public async Task<ActionResult<ApiResponse<VendorIntelReportDetailDto>>> GetById(string id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        if (result == null)
            return NotFound(ApiResponse<VendorIntelReportDetailDto>.Fail("报告不存在", 404));
        return Ok(ApiResponse<VendorIntelReportDetailDto>.Ok(result, "ok"));
    }
}
