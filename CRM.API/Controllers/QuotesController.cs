using CRM.API.Utilities;
using CRM.Core.Interfaces;
using CRM.Core.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
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
        private readonly IQuoteListQuery _quoteListQuery;
        private readonly IRbacService _rbacService;

        public QuotesController(IQuoteService quoteService, IQuoteListQuery quoteListQuery, IRbacService rbacService)
        {
            _quoteService = quoteService;
            _quoteListQuery = quoteListQuery;
            _rbacService = rbacService;
        }

        // GET /api/v1/quotes
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] short? status = null,
            [FromQuery] string? rfqItemId = null,
            [FromQuery] string? aggregateCreateFrom = null,
            [FromQuery] string? aggregateCreateToExclusive = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                DateTime? aggFrom = null;
                DateTime? aggToEx = null;
                if (TryParseAggregateInstant(aggregateCreateFrom, out var tFrom))
                    aggFrom = tFrom;
                if (TryParseAggregateInstant(aggregateCreateToExclusive, out var tToEx))
                    aggToEx = tToEx;

                var request = new QuoteQueryRequest
                {
                    Page = page,
                    PageSize = pageSize,
                    Keyword = keyword,
                    Status = status,
                    RfqItemId = rfqItemId,
                    AggregateCreateFromUtc = aggFrom,
                    AggregateCreateToExclusiveUtc = aggToEx
                };

                var result = await _quoteService.GetPagedAsync(request);
                var quotes = result.Items.ToList();
                if (await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User))
                    PurchaseSensitiveFieldMask511.ApplyQuotesVendorIdentityOnly(quotes, true);
                var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(uid))
                {
                    var s = await _rbacService.GetUserPermissionSummaryAsync(uid);
                    if (SaleSensitiveFieldMask521.ShouldMask(s))
                        SaleSensitiveFieldMask521.ApplyQuotes(quotes, true);
                }

                var aggregates = await _quoteListQuery.GetAggregatesAsync(request, cancellationToken);
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        items = quotes,
                        total = result.TotalCount,
                        page = result.PageIndex,
                        pageSize = result.PageSize,
                        aggregates = new
                        {
                            aggregates.TotalCount,
                            aggregates.PendingCount,
                            aggregates.SentCount,
                            aggregates.AcceptedCount,
                            aggregates.CreatedInRangeCount
                        }
                    },
                    errorCode = 0
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message, errorCode = 500 });
            }
        }

        private static bool TryParseAggregateInstant(string? s, out DateTime utc)
        {
            utc = default;
            if (string.IsNullOrWhiteSpace(s))
                return false;
            if (!DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                return false;
            utc = dt.Kind switch
            {
                DateTimeKind.Utc => dt,
                DateTimeKind.Local => dt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
            };
            return true;
        }

        /// <summary>按需求明细行 ID 批量统计关联报价条数（逗号分隔，最多 500 个）。</summary>
        [HttpGet("aggregate/quote-counts-by-rfq-item-ids")]
        public async Task<IActionResult> GetQuoteCountsByRfqItemIds(
            [FromQuery] string? rfqItemIds,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idList = string.IsNullOrWhiteSpace(rfqItemIds)
                    ? Array.Empty<string>()
                    : rfqItemIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var map = await _quoteListQuery.GetQuoteCountsByRfqItemIdsAsync(idList, cancellationToken);
                var counts = idList
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(500)
                    .ToDictionary(
                        id => id,
                        id => map.TryGetValue(id, out var c) ? c : 0,
                        StringComparer.OrdinalIgnoreCase);
                return Ok(new { success = true, data = new { counts }, errorCode = 0 });
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
