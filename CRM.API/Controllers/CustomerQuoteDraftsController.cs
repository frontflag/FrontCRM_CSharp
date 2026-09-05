using CRM.API.Authorization;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/customer-quote-drafts")]
public class CustomerQuoteDraftsController : ControllerBase
{
    private readonly ICustomerQuoteService _service;

    public CustomerQuoteDraftsController(ICustomerQuoteService service) => _service = service;

    [HttpGet]
    [RequirePermission("customer-quote.read")]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { success = false, message = "未登录" });

        var (items, total) = await _service.GetDraftsPagedAsync(userId, page, pageSize, cancellationToken);
        return Ok(new { success = true, data = new { items, total, page, pageSize }, errorCode = 0 });
    }

    [HttpPost]
    [RequirePermission("customer-quote.write")]
    public async Task<IActionResult> Add([FromBody] AddCustomerQuoteDraftRequest request, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { success = false, message = "未登录" });

        try
        {
            if (!string.IsNullOrWhiteSpace(request.QuoteItemId))
            {
                var row = await _service.AddDraftFromQuoteItemAsync(userId, request.QuoteItemId, cancellationToken);
                return Ok(new { success = true, data = row, errorCode = 0 });
            }

            if (!string.IsNullOrWhiteSpace(request.QuoteId))
            {
                var rows = await _service.AddDraftsFromQuoteAsync(userId, request.QuoteId, cancellationToken);
                return Ok(new { success = true, data = rows, errorCode = 0 });
            }

            return BadRequest(new { success = false, message = "请指定 quoteItemId 或 quoteId" });
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

    [HttpDelete("{id}")]
    [RequirePermission("customer-quote.write")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { success = false, message = "未登录" });

        try
        {
            await _service.DeleteDraftAsync(userId, id, cancellationToken);
            return Ok(new { success = true, errorCode = 0 });
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

    [HttpPost("generate-quote")]
    [RequirePermission("customer-quote.write")]
    public async Task<IActionResult> GenerateQuote([FromBody] GenerateCustomerQuoteRequest request, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { success = false, message = "未登录" });

        try
        {
            var quote = await _service.GenerateFromDraftsAsync(userId, request.DraftIds ?? new List<string>(), cancellationToken);
            return Ok(new { success = true, data = quote, errorCode = 0 });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}

public class AddCustomerQuoteDraftRequest
{
    public string? QuoteItemId { get; set; }
    public string? QuoteId { get; set; }
}

public class GenerateCustomerQuoteRequest
{
    public List<string>? DraftIds { get; set; }
}
