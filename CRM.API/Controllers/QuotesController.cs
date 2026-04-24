using CRM.API.Utilities;
using CRM.Core.Interfaces;
using CRM.Core.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;

namespace CRM.API.Controllers
{
    /// <summary>报价单 API — /api/v1/quotes</summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class QuotesController : ControllerBase
    {
        private readonly IQuoteService _quoteService;
        private readonly IRbacService _rbacService;

        public QuotesController(IQuoteService quoteService, IRbacService rbacService)
        {
            _quoteService = quoteService;
            _rbacService = rbacService;
        }

        // GET /api/v1/quotes
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var quotes = (await _quoteService.GetAllAsync()).ToList();
                if (await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User))
                    PurchaseSensitiveFieldMask511.ApplyQuotesVendorIdentityOnly(quotes, true);
                var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(uid))
                {
                    var s = await _rbacService.GetUserPermissionSummaryAsync(uid);
                    if (SaleSensitiveFieldMask521.ShouldMask(s))
                        SaleSensitiveFieldMask521.ApplyQuotes(quotes, true);
                }

                return Ok(new { success = true, data = quotes, errorCode = 0 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message, errorCode = 500 });
            }
        }

        // GET /api/v1/quotes/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var quote = await _quoteService.GetByIdAsync(id);
                if (quote == null)
                    return NotFound(new { success = false, message = $"报价单 {id} 不存在", errorCode = 404 });
                if (await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User))
                    PurchaseSensitiveFieldMask511.ApplyQuoteVendorIdentityOnly(quote, true);
                var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(uid))
                {
                    var s = await _rbacService.GetUserPermissionSummaryAsync(uid);
                    if (SaleSensitiveFieldMask521.ShouldMask(s))
                        SaleSensitiveFieldMask521.ApplyQuote(quote, true);
                }

                return Ok(new { success = true, data = quote, errorCode = 0 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message, errorCode = 500 });
            }
        }

        // POST /api/v1/quotes
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateQuoteRequest request)
        {
            try
            {
                var quote = await _quoteService.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = quote.Id },
                    new { success = true, message = "报价单创建成功", data = quote, errorCode = 0 });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message, errorCode = 400 });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message, errorCode = 409 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message, errorCode = 500 });
            }
        }

        // PUT /api/v1/quotes/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateQuoteRequest request)
        {
            try
            {
                var quote = await _quoteService.UpdateAsync(id, request);
                return Ok(new { success = true, message = "报价单更新成功", data = quote, errorCode = 0 });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message, errorCode = 400 });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message, errorCode = 404 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message, errorCode = 500 });
            }
        }

        // DELETE /api/v1/quotes/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _quoteService.DeleteAsync(id);
                return Ok(new { success = true, message = "报价单删除成功", errorCode = 0 });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message, errorCode = 400 });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message, errorCode = 404 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message, errorCode = 500 });
            }
        }

        // PATCH /api/v1/quotes/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] QuoteUpdateStatusRequest request)
        {
            try
            {
                await _quoteService.UpdateStatusAsync(id, request.Status);
                return Ok(new { success = true, message = "状态更新成功", errorCode = 0 });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message, errorCode = 404 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message, errorCode = 500 });
            }
        }
    }

    public class QuoteUpdateStatusRequest
    {
        public short Status { get; set; }
    }
}
