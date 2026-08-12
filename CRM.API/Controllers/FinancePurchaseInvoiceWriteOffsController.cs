using System.Security.Claims;
using CRM.API.Authorization;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[RequirePermission("finance-purchase-invoice.read")]
[ApiController]
[Route("api/v1/finance/purchase-invoice-write-offs")]
public class FinancePurchaseInvoiceWriteOffsController : ControllerBase
{
    private readonly IFinancePurchaseInvoiceWriteOffService _service;
    private readonly ILogger<FinancePurchaseInvoiceWriteOffsController> _logger;

    public FinancePurchaseInvoiceWriteOffsController(
        IFinancePurchaseInvoiceWriteOffService service,
        ILogger<FinancePurchaseInvoiceWriteOffsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("vendor-summaries")]
    public async Task<IActionResult> GetVendorSummaries([FromQuery] string? keyword, CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var data = await _service.GetVendorSummariesAsync(keyword, userId, cancellationToken);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取进项发票核销供应商汇总失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("candidates")]
    public async Task<IActionResult> GetCandidates(
        [FromQuery] string vendorId,
        [FromQuery] byte currency = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(vendorId))
                return BadRequest(new { success = false, message = "请指定供应商" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var data = await _service.GetCandidatesAsync(vendorId, currency, userId, cancellationToken);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取进项发票核销候选失败 VendorId={VendorId}", vendorId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [RequirePermission("finance-purchase-invoice.write")]
    [HttpPost]
    public async Task<IActionResult> Apply(
        [FromBody] FinancePurchaseInvoiceWriteOffRequest request,
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
            _logger.LogError(ex, "进项发票核销提交失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
