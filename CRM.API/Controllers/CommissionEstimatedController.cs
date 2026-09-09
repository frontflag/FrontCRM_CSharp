using System.Security.Claims;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/commission/estimated")]
public class CommissionEstimatedController : ControllerBase
{
    readonly ICommissionResultService _service;
    readonly IRbacService _rbac;
    readonly ILogger<CommissionEstimatedController> _logger;

    public CommissionEstimatedController(
        ICommissionResultService service,
        IRbacService rbac,
        ILogger<CommissionEstimatedController> logger)
    {
        _service = service;
        _rbac = rbac;
        _logger = logger;
    }

    [HttpGet("window")]
    public async Task<ActionResult<ApiResponse<CommissionOpenWindowDto>>> Window(
        [FromQuery] short roleType,
        CancellationToken ct)
    {
        var gate = await GateAsync(roleType, official: false);
        if (gate.Error != null) return gate.Error;
        return Ok(ApiResponse<CommissionOpenWindowDto>.Ok(_service.GetOpenWindow(), "OK"));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<CommissionPaged<CommissionSummaryDto>>>> Summary(
        [FromQuery] short roleType,
        [FromQuery] string? keyword,
        [FromQuery] string? userId,
        [FromQuery] DateOnly? poolDateFrom,
        [FromQuery] DateOnly? poolDateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var gate = await GateAsync(roleType, official: false);
        if (gate.Error != null) return gate.Error;
        try
        {
            var data = await _service.ListSummaryAsync(BuildQuery(roleType, keyword, userId, poolDateFrom, poolDateTo, page, pageSize, gate.ForceUserId), ct);
            return Ok(ApiResponse<CommissionPaged<CommissionSummaryDto>>.Ok(data, "OK"));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ApiResponse<CommissionPaged<CommissionSummaryDto>>.Fail(ex.Message, 400));
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<CommissionPaged<CommissionLineDto>>>> List(
        [FromQuery] short roleType,
        [FromQuery] string? keyword,
        [FromQuery] string? userId,
        [FromQuery] DateOnly? poolDateFrom,
        [FromQuery] DateOnly? poolDateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var gate = await GateAsync(roleType, official: false);
        if (gate.Error != null) return gate.Error;
        try
        {
            var data = await _service.ListLinesAsync(BuildQuery(roleType, keyword, userId, poolDateFrom, poolDateTo, page, pageSize, gate.ForceUserId), ct);
            return Ok(ApiResponse<CommissionPaged<CommissionLineDto>>.Ok(data, "OK"));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ApiResponse<CommissionPaged<CommissionLineDto>>.Fail(ex.Message, 400));
        }
    }

    [HttpGet("months")]
    public async Task<ActionResult<ApiResponse<CommissionPersonMonthsDto>>> Months(
        [FromQuery] short roleType,
        [FromQuery] string userId,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var gate = await GatePersonAsync(roleType, official: false, userId);
        if (gate != null) return gate;
        try
        {
            var data = await _service.ListMonthsAsync(
                new CommissionPersonQuery
                {
                    RoleType = roleType,
                    Official = false,
                    UserId = userId,
                    Keyword = keyword,
                    Page = page,
                    PageSize = pageSize
                },
                ct);
            return Ok(ApiResponse<CommissionPersonMonthsDto>.Ok(data, "OK"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<CommissionPersonMonthsDto>.Fail(ex.Message, 404));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ApiResponse<CommissionPersonMonthsDto>.Fail(ex.Message, 400));
        }
    }

    [HttpGet("person-lines")]
    public async Task<ActionResult<ApiResponse<CommissionPersonDetailDto>>> PersonLines(
        [FromQuery] short roleType,
        [FromQuery] string userId,
        [FromQuery] string calcMonth,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var gate = await GatePersonAsync(roleType, official: false, userId);
        if (gate != null) return gate;
        try
        {
            var data = await _service.ListPersonLinesAsync(
                new CommissionPersonQuery
                {
                    RoleType = roleType,
                    Official = false,
                    UserId = userId,
                    CalcMonth = calcMonth,
                    Keyword = keyword,
                    Page = page,
                    PageSize = pageSize
                },
                ct);
            return Ok(ApiResponse<CommissionPersonDetailDto>.Ok(data, "OK"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<CommissionPersonDetailDto>.Fail(ex.Message, 404));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ApiResponse<CommissionPersonDetailDto>.Fail(ex.Message, 400));
        }
    }

    [HttpPost("recalc")]
    public async Task<ActionResult<ApiResponse<CommissionCalcRunResult>>> Recalc(CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return StatusCode(403, ApiResponse<CommissionCalcRunResult>.Fail("无权重算预计提成", 403));
        var summary = await _rbac.GetUserPermissionSummaryAsync(userId);
        if (!summary.CanForceDelete)
            return StatusCode(403, ApiResponse<CommissionCalcRunResult>.Fail("无权重算预计提成", 403));
        try
        {
            var data = await _service.RecalcEstimatedAsync(null, ct);
            return Ok(ApiResponse<CommissionCalcRunResult>.Ok(data, "已重算"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预计提成重算失败");
            return StatusCode(500, ApiResponse<CommissionCalcRunResult>.Fail(ex.Message, 500));
        }
    }

    CommissionResultQuery BuildQuery(
        short roleType,
        string? keyword,
        string? userId,
        DateOnly? from,
        DateOnly? to,
        int page,
        int pageSize,
        string? forceUserId) =>
        new()
        {
            RoleType = roleType,
            Official = false,
            Keyword = keyword,
            UserId = forceUserId ?? userId,
            PoolDateFrom = from,
            PoolDateTo = to,
            Page = page,
            PageSize = pageSize
        };

    async Task<(ActionResult? Error, string? ForceUserId)> GateAsync(short roleType, bool official)
    {
        var userId = CurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return (StatusCode(403, ApiResponse<object>.Fail("无权查看提成", 403)), null);
        if (!CommissionLadder.IsRoleType(roleType))
            return (BadRequest(ApiResponse<object>.Fail("类型须为业务员或采购员", 400)), null);

        var summary = await _rbac.GetUserPermissionSummaryAsync(userId);
        var code = CommissionPermissionCodes.ReadCode(roleType, official);
        if (!summary.HasPermissionCode(code))
            return (StatusCode(403, ApiResponse<object>.Fail("无权查看提成", 403)), null);

        var seeAll = summary.CanForceDelete || summary.HasBizDataBypass;
        return (null, seeAll ? null : userId);
    }

    async Task<ActionResult?> GatePersonAsync(short roleType, bool official, string? userId)
    {
        var gate = await GateAsync(roleType, official);
        if (gate.Error != null) return gate.Error;
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest(ApiResponse<object>.Fail("须指定人员", 400));
        if (gate.ForceUserId != null
            && !string.Equals(gate.ForceUserId, userId, StringComparison.OrdinalIgnoreCase))
            return StatusCode(403, ApiResponse<object>.Fail("无权查看他人提成", 403));
        return null;
    }

    string? CurrentUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
