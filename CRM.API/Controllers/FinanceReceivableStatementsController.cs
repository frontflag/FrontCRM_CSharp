using System.Security.Claims;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[RequirePermission("finance-receipt.read")]
[ApiController]
[Route("api/v1/finance/receivable-statements")]
public class FinanceReceivableStatementsController : ControllerBase
{
    private readonly IFinanceReceivableStatementQuery _query;
    private readonly ILogger<FinanceReceivableStatementsController> _logger;

    public FinanceReceivableStatementsController(
        IFinanceReceivableStatementQuery query,
        ILogger<FinanceReceivableStatementsController> logger)
    {
        _query = query;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] string? keyword,
        [FromQuery] bool? onlyOpen,
        [FromQuery] short? currency,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _query.GetPagedAsync(
                new FinanceReceivableStatementListQueryRequest
                {
                    Keyword = keyword,
                    OnlyOpen = onlyOpen == true,
                    Currency = currency,
                    Page = page,
                    PageSize = pageSize,
                    CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                },
                cancellationToken);
            return Ok(new
            {
                success = true,
                data = new
                {
                    items = result.Items,
                    total = result.TotalCount,
                    page = result.PageIndex,
                    pageSize = result.PageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取客户对账单列表失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{customerId}/{currency:int}")]
    public async Task<IActionResult> GetDetail(
        string customerId,
        int currency,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? aging,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!DateOnly.TryParse(from, out var periodFrom) || !DateOnly.TryParse(to, out var periodTo))
                return BadRequest(ApiResponse<object>.Fail("请指定对账期间起止日期"));

            if (periodFrom > periodTo)
                return BadRequest(ApiResponse<object>.Fail("对账期间起日不能晚于止日"));

            if (currency is < 1 or > 6)
                return BadRequest(ApiResponse<object>.Fail("币别无效"));

            var agingCutoff = DateOnly.TryParse(aging, out var agingDate)
                ? agingDate
                : DateOnly.FromDateTime(DateTime.UtcNow);

            var data = await _query.GetDetailAsync(
                customerId,
                (short)currency,
                periodFrom,
                periodTo,
                agingCutoff,
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                cancellationToken);
            if (data == null)
                return NotFound(ApiResponse<object>.Fail("客户对账单不存在或无权查看"));

            return Ok(ApiResponse<FinanceReceivableStatementDetailDto>.Ok(data));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取客户对账单详情失败 CustomerId={CustomerId} Currency={Currency}", customerId, currency);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
