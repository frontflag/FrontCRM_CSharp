using CRM.Core.Interfaces;
using CRM.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers
{
    [RequirePermission("finance-purchase-invoice.read")]
    [ApiController]
    [Route("api/v1/finance/purchase-invoices")]
    public class FinancePurchaseInvoicesController : ControllerBase
    {
        private readonly IFinancePurchaseInvoiceService _service;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly ILogger<FinancePurchaseInvoicesController> _logger;

        public FinancePurchaseInvoicesController(IFinancePurchaseInvoiceService service, IDataPermissionService dataPermissionService, ILogger<FinancePurchaseInvoicesController> logger)
        {
            _service = service;
            _dataPermissionService = dataPermissionService;
            _logger = logger;
        }

        /// <summary>获取进项发票列表</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? keyword,
            [FromQuery] short? invoiceStatus,
            [FromQuery] byte? confirmStatus,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var request = new FinancePurchaseInvoiceQueryRequest
                {
                    Keyword = keyword,
                    InvoiceStatus = invoiceStatus,
                    ConfirmStatus = confirmStatus,
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
                _logger.LogError(ex, "获取进项发票列表失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>获取单个进项发票</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var invoice = await _service.GetByIdAsync(id);
                if (invoice == null) return NotFound(new { success = false, message = "进项发票不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessFinancePurchaseInvoiceAsync(userId, invoice))
                    return StatusCode(403, new { success = false, message = "无权限访问该进项发票" });
                return Ok(new { success = true, data = invoice });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>新建进项发票</summary>
        [HttpPost]
        [RequirePermission("finance-purchase-invoice.write")]
        public async Task<IActionResult> Create([FromBody] CreateFinancePurchaseInvoiceRequest request)
        {
            try
            {
                var invoice = await _service.CreateAsync(request);
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
                _logger.LogError(ex, "新建进项发票失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>更新进项发票</summary>
        [HttpPut("{id}")]
        [RequirePermission("finance-purchase-invoice.write")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateFinancePurchaseInvoiceRequest request)
        {
            try
            {
                var invoice = await _service.UpdateAsync(id, request);
                return Ok(new { success = true, data = invoice });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "进项发票不存在" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新进项发票失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>认证进项发票</summary>
        [HttpPost("{id}/confirm")]
        [RequirePermission("finance-purchase-invoice.write")]
        public async Task<IActionResult> Confirm(string id, [FromBody] ConfirmInvoiceRequest request)
        {
            try
            {
                await _service.ConfirmAsync(id, request.ConfirmDate);
                return Ok(new { success = true, message = "发票认证成功" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "进项发票不存在" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "认证进项发票失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>冲红进项发票</summary>
        [HttpPost("{id}/red-invoice")]
        [RequirePermission("finance-purchase-invoice.write")]
        public async Task<IActionResult> RedInvoice(string id)
        {
            try
            {
                await _service.RedInvoiceAsync(id);
                return Ok(new { success = true, message = "发票冲红成功" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "进项发票不存在" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "冲红进项发票失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>删除进项发票</summary>
        [HttpDelete("{id}")]
        [RequirePermission("finance-purchase-invoice.write")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok(new { success = true, message = "删除成功" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "进项发票不存在" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除进项发票失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    public class ConfirmInvoiceRequest
    {
        public DateTime ConfirmDate { get; set; } = DateTime.UtcNow;
    }
}
