using CRM.API.Authorization;
using CRM.API.Utilities;
using CRM.Core.Interfaces;
using CRM.Core.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/finance/payments")]
    public class FinancePaymentsController : ControllerBase
    {
        private readonly IFinancePaymentService _service;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IRbacService _rbacService;
        private readonly ILogger<FinancePaymentsController> _logger;

        public FinancePaymentsController(
            IFinancePaymentService service,
            IDataPermissionService dataPermissionService,
            IRbacService rbacService,
            ILogger<FinancePaymentsController> logger)
        {
            _service = service;
            _dataPermissionService = dataPermissionService;
            _rbacService = rbacService;
            _logger = logger;
        }

        /// <summary>获取付款单列表</summary>
        [HttpGet]
        [RequireAnyPermission("finance-payment.read", "purchase-order.read")]
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
                var items = result.Items.ToList();
                if (await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User))
                    PurchaseSensitiveFieldMask511.ApplyFinancePayments(items, true);
                return Ok(new { success = true, data = new { items, total = result.TotalCount, page = result.PageIndex, pageSize = result.PageSize } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取付款单列表失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>获取单个付款单</summary>
        [HttpGet("{id}")]
        [RequireAnyPermission("finance-payment.read", "purchase-order.read")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var payment = await _service.GetByIdAsync(id);
                if (payment == null) return NotFound(new { success = false, message = "付款单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessFinancePaymentAsync(userId, payment))
                    return StatusCode(403, new { success = false, message = "无权限访问该付款单" });
                if (await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User))
                    PurchaseSensitiveFieldMask511.ApplyFinancePayment(payment, true);
                return Ok(new { success = true, data = payment });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>新建付款单（采购端可凭采购订单写权限从明细发起）</summary>
        [HttpPost]
        [RequireAnyPermission("finance-payment.write", "purchase-order.write")]
        public async Task<IActionResult> Create([FromBody] CreateFinancePaymentRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var payment = await _service.CreateAsync(request, userId);
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
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                _logger.LogError(ex, "新建付款单保存失败");
                return BadRequest(new { success = false, message = innerMessage });
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
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var payment = await _service.UpdateAsync(id, request, userId);
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

        /// <summary>更新付款单状态（提交审核/审核通过/驳回/作废等）；采购从明细提交草稿→待审用 Patch</summary>
        [HttpPatch("{id}/status")]
        [RequireAnyPermission("finance-payment.write", "purchase-order.write")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] FinancePaymentStatusRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.UpdateStatusAsync(id, request.Status, remark: null, actingUserId: userId);
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

        /// <summary>提交审核（1 -> 2）</summary>
        [HttpPost("{id}/submit")]
        [RequirePermission("finance-payment.write")]
        public async Task<IActionResult> Submit(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.UpdateStatusAsync(id, 2, remark: null, actingUserId: userId);
                return Ok(new { success = true, message = "提交审核成功" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提交付款单审核失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>审核通过（2 -> 10）</summary>
        [HttpPost("{id}/approve")]
        [RequirePermission("finance-payment.write")]
        public async Task<IActionResult> Approve(string id, [FromBody] FinancePaymentDecisionRequest? request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.UpdateStatusAsync(id, 10, request?.Remark, userId);
                return Ok(new { success = true, message = "审核通过" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "审核付款单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>审核驳回（2 -> -1）</summary>
        [HttpPost("{id}/reject")]
        [RequirePermission("finance-payment.write")]
        public async Task<IActionResult> Reject(string id, [FromBody] FinancePaymentDecisionRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Remark))
                    return BadRequest(new { success = false, message = "驳回原因不能为空" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.UpdateStatusAsync(id, -1, request.Remark, userId);
                return Ok(new { success = true, message = "已驳回" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "驳回付款单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>确认付款完成（10 -> 100）</summary>
        [HttpPost("{id}/complete")]
        [RequirePermission("finance-payment.write")]
        public async Task<IActionResult> Complete(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.UpdateStatusAsync(id, 100, remark: null, actingUserId: userId);
                return Ok(new { success = true, message = "付款完成" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "确认付款失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>取消付款单（1/2 -> -2）</summary>
        [HttpPost("{id}/cancel")]
        [RequirePermission("finance-payment.write")]
        public async Task<IActionResult> Cancel(string id, [FromBody] FinancePaymentDecisionRequest? request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.UpdateStatusAsync(id, -2, request?.Remark, userId);
                return Ok(new { success = true, message = "已取消" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消付款单失败");
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

    public class FinancePaymentDecisionRequest
    {
        public string? Remark { get; set; }
    }
}
