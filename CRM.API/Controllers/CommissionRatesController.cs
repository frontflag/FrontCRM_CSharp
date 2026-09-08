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

    public sealed class CreateCommissionRateVersionRequest
    {
        public short RoleType { get; set; }
        public string? Remark { get; set; }
        public string? CopyFromVersionId { get; set; }
    }

    public sealed class UpdateCommissionRateVersionRequest
    {
        public string? Remark { get; set; }
    }

    public sealed class SetActiveCommissionRateVersionsRequest
    {
        public string? SalesVersionId { get; set; }
        public string? PurchaseVersionId { get; set; }
        public int ReceiptWriteoffDelayDays { get; set; }
    }

    [HttpGet("settings")]
    public async Task<ActionResult<ApiResponse<CommissionRateSettingsDto>>> Settings(CancellationToken ct)
    {
        try
        {
            if (!await CanAccessAsync())
                return StatusCode(403, ApiResponse<CommissionRateSettingsDto>.Fail("无权查看提成系数", 403));

            var data = await _service.GetSettingsAsync(ct);
            return Ok(ApiResponse<CommissionRateSettingsDto>.Ok(data, "OK"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取提成系数版本失败");
            return StatusCode(500, ApiResponse<CommissionRateSettingsDto>.Fail(ex.Message, 500));
        }
    }

    [HttpGet("versions")]
    public async Task<ActionResult<ApiResponse<List<CommissionRateVersionDto>>>> ListVersions(
        [FromQuery] short roleType,
        CancellationToken ct)
    {
        try
        {
            if (!await CanAccessAsync())
                return StatusCode(403, ApiResponse<List<CommissionRateVersionDto>>.Fail("无权查看提成系数", 403));

            var items = await _service.ListVersionsAsync(roleType, ct);
            return Ok(ApiResponse<List<CommissionRateVersionDto>>.Ok(items.ToList(), "OK"));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ApiResponse<List<CommissionRateVersionDto>>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取提成系数版本失败");
            return StatusCode(500, ApiResponse<List<CommissionRateVersionDto>>.Fail(ex.Message, 500));
        }
    }

    [HttpPost("versions")]
    public async Task<ActionResult<ApiResponse<CommissionRateVersionDto>>> CreateVersion(
        [FromBody] CreateCommissionRateVersionRequest request,
        CancellationToken ct)
    {
        try
        {
            if (!await CanAccessAsync())
                return StatusCode(403, ApiResponse<CommissionRateVersionDto>.Fail("无权维护提成系数", 403));

            var created = await _service.CreateVersionAsync(
                request.RoleType,
                request.Remark,
                request.CopyFromVersionId,
                CurrentUserId(),
                ct);
            return Ok(ApiResponse<CommissionRateVersionDto>.Ok(created, "已新建版本"));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ApiResponse<CommissionRateVersionDto>.Fail(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<CommissionRateVersionDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "新建提成系数版本失败");
            return StatusCode(500, ApiResponse<CommissionRateVersionDto>.Fail(ex.Message, 500));
        }
    }

    [HttpPut("versions/active")]
    public async Task<ActionResult<ApiResponse<CommissionRateSettingsDto>>> SetActive(
        [FromBody] SetActiveCommissionRateVersionsRequest request,
        CancellationToken ct)
    {
        try
        {
            if (!await CanAccessAsync())
                return StatusCode(403, ApiResponse<CommissionRateSettingsDto>.Fail("无权维护提成系数", 403));

            await _service.SetActiveVersionsAsync(
                request.SalesVersionId ?? string.Empty,
                request.PurchaseVersionId ?? string.Empty,
                request.ReceiptWriteoffDelayDays,
                CurrentUserId(),
                ct);
            var data = await _service.GetSettingsAsync(ct);
            return Ok(ApiResponse<CommissionRateSettingsDto>.Ok(data, "已保存"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<CommissionRateSettingsDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存提成系数生效版本失败");
            return StatusCode(500, ApiResponse<CommissionRateSettingsDto>.Fail(ex.Message, 500));
        }
    }

    [HttpPut("versions/{id}")]
    public async Task<ActionResult<ApiResponse<CommissionRateVersionDto>>> UpdateVersion(
        string id,
        [FromBody] UpdateCommissionRateVersionRequest request,
        CancellationToken ct)
    {
        try
        {
            if (!await CanAccessAsync())
                return StatusCode(403, ApiResponse<CommissionRateVersionDto>.Fail("无权维护提成系数", 403));

            var updated = await _service.UpdateVersionAsync(id, request.Remark, CurrentUserId(), ct);
            return Ok(ApiResponse<CommissionRateVersionDto>.Ok(updated, "已保存"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<CommissionRateVersionDto>.Fail(ex.Message, 404));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<CommissionRateVersionDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存提成系数版本失败");
            return StatusCode(500, ApiResponse<CommissionRateVersionDto>.Fail(ex.Message, 500));
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CommissionRateDto>>>> List(
        [FromQuery] short roleType,
        [FromQuery] string? versionId,
        CancellationToken ct)
    {
        try
        {
            if (!await CanAccessAsync())
                return StatusCode(403, ApiResponse<List<CommissionRateDto>>.Fail("无权查看提成系数", 403));

            var items = await _service.ListAsync(roleType, versionId, ct);
            return Ok(ApiResponse<List<CommissionRateDto>>.Ok(items.ToList(), "OK"));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ApiResponse<List<CommissionRateDto>>.Fail(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
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

            var updated = await _service.UpdateAsync(
                id,
                request.Ladders ?? new List<CommissionLadderWriteDto>(),
                request.Remark,
                CurrentUserId(),
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

    string? CurrentUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    async Task<bool> CanAccessAsync()
    {
        var userId = CurrentUserId();
        if (string.IsNullOrWhiteSpace(userId)) return false;
        var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
        return summary.CanForceDelete;
    }
}
