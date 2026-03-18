using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/finance/sell-invoices")]
    public class FinanceSellInvoicesController : ControllerBase
    {
        private readonly IFinanceSellInvoiceService _service;
        private readonly ILogger<FinanceSellInvoicesController> _logger;

        public FinanceSellInvoicesController(IFinanceSellInvoiceService service, ILogger<FinanceSellInvoicesController> logger)
        {
            _service = service;
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
                var all = await _service.GetAllAsync();
                var query = all.AsQueryable();

                if (!string.IsNullOrWhiteSpace(keyword))
                    query = query.Where(inv =>
                        (inv.CustomerName != null && inv.CustomerName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                        (inv.InvoiceCode != null && inv.InvoiceCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                        (inv.InvoiceNo != null && inv.InvoiceNo.Contains(keyword, StringComparison.OrdinalIgnoreCase)));

                if (invoiceStatus.HasValue)
                    query = query.Where(inv => inv.InvoiceStatus == invoiceStatus.Value);

                if (receiveStatus.HasValue)
                    query = query.Where(inv => inv.ReceiveStatus == receiveStatus.Value);

                if (DateTime.TryParse(startDate, out var start))
                    query = query.Where(inv => inv.MakeInvoiceDate >= start);

                if (DateTime.TryParse(endDate, out var end))
                    query = query.Where(inv => inv.MakeInvoiceDate <= end.AddDays(1));

                var total = query.Count();
                var items = query
                    .OrderByDescending(inv => inv.CreateTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new { success = true, data = new { items, total, page, pageSize } });
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
                return Ok(new { success = true, data = invoice });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>新建销项发票</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFinanceSellInvoiceRequest request)
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
                _logger.LogError(ex, "新建销项发票失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>更新销项发票</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateFinanceSellInvoiceRequest request)
        {
            try
            {
                var invoice = await _service.UpdateAsync(id, request);
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
        public async Task<IActionResult> UpdateInvoiceStatus(string id, [FromBody] UpdateInvoiceStatusRequest request)
        {
            try
            {
                await _service.UpdateInvoiceStatusAsync(id, request.InvoiceStatus);
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

        /// <summary>作废销项发票</summary>
        [HttpPost("{id}/void")]
        public async Task<IActionResult> Void(string id)
        {
            try
            {
                await _service.VoidAsync(id);
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
