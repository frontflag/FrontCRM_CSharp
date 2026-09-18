using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/commission/pool")]
public class CommissionPoolController : ControllerBase
{
    readonly ICommissionPoolListService _service;
    readonly IRbacService _rbac;

    public CommissionPoolController(ICommissionPoolListService service, IRbacService rbac)
    {
        _service = service;
        _rbac = rbac;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<CommissionPaged<CommissionPoolListRowDto>>>> List(
        [FromQuery] string? keyword,
        [FromQuery] string? purchasePn,
        [FromQuery] string? salesUserId,
        [FromQuery] string? purchaseUserId,
        [FromQuery] short? salesStatus,
        [FromQuery] short? purchaseStatus,
        [FromQuery] short? receiptStatus,
        [FromQuery] DateOnly? stockOutDateFrom,
        [FromQuery] DateOnly? stockOutDateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return StatusCode(403, ApiResponse<CommissionPaged<CommissionPoolListRowDto>>.Fail("无权查看提成池", 403));

        var summary = await _rbac.GetUserPermissionSummaryAsync(userId);
        if (!CommissionPoolAccessRules.CanEnter(summary))
            return StatusCode(403, ApiResponse<CommissionPaged<CommissionPoolListRowDto>>.Fail("无权查看提成池", 403));

        var scope = CommissionPoolAccessRules.ResolveRowScope(summary);
        var seeAll = CommissionPoolAccessRules.SeesAll(summary);

        try
        {
            var data = await _service.ListAsync(
                new CommissionPoolListQuery
                {
                    Keyword = keyword,
                    PurchasePn = purchasePn,
                    SalesUserId = seeAll ? salesUserId : null,
                    PurchaseUserId = seeAll ? purchaseUserId : null,
                    RestrictSalesUserId = scope.RestrictSalesUserId,
                    RestrictPurchaseUserId = scope.RestrictPurchaseUserId,
                    RestrictEither = scope.RestrictEither,
                    SalesCommissionStatus = salesStatus,
                    PurchaseCommissionStatus = purchaseStatus,
                    ReceiptProgressStatus = receiptStatus,
                    StockOutDateFrom = stockOutDateFrom,
                    StockOutDateTo = stockOutDateTo,
                    Page = page,
                    PageSize = pageSize
                },
                ct);
            return Ok(ApiResponse<CommissionPaged<CommissionPoolListRowDto>>.Ok(data, "OK"));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ApiResponse<CommissionPaged<CommissionPoolListRowDto>>.Fail(ex.Message, 400));
        }
    }
}
