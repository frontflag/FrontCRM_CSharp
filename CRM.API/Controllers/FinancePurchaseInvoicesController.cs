using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/finance/purchase-invoices")]
    public class FinancePurchaseInvoicesController : ControllerBase
    {
        private readonly IFinancePurchaseInvoiceService _service;
        private readonly ILogger<FinancePurchaseInvoicesController> _logger;

        public FinancePurchaseInvoicesController(IFinancePurchaseInvoiceService service, ILogger<FinancePurchaseInvoicesController> logger)
        {
            _service = service;
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
                var all = await _service.GetAllAsync();
                var query = all.AsQueryable();

                if (!string.IsNullOrWhiteSpace(keyword))
                    query = query.Where(inv =>
                        (inv.VendorName != null && inv.VendorName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                        (inv.InvoiceNo != null && inv.InvoiceNo.Contains(keyword, StringComparison.OrdinalIgnoreCase)));

                if (confirmStatus.HasValue)
                    query = query.Where(inv => inv.ConfirmStatus == confirmStatus.Value);

                if (invoiceStatus.HasValue)
                    query = query.Where(inv => inv.RedInvoiceStatus == invoiceStatus.Value);

                if (DateTime.TryParse(startDate, out var start))
                    query = query.Where(inv => inv.InvoiceDate >= start);

                if (DateTime.TryParse(endDate, out var end))
                    query = query.Where(inv => inv.InvoiceDate <= end.AddDays(1));

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
                return Ok(new { success = true, data = invoice });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>新建进项发票</summary>
        [HttpPost]
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
