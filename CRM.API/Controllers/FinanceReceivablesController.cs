using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[RequirePermission("finance-receipt.read")]
[ApiController]
[Route("api/v1/finance/receivables")]
public class FinanceReceivablesController : ControllerBase
{
    private readonly IFinanceReceivableService _service;
    private readonly ILogger<FinanceReceivablesController> _logger;

    public FinanceReceivablesController(
        IFinanceReceivableService service,
        ILogger<FinanceReceivablesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] string? keyword,
        [FromQuery] string? customerId,
        [FromQuery] short? verificationStatus,
        [FromQuery] bool? onlyOpen,
        [FromQuery] string? stockOutDateFrom,
        [FromQuery] string? stockOutDateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var request = new FinanceReceivableQueryRequest
            {
                Keyword = keyword,
                CustomerId = customerId,
                VerificationStatus = verificationStatus,
                OnlyOpen = onlyOpen ?? true,
                StockOutDateFrom = DateTime.TryParse(stockOutDateFrom, out var from) ? from : null,
                StockOutDateTo = DateTime.TryParse(stockOutDateTo, out var to) ? to : null,
                Page = page,
                PageSize = pageSize,
                CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            };
            var result = await _service.GetPagedAsync(request);
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
            _logger.LogError(ex, "获取应收款列表失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}

[RequirePermission("finance-receipt.read")]
[ApiController]
[Route("api/v1/finance/receivable-write-offs")]
public class FinanceReceivableWriteOffsController : ControllerBase
{
    private readonly IFinanceReceivableService _service;
    private readonly ILogger<FinanceReceivableWriteOffsController> _logger;

    public FinanceReceivableWriteOffsController(
        IFinanceReceivableService service,
        ILogger<FinanceReceivableWriteOffsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("candidates")]
    public async Task<IActionResult> GetCandidates([FromQuery] string customerId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(customerId))
                return BadRequest(new { success = false, message = "请指定客户" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var data = await _service.GetWriteOffCandidatesAsync(customerId, userId);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取核销候选数据失败 CustomerId={CustomerId}", customerId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [RequirePermission("finance-receipt.write")]
    [HttpPost]
    public async Task<IActionResult> Apply([FromBody] FinanceReceivableWriteOffRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _service.ApplyWriteOffAsync(request, userId);
            if (result.RequiresSoMismatchConfirm)
            {
                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "预收关联销售订单与应收不一致，请确认后继续"
                });
            }

            return Ok(new { success = true, data = result, message = "核销成功" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "收款核销失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
