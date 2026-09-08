using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/commission-rates")]
public class CommissionRatesController : ControllerBase
{
    private readonly ICommissionRateService _service;
    private readonly IRbacService _rbacService;
    private readonly ILogger<CommissionRatesController> _logger;

    public CommissionRatesController(
        ICommissionRateService service,
        IRbacService rbacService,
        ILogger<CommissionRatesController> logger)
    {
        _service = service;
        _rbacService = rbacService;
        _logger = logger;
    }

    public sealed class UpdateCommissionRateRequest
    {
        public string? Remark { get; set; }
        public List<CommissionLadderWriteDto>? Ladders { get; set; }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CommissionRateDto>>>> List(
        [FromQuery] short roleType,
        CancellationToken ct)
    {
        try
        {
            if (!await CanAccessAsync())
                return StatusCode(403, ApiResponse<List<CommissionRateDto>>.Fail("无权查看提成系数", 403));

            var items = await _service.ListAsync(roleType, ct);
            return Ok(ApiResponse<List<CommissionRateDto>>.Ok(items.ToList(), "OK"));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ApiResponse<List<CommissionRateDto>>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取提成系数失败");
            return StatusCode(500, ApiResponse<List<CommissionRateDto>>.Fail(ex.Message, 500));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<CommissionRateDto>>> Update(
        string id,
        [FromBody] UpdateCommissionRateRequest request,
        CancellationToken ct)
    {
        try
        {
            var existing = await _service.GetAsync(id, ct);
            if (existing == null)
                return NotFound(ApiResponse<CommissionRateDto>.Fail("记录不存在", 404));
            if (!await CanAccessAsync())
                return StatusCode(403, ApiResponse<CommissionRateDto>.Fail("无权维护提成系数", 403));

            var operatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var updated = await _service.UpdateAsync(
                id,
                request.Ladders ?? new List<CommissionLadderWriteDto>(),
                request.Remark,
                operatorId,
                ct);
            return Ok(ApiResponse<CommissionRateDto>.Ok(updated, "已保存"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<CommissionRateDto>.Fail(ex.Message, 404));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<CommissionRateDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存提成系数失败");
            return StatusCode(500, ApiResponse<CommissionRateDto>.Fail(ex.Message, 500));
        }
    }

    async Task<bool> CanAccessAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return false;
        var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
        return summary.CanForceDelete;
    }
}
