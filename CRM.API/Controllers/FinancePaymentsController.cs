using CRM.Core.Interfaces;
using CRM.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers
{
    [RequirePermission("finance-payment.read")]
    [ApiController]
    [Route("api/v1/finance/payments")]
    public class FinancePaymentsController : ControllerBase
    {
        private readonly IFinancePaymentService _service;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly ILogger<FinancePaymentsController> _logger;

        public FinancePaymentsController(IFinancePaymentService service, IDataPermissionService dataPermissionService, ILogger<FinancePaymentsController> logger)
        {
            _service = service;
            _dataPermissionService = dataPermissionService;
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
                var request = new FinancePaymentQueryRequest
                {
                    Keyword = keyword,
                    Status = status,
                    StartDate = DateTime.TryParse(startDate, out var start) ? start : null,
                    EndDate = DateTime.TryParse(endDate, out var end) ? end : null,
                    Page = page,
                    PageSize = pageSize,
                    CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                };
                var result = await _service.GetPagedAsync(request);
                return Ok(new { success = true, data = new { items = result.Items, total = result.TotalCount, page = result.PageIndex, pageSize = result.PageSize } });
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
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessFinancePaymentAsync(userId, payment))
                    return StatusCode(403, new { success = false, message = "无权限访问该付款单" });
                return Ok(new { success = true, data = payment });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>新建付款单</summary>
        [HttpPost]
        [RequirePermission("finance-payment.write")]
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
        [RequirePermission("finance-payment.write")]
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
        [RequirePermission("finance-payment.write")]
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
        [RequirePermission("finance-payment.write")]
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
        [RequirePermission("finance-payment.write")]
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
