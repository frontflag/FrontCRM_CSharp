using CRM.Core.Interfaces;
using CRM.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers
{
    [RequirePermission("finance-sell-invoice.read")]
    [ApiController]
    [Route("api/v1/finance/sell-invoices")]
    public class FinanceSellInvoicesController : ControllerBase
    {
        private readonly IFinanceSellInvoiceService _service;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly ILogger<FinanceSellInvoicesController> _logger;

        public FinanceSellInvoicesController(IFinanceSellInvoiceService service, IDataPermissionService dataPermissionService, ILogger<FinanceSellInvoicesController> logger)
        {
            _service = service;
            _dataPermissionService = dataPermissionService;
            _logger = logger;
        }

        /// <summary>获取销项发票列表</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? keyword,
            [FromQuery] short? invoiceStatus,
            [FromQuery] byte? receiveStatus,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var request = new FinanceSellInvoiceQueryRequest
                {
                    Keyword = keyword,
                    InvoiceStatus = invoiceStatus,
                    ReceiveStatus = receiveStatus,
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
                _logger.LogError(ex, "获取销项发票列表失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>获取单个销项发票</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var invoice = await _service.GetByIdAsync(id);
                if (invoice == null) return NotFound(new { success = false, message = "销项发票不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessFinanceSellInvoiceAsync(userId, invoice))
                    return StatusCode(403, new { success = false, message = "无权限访问该销项发票" });
                return Ok(new { success = true, data = invoice });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>新建销项发票</summary>
        [HttpPost]
        [RequirePermission("finance-sell-invoice.write")]
        public async Task<IActionResult> Create([FromBody] CreateFinanceSellInvoiceRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var invoice = await _service.CreateAsync(request, userId);
                return CreatedAtAction(nameof(GetById), new { id = invoice.Id },
                    new { success = true, data = invoice });
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
                _logger.LogError(ex, "新建销项发票失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>更新销项发票</summary>
        [HttpPut("{id}")]
        [RequirePermission("finance-sell-invoice.write")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateFinanceSellInvoiceRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var invoice = await _service.UpdateAsync(id, request, userId);
                return Ok(new { success = true, data = invoice });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "销项发票不存在" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新销项发票失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>更新销项发票开票状态（申请开票/已开票/开票失败）</summary>
        [HttpPatch("{id}/invoice-status")]
        [RequirePermission("finance-sell-invoice.write")]
        public async Task<IActionResult> UpdateInvoiceStatus(string id, [FromBody] UpdateInvoiceStatusRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.UpdateInvoiceStatusAsync(id, request.InvoiceStatus, userId);
                return Ok(new { success = true, message = "开票状态更新成功" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "销项发票不存在" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新销项发票开票状态失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>提交开票申请（1 -> 2）</summary>
        [HttpPost("{id}/submit-application")]
        [RequirePermission("finance-sell-invoice.write")]
        public async Task<IActionResult> SubmitApplication(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.UpdateInvoiceStatusAsync(id, 2, userId);
                return Ok(new { success = true, message = "已提交开票申请" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提交销项发票开票申请失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>开票成功（2 -> 100）</summary>
        [HttpPost("{id}/mark-issued")]
        [RequirePermission("finance-sell-invoice.write")]
        public async Task<IActionResult> MarkIssued(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.UpdateInvoiceStatusAsync(id, 100, userId);
                return Ok(new { success = true, message = "已标记开票成功" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "标记销项发票开票成功失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>开票失败（2 -> 101）</summary>
        [HttpPost("{id}/mark-issue-failed")]
        [RequirePermission("finance-sell-invoice.write")]
        public async Task<IActionResult> MarkIssueFailed(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.UpdateInvoiceStatusAsync(id, 101, userId);
                return Ok(new { success = true, message = "已标记开票失败" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "标记销项发票开票失败失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>作废销项发票</summary>
        [HttpPost("{id}/void")]
        [RequirePermission("finance-sell-invoice.write")]
        public async Task<IActionResult> Void(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.VoidAsync(id, userId);
                return Ok(new { success = true, message = "发票作废成功" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "销项发票不存在" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "作废销项发票失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>删除销项发票</summary>
        [HttpDelete("{id}")]
        [RequirePermission("finance-sell-invoice.write")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok(new { success = true, message = "删除成功" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "销项发票不存在" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除销项发票失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    public class UpdateInvoiceStatusRequest
    {
        public short InvoiceStatus { get; set; }
    }
}
