using System.Security.Claims;
using CRM.API.Authorization;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[RequirePermission("finance-sell-invoice.read")]
[ApiController]
[Route("api/v1/finance/sell-invoice-write-offs")]
public class FinanceSellInvoiceWriteOffsController : ControllerBase
{
    private readonly IFinanceSellInvoiceWriteOffService _service;
    private readonly ILogger<FinanceSellInvoiceWriteOffsController> _logger;

    public FinanceSellInvoiceWriteOffsController(
        IFinanceSellInvoiceWriteOffService service,
        ILogger<FinanceSellInvoiceWriteOffsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("customer-summaries")]
    public async Task<IActionResult> GetCustomerSummaries([FromQuery] string? keyword, CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var data = await _service.GetCustomerSummariesAsync(keyword, userId, cancellationToken);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取销项发票核销客户汇总失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("candidates")]
    public async Task<IActionResult> GetCandidates(
        [FromQuery] string customerId,
        [FromQuery] byte currency = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(customerId))
                return BadRequest(new { success = false, message = "请指定客户" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var data = await _service.GetCandidatesAsync(customerId, currency, userId, cancellationToken);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取销项发票核销候选失败 CustomerId={CustomerId}", customerId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [RequirePermission("finance-sell-invoice.write")]
    [HttpPost]
    public async Task<IActionResult> Apply(
        [FromBody] FinanceSellInvoiceWriteOffRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _service.ApplyAsync(request, userId, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "销项发票核销提交失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
