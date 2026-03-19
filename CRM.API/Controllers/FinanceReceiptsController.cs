using CRM.Core.Interfaces;
using CRM.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers
{
    [RequirePermission("finance-receipt.read")]
    [ApiController]
    [Route("api/v1/finance/receipts")]
    public class FinanceReceiptsController : ControllerBase
    {
        private readonly IFinanceReceiptService _service;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly ILogger<FinanceReceiptsController> _logger;

        public FinanceReceiptsController(IFinanceReceiptService service, IDataPermissionService dataPermissionService, ILogger<FinanceReceiptsController> logger)
        {
            _service = service;
            _dataPermissionService = dataPermissionService;
            _logger = logger;
        }

        /// <summary>获取收款单列表</summary>
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
                var request = new FinanceReceiptQueryRequest
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
                _logger.LogError(ex, "获取收款单列表失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>获取单个收款单</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var receipt = await _service.GetByIdAsync(id);
                if (receipt == null) return NotFound(new { success = false, message = "收款单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessFinanceReceiptAsync(userId, receipt))
                    return StatusCode(403, new { success = false, message = "无权限访问该收款单" });
                return Ok(new { success = true, data = receipt });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>新建收款单</summary>
        [HttpPost]
        [RequirePermission("finance-receipt.write")]
        public async Task<IActionResult> Create([FromBody] CreateFinanceReceiptRequest request)
        {
            try
            {
                var receipt = await _service.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = receipt.Id },
                    new { success = true, data = receipt });
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
                _logger.LogError(ex, "新建收款单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>更新收款单</summary>
        [HttpPut("{id}")]
        [RequirePermission("finance-receipt.write")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateFinanceReceiptRequest request)
        {
            try
            {
                var receipt = await _service.UpdateAsync(id, request);
                return Ok(new { success = true, data = receipt });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "收款单不存在" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新收款单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>更新收款单状态</summary>
        [HttpPatch("{id}/status")]
        [RequirePermission("finance-receipt.write")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] FinanceReceiptStatusRequest request)
        {
            try
            {
                await _service.UpdateStatusAsync(id, request.Status);
                return Ok(new { success = true, message = "状态更新成功" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "收款单不存在" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新收款单状态失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>删除收款单</summary>
        [HttpDelete("{id}")]
        [RequirePermission("finance-receipt.write")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok(new { success = true, message = "删除成功" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "收款单不存在" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除收款单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>核销收款明细</summary>
        [HttpPost("items/{receiptItemId}/verify")]
        [RequirePermission("finance-receipt.write")]
        public async Task<IActionResult> VerifyItem(string receiptItemId, [FromBody] VerifyReceiptItemRequest request)
        {
            try
            {
                await _service.VerifyReceiptItemAsync(receiptItemId, request.SellInvoiceId, request.Amount);
                return Ok(new { success = true, message = "核销成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "核销收款明细失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    public class FinanceReceiptStatusRequest
    {
        public short Status { get; set; }
    }

    public class VerifyReceiptItemRequest
    {
        public string SellInvoiceId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
