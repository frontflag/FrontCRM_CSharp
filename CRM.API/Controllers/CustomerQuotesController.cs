using CRM.API.Authorization;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/customer-quotes")]
public class CustomerQuotesController : ControllerBase
{
    private readonly ICustomerQuoteService _service;

    public CustomerQuotesController(ICustomerQuoteService service) => _service = service;

    [HttpGet]
    [RequirePermission("customer-quote.read")]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] short? status = null,
        [FromQuery] string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var (items, total) = await _service.GetQuotesPagedAsync(userId, page, pageSize, status, keyword, cancellationToken);
        return Ok(new { success = true, data = new { items, total, page, pageSize }, errorCode = 0 });
    }

    [HttpGet("{id}")]
    [RequirePermission("customer-quote.read")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var row = await _service.GetQuoteByIdAsync(userId, id, cancellationToken);
        if (row == null)
            return NotFound(new { success = false, message = "客户报价单不存在或无权查看" });
        return Ok(new { success = true, data = row, errorCode = 0 });
    }

    [HttpPut("{id}")]
    [RequirePermission("customer-quote.write")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCustomerQuoteRequest request, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { success = false, message = "未登录" });

        try
        {
            var row = await _service.UpdateQuoteAsync(userId, id, request, cancellationToken);
            return Ok(new { success = true, data = row, errorCode = 0 });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("{id}/apply-profit-factor")]
    [RequirePermission("customer-quote.write")]
    public async Task<IActionResult> ApplyProfitFactor(string id, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { success = false, message = "未登录" });

        try
        {
            var row = await _service.ApplyProfitFactorAsync(userId, id, cancellationToken);
            return Ok(new { success = true, data = row, errorCode = 0 });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
