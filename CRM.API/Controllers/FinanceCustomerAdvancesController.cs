using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[RequirePermission("finance-receipt.read")]
[ApiController]
[Route("api/v1/finance/customer-advances")]
public class FinanceCustomerAdvancesController : ControllerBase
{
    private readonly IFinanceCustomerAdvanceService _service;
    private readonly ILogger<FinanceCustomerAdvancesController> _logger;

    public FinanceCustomerAdvancesController(
        IFinanceCustomerAdvanceService service,
        ILogger<FinanceCustomerAdvancesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] string? keyword,
        [FromQuery] string? customerId,
        [FromQuery] short? currency,
        [FromQuery] bool? onlyPositiveBalance,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _service.GetPagedAsync(new FinanceCustomerAdvanceQueryRequest
            {
                Keyword = keyword,
                CustomerId = customerId,
                Currency = currency,
                OnlyPositiveBalance = onlyPositiveBalance ?? true,
                Page = page,
                PageSize = pageSize,
                CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            });
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
            _logger.LogError(ex, "获取预收余额列表失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("ledger")]
    public async Task<IActionResult> GetLedger(
        [FromQuery] string? customerId,
        [FromQuery] short? currency,
        [FromQuery] short? ledgerType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _service.GetLedgerPagedAsync(new FinanceCustomerAdvanceLedgerQueryRequest
            {
                CustomerId = customerId,
                Currency = currency,
                LedgerType = ledgerType,
                Page = page,
                PageSize = pageSize,
                CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            });
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
            _logger.LogError(ex, "获取预收流水失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance([FromQuery] string customerId, [FromQuery] short currency = 1)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(customerId))
                return BadRequest(new { success = false, message = "请指定客户" });

            var balance = await _service.GetBalanceAsync(customerId.Trim(), currency);
            var all = await _service.GetBalancesForCustomerAsync(customerId.Trim());
            return Ok(new { success = true, data = new { balance, balances = all } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取预收余额失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
