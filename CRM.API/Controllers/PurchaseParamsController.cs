using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/purchase-params")]
public class PurchaseParamsController : ControllerBase
{
    private readonly IPurchaseQuoterPoolService _service;
    private readonly ILogger<PurchaseParamsController> _logger;

    public PurchaseParamsController(IPurchaseQuoterPoolService service, ILogger<PurchaseParamsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("assignee-count")]
    [RequirePermission("system.params.purchase.assignee-count.read")]
    public async Task<ActionResult<ApiResponse<PurchaseParamsAssigneeCountDto>>> GetAssigneeCount(CancellationToken ct)
    {
        try
        {
            var count = await _service.GetAssigneeCountAsync(ct);
            return Ok(ApiResponse<PurchaseParamsAssigneeCountDto>.Ok(
                new PurchaseParamsAssigneeCountDto { Count = count },
                "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "????????");
            return StatusCode(500, ApiResponse<PurchaseParamsAssigneeCountDto>.Fail("????", 500));
        }
    }

    [HttpPut("assignee-count")]
    [RequirePermission("system.params.purchase.assignee-count.write")]
    public async Task<ActionResult<ApiResponse<PurchaseParamsAssigneeCountDto>>> SetAssigneeCount(
        [FromBody] SetPurchaseParamsAssigneeCountRequest? body,
        CancellationToken ct)
    {
        if (body == null)
            return BadRequest(ApiResponse<PurchaseParamsAssigneeCountDto>.Fail("?????", 400));
        try
        {
            await _service.SetAssigneeCountAsync(body.Count, ct);
            return Ok(ApiResponse<PurchaseParamsAssigneeCountDto>.Ok(
                new PurchaseParamsAssigneeCountDto { Count = body.Count },
                "???"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<PurchaseParamsAssigneeCountDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "????????");
            return StatusCode(500, ApiResponse<PurchaseParamsAssigneeCountDto>.Fail("????", 500));
        }
    }

    [HttpGet("demand-protection-minutes")]
    [RequirePermission("system.params.purchase.demand-protection.read")]
    public async Task<ActionResult<ApiResponse<PurchaseParamsDemandProtectionMinutesDto>>> GetDemandProtectionMinutes(CancellationToken ct)
    {
        try
        {
            var minutes = await _service.GetDemandProtectionMinutesAsync(ct);
            return Ok(ApiResponse<PurchaseParamsDemandProtectionMinutesDto>.Ok(
                new PurchaseParamsDemandProtectionMinutesDto { Minutes = minutes },
                "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "??????????");
            return StatusCode(500, ApiResponse<PurchaseParamsDemandProtectionMinutesDto>.Fail("????", 500));
        }
    }

    [HttpPut("demand-protection-minutes")]
    [RequirePermission("system.params.purchase.demand-protection.write")]
    public async Task<ActionResult<ApiResponse<PurchaseParamsDemandProtectionMinutesDto>>> SetDemandProtectionMinutes(
        [FromBody] SetPurchaseParamsDemandProtectionMinutesRequest? body,
        CancellationToken ct)
    {
        if (body == null)
            return BadRequest(ApiResponse<PurchaseParamsDemandProtectionMinutesDto>.Fail("?????", 400));
        try
        {
            await _service.SetDemandProtectionMinutesAsync(body.Minutes, ct);
            return Ok(ApiResponse<PurchaseParamsDemandProtectionMinutesDto>.Ok(
                new PurchaseParamsDemandProtectionMinutesDto { Minutes = body.Minutes },
                "???"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<PurchaseParamsDemandProtectionMinutesDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "??????????");
            return StatusCode(500, ApiResponse<PurchaseParamsDemandProtectionMinutesDto>.Fail("????", 500));
        }
    }

    [HttpGet("default-assign-method")]
    [RequirePermission("system.params.purchase.default-assign-method.read")]
    public async Task<ActionResult<ApiResponse<PurchaseParamsDefaultAssignMethodDto>>> GetDefaultAssignMethod(CancellationToken ct)
    {
        try
        {
            var assignMethod = await _service.GetDefaultAssignMethodAsync(ct);
            return Ok(ApiResponse<PurchaseParamsDefaultAssignMethodDto>.Ok(
                new PurchaseParamsDefaultAssignMethodDto { AssignMethod = assignMethod },
                "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "??????????");
            return StatusCode(500, ApiResponse<PurchaseParamsDefaultAssignMethodDto>.Fail("????", 500));
        }
    }

    [HttpPut("default-assign-method")]
    [RequirePermission("system.params.purchase.default-assign-method.write")]
    public async Task<ActionResult<ApiResponse<PurchaseParamsDefaultAssignMethodDto>>> SetDefaultAssignMethod(
        [FromBody] SetPurchaseParamsDefaultAssignMethodRequest? body,
        CancellationToken ct)
    {
        if (body == null)
            return BadRequest(ApiResponse<PurchaseParamsDefaultAssignMethodDto>.Fail("?????", 400));
        try
        {
            await _service.SetDefaultAssignMethodAsync(body.AssignMethod, ct);
            var assignMethod = await _service.GetDefaultAssignMethodAsync(ct);
            return Ok(ApiResponse<PurchaseParamsDefaultAssignMethodDto>.Ok(
                new PurchaseParamsDefaultAssignMethodDto { AssignMethod = assignMethod },
                "???"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<PurchaseParamsDefaultAssignMethodDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "??????????");
            return StatusCode(500, ApiResponse<PurchaseParamsDefaultAssignMethodDto>.Fail("????", 500));
        }
    }

    [HttpGet("allow-refresh-completed-biz-nodes")]
    [RequirePermission("system.params.purchase.refresh-vendor.read")]
    public async Task<ActionResult<ApiResponse<PurchaseParamsAllowRefreshCompletedBizNodesDto>>> GetAllowRefreshCompletedBizNodes(
        CancellationToken ct)
    {
        try
        {
            var allow = await _service.GetAllowRefreshCompletedBizNodesAsync(ct);
            return Ok(ApiResponse<PurchaseParamsAllowRefreshCompletedBizNodesDto>.Ok(
                new PurchaseParamsAllowRefreshCompletedBizNodesDto { Allow = allow },
                "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "?????????????????");
            return StatusCode(500, ApiResponse<PurchaseParamsAllowRefreshCompletedBizNodesDto>.Fail("????", 500));
        }
    }

    [HttpPut("allow-refresh-completed-biz-nodes")]
    [RequirePermission("system.params.purchase.refresh-vendor.write")]
    public async Task<ActionResult<ApiResponse<PurchaseParamsAllowRefreshCompletedBizNodesDto>>> SetAllowRefreshCompletedBizNodes(
        [FromBody] SetPurchaseParamsAllowRefreshCompletedBizNodesRequest? body,
        CancellationToken ct)
    {
        if (body == null)
            return BadRequest(ApiResponse<PurchaseParamsAllowRefreshCompletedBizNodesDto>.Fail("?????", 400));

        try
        {
            await _service.SetAllowRefreshCompletedBizNodesAsync(body.Allow, ct);
            return Ok(ApiResponse<PurchaseParamsAllowRefreshCompletedBizNodesDto>.Ok(
                new PurchaseParamsAllowRefreshCompletedBizNodesDto { Allow = body.Allow },
                "???"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "?????????????????");
            return StatusCode(500, ApiResponse<PurchaseParamsAllowRefreshCompletedBizNodesDto>.Fail("????", 500));
        }
    }

    [HttpGet("quoter-pool")]
    [RequirePermission("system.params.purchase.quoter-pool.read")]
    public async Task<ActionResult<ApiResponse<PurchaseQuoterPoolListResponse>>> GetQuoterPool(
        [FromQuery] string? filter,
        CancellationToken ct)
    {
        try
        {
            var result = await _service.ListMembersAsync(filter, ct);
            return Ok(ApiResponse<PurchaseQuoterPoolListResponse>.Ok(MapPool(result), "ok"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "????????");
            return StatusCode(500, ApiResponse<PurchaseQuoterPoolListResponse>.Fail("????", 500));
        }
    }

    [HttpPut("quoter-pool")]
    [RequirePermission("system.params.purchase.quoter-pool.write")]
    public async Task<ActionResult<ApiResponse<PurchaseQuoterPoolListResponse>>> SaveQuoterPool(
        [FromBody] SavePurchaseQuoterPoolRequest? body,
        CancellationToken ct)
    {
        if (body == null)
            return BadRequest(ApiResponse<PurchaseQuoterPoolListResponse>.Fail("?????", 400));
        try
        {
            var result = await _service.SavePoolAsync(body.UserIds ?? [], ct);
            return Ok(ApiResponse<PurchaseQuoterPoolListResponse>.Ok(MapPool(result), "???"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<PurchaseQuoterPoolListResponse>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "????????");
            return StatusCode(500, ApiResponse<PurchaseQuoterPoolListResponse>.Fail("????", 500));
        }
    }

    private static PurchaseQuoterPoolListResponse MapPool(PurchaseQuoterPoolListResult result) =>
        new()
        {
            SelectedCount = result.SelectedCount,
            Items = result.Items.Select(m => new PurchaseQuoterPoolMemberResponse
            {
                UserId = m.UserId,
                UserName = m.UserName,
                RealName = m.RealName,
                DepartmentName = m.DepartmentName,
                IsActive = m.IsActive,
                IsSelected = m.IsSelected
            }).ToList()
        };
}
