using CRM.Core.Interfaces;
using CRM.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers
{
    [RequirePermission("sales-order.read")]
    [ApiController]
    [Route("api/v1/sales-orders")]
    public class SalesOrdersController : ControllerBase
    {
        private readonly ISalesOrderService _service;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IRbacService _rbacService;
        private readonly ILogger<SalesOrdersController> _logger;

        public SalesOrdersController(
            ISalesOrderService service,
            IDataPermissionService dataPermissionService,
            IRbacService rbacService,
            ILogger<SalesOrdersController> logger)
        {
            _service = service;
            _dataPermissionService = dataPermissionService;
            _rbacService = rbacService;
            _logger = logger;
        }

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
                var request = new SalesOrderQueryRequest
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
                var summary = await GetPermissionSummaryAsync(request.CurrentUserId);
                var items = result.Items.Select(x => MaskSalesOrder(x, summary)).ToList();
                return Ok(new { success = true, data = new { items, total = result.TotalCount, page = result.PageIndex, pageSize = result.PageSize } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取销售订单列表失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var order = await _service.GetByIdAsync(id);
                if (order == null) return NotFound(new { success = false, message = "销售订单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessSalesOrderAsync(userId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该销售订单" });
                var summary = await GetPermissionSummaryAsync(userId);
                return Ok(new { success = true, data = MaskSalesOrder(order, summary) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("by-customer/{customerId}")]
        public async Task<IActionResult> GetByCustomer(string customerId)
        {
            try
            {
                var orders = await _service.GetByCustomerIdAsync(customerId);
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var summary = await GetPermissionSummaryAsync(userId);
                return Ok(new { success = true, data = orders.Select(x => MaskSalesOrder(x, summary)).ToList() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}/purchase-orders")]
        public async Task<IActionResult> GetRelatedPurchaseOrders(string id)
        {
            try
            {
                var pos = await _service.GetRelatedPurchaseOrdersAsync(id);
                return Ok(new { success = true, data = pos });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("sales-order.write")]
        public async Task<IActionResult> Create([FromBody] CreateSalesOrderRequest request)
        {
            try
            {
                var order = await _service.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = order.Id },
                    new { success = true, data = order });
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
                _logger.LogError(ex, "创建销售订单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [RequirePermission("sales-order.write")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateSalesOrderRequest request)
        {
            try
            {
                var order = await _service.UpdateAsync(id, request);
                return Ok(new { success = true, data = order });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [RequirePermission("sales-order.write")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok(new { success = true, message = "删除成功" });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPatch("{id}/status")]
        [RequirePermission("sales-order.write")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] SalesOrderUpdateStatusRequest request)
        {
            try
            {
                await _service.UpdateStatusAsync(id, request.Status);
                return Ok(new { success = true, message = "状态更新成功" });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private async Task<UserPermissionSummaryDto?> GetPermissionSummaryAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;
            return await _rbacService.GetUserPermissionSummaryAsync(userId);
        }

        private object MaskSalesOrder(CRM.Core.Models.Sales.SellOrder order, UserPermissionSummaryDto? summary)
        {
            var canViewCustomerInfo = summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("customer.info.read") ?? false);
            var canViewSalesAmount = summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("sales.amount.read") ?? false);

            return new
            {
                order.Id,
                order.SellOrderCode,
                CustomerId = canViewCustomerInfo ? order.CustomerId : null,
                CustomerName = canViewCustomerInfo ? order.CustomerName : null,
                order.SalesUserId,
                order.SalesUserName,
                order.Status,
                order.Type,
                order.Currency,
                Total = canViewSalesAmount ? order.Total : 0m,
                ConvertTotal = canViewSalesAmount ? order.ConvertTotal : 0m,
                order.ItemRows,
                order.PurchaseOrderStatus,
                order.StockOutStatus,
                order.StockInStatus,
                order.FinanceReceiptStatus,
                order.FinancePaymentStatus,
                order.InvoiceStatus,
                order.DeliveryAddress,
                order.DeliveryDate,
                order.Comment,
                order.CreateTime,
                order.ModifyTime,
                Items = (order.Items ?? Enumerable.Empty<CRM.Core.Models.Sales.SellOrderItem>()).Select(i => new
                {
                    i.Id,
                    i.SellOrderId,
                    i.QuoteId,
                    i.ProductId,
                    i.PN,
                    i.Brand,
                    CustomerPnNo = canViewCustomerInfo ? i.CustomerPnNo : null,
                    i.Qty,
                    i.PurchasedQty,
                    Price = canViewSalesAmount ? i.Price : 0m,
                    i.Currency,
                    i.DateCode,
                    i.DeliveryDate,
                    i.Status,
                    i.Comment,
                    i.CreateTime,
                    i.ModifyTime
                }).ToList()
            };
        }
    }

    public class SalesOrderUpdateStatusRequest
    {
        public short Status { get; set; }
    }
}
