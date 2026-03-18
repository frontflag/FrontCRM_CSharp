using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/finance/payments")]
    public class FinancePaymentsController : ControllerBase
    {
        private readonly IFinancePaymentService _service;
        private readonly ILogger<FinancePaymentsController> _logger;

        public FinancePaymentsController(IFinancePaymentService service, ILogger<FinancePaymentsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>获取付款单列表</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? keyword,
            [FromQuery] short? status,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var all = await _service.GetAllAsync();
                var query = all.AsQueryable();

                if (!string.IsNullOrWhiteSpace(keyword))
                    query = query.Where(p =>
                        (p.FinancePaymentCode != null && p.FinancePaymentCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                        (p.VendorName != null && p.VendorName.Contains(keyword, StringComparison.OrdinalIgnoreCase)));

                if (status.HasValue)
                    query = query.Where(p => p.Status == status.Value);

                if (DateTime.TryParse(startDate, out var start))
                    query = query.Where(p => p.CreateTime >= start);

                if (DateTime.TryParse(endDate, out var end))
                    query = query.Where(p => p.CreateTime <= end.AddDays(1));

                var total = query.Count();
                var items = query
                    .OrderByDescending(p => p.CreateTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new { success = true, data = new { items, total, page, pageSize } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取付款单列表失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>获取单个付款单</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var payment = await _service.GetByIdAsync(id);
                if (payment == null) return NotFound(new { success = false, message = "付款单不存在" });
                return Ok(new { success = true, data = payment });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>新建付款单</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFinancePaymentRequest request)
        {
            try
            {
                var payment = await _service.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = payment.Id },
                    new { success = true, data = payment });
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
                _logger.LogError(ex, "新建付款单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>更新付款单</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateFinancePaymentRequest request)
        {
            try
            {
                var payment = await _service.UpdateAsync(id, request);
                return Ok(new { success = true, data = payment });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "付款单不存在" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新付款单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>更新付款单状态（提交审核/审核通过/驳回/作废等）</summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] FinancePaymentStatusRequest request)
        {
            try
            {
                await _service.UpdateStatusAsync(id, request.Status);
                return Ok(new { success = true, message = "状态更新成功" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "付款单不存在" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新付款单状态失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>删除付款单</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok(new { success = true, message = "删除成功" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "付款单不存在" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除付款单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>核销付款明细</summary>
        [HttpPost("items/{paymentItemId}/verify")]
        public async Task<IActionResult> VerifyItem(string paymentItemId, [FromBody] VerifyPaymentItemRequest request)
        {
            try
            {
                await _service.VerifyPaymentItemAsync(paymentItemId, request.Amount);
                return Ok(new { success = true, message = "核销成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "核销付款明细失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    public class FinancePaymentStatusRequest
    {
        public short Status { get; set; }
    }

    public class VerifyPaymentItemRequest
    {
        public decimal Amount { get; set; }
    }
}
