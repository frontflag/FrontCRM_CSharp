using System.Security.Claims;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.API.Utilities;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/incentive-targets")]
[Authorize]
public class IncentiveTargetsController : ControllerBase
{
    readonly IIncentiveTargetService _service;
    readonly IRbacService _rbac;

    public IncentiveTargetsController(IIncentiveTargetService service, IRbacService rbac)
    {
        _service = service;
        _rbac = rbac;
    }

    string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    bool IsImpersonating =>
        !string.IsNullOrWhiteSpace(User.FindFirst(ImpersonationClaimTypes.Impersonator)?.Value);

    [HttpGet("me")]
    [RequirePermission(IncentiveTargetPermissionCodes.Read)]
    public async Task<ActionResult<ApiResponse<IncentiveTargetMineDto>>> GetMine(CancellationToken cancellationToken)
    {
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<IncentiveTargetMineDto>.Fail("未登录", 401));
        var dto = await _service.GetMineAsync(uid, cancellationToken);
        await ApplyMaskAsync(dto);
        return Ok(ApiResponse<IncentiveTargetMineDto>.Ok(dto));
    }

    [HttpPut("me")]
    [RequirePermission(IncentiveTargetPermissionCodes.Write)]
    public async Task<ActionResult<ApiResponse<IncentiveTargetMineDto>>> PutMine(
        [FromBody] IncentiveTargetPutRequest request,
        CancellationToken cancellationToken)
    {
        if (IsImpersonating)
            return StatusCode(403, ApiResponse<IncentiveTargetMineDto>.Fail("模拟登录不能改激励目标", 403));
        var uid = CurrentUserId;
        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(ApiResponse<IncentiveTargetMineDto>.Fail("未登录", 401));
        try
        {
            var dto = await _service.PutMineAsync(uid, request, cancellationToken);
            await ApplyMaskAsync(dto);
            return Ok(ApiResponse<IncentiveTargetMineDto>.Ok(dto));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<IncentiveTargetMineDto>.Fail(ex.Message, 400));
        }
    }

    async Task ApplyMaskAsync(IncentiveTargetMineDto dto)
    {
        var mask = dto.RoleType == (short)CommissionRoleType.Purchase
            ? await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbac, User)
            : await SaleMaskHttp.ShouldMaskSale521Async(_rbac, User);
        if (!mask)
            return;
        dto.Term.ActualUsd = null;
        dto.Term.CompletionPct = null;
        dto.Year.ActualUsd = null;
        dto.Year.CompletionPct = null;
    }
}
