using CRM.Core.Interfaces;
using CRM.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers
{
    [RequirePermission("purchase-order.read")]
    [ApiController]
    [Route("api/v1/purchase-orders")]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly IPurchaseOrderService _service;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IRbacService _rbacService;
        private readonly ILogger<PurchaseOrdersController> _logger;

        public PurchaseOrdersController(
            IPurchaseOrderService service,
            IDataPermissionService dataPermissionService,
            IRbacService rbacService,
            ILogger<PurchaseOrdersController> logger)
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
                var request = new PurchaseOrderQueryRequest
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
                var items = result.Items.Select(x => MaskPurchaseOrder(x, summary)).ToList();
                return Ok(new { success = true, data = new { items, total = result.TotalCount, page = result.PageIndex, pageSize = result.PageSize } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取采购订单列表失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var order = await _service.GetByIdAsync(id);
                if (order == null) return NotFound(new { success = false, message = "采购订单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessPurchaseOrderAsync(userId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该采购订单" });
                var summary = await GetPermissionSummaryAsync(userId);
                return Ok(new { success = true, data = MaskPurchaseOrder(order, summary) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("by-sell-order/{sellOrderCode}")]
        public async Task<IActionResult> GetBySellOrder(string sellOrderCode)
        {
            try
            {
                var orders = await _service.GetBySellOrderCodeAsync(sellOrderCode);
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var summary = await GetPermissionSummaryAsync(userId);
                return Ok(new { success = true, data = orders.Select(x => MaskPurchaseOrder(x, summary)).ToList() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("purchase-order.write")]
        public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderRequest request)
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
                _logger.LogError(ex, "创建采购订单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [RequirePermission("purchase-order.write")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdatePurchaseOrderRequest request)
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
        [RequirePermission("purchase-order.write")]
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
        [RequirePermission("purchase-order.write")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] PurchaseOrderUpdateStatusRequest request)
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

        /// <summary>以销定采：根据销售订单自动生成采购订单</summary>
        [HttpPost("auto-generate/{sellOrderId}")]
        [RequirePermission("purchase-order.write")]
        public async Task<IActionResult> AutoGenerate(string sellOrderId)
        {
            try
            {
                var orders = await _service.AutoGenerateFromSellOrderAsync(sellOrderId);
                return Ok(new { success = true, data = orders, message = $"成功生成 {orders.Count()} 张采购订单" });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自动生成采购订单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private async Task<UserPermissionSummaryDto?> GetPermissionSummaryAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;
            return await _rbacService.GetUserPermissionSummaryAsync(userId);
        }

        private object MaskPurchaseOrder(CRM.Core.Models.Purchase.PurchaseOrder order, UserPermissionSummaryDto? summary)
        {
            var canViewVendorInfo = summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("vendor.info.read") ?? false);
            var canViewPurchaseAmount = summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("purchase.amount.read") ?? false);

            return new
            {
                order.Id,
                order.PurchaseOrderCode,
                VendorId = canViewVendorInfo ? order.VendorId : null,
                VendorName = canViewVendorInfo ? order.VendorName : null,
                VendorCode = canViewVendorInfo ? order.VendorCode : null,
                VendorContactId = canViewVendorInfo ? order.VendorContactId : null,
                order.PurchaseUserId,
                order.PurchaseUserName,
                order.Status,
                order.Type,
                order.Currency,
                Total = canViewPurchaseAmount ? order.Total : 0m,
                ConvertTotal = canViewPurchaseAmount ? order.ConvertTotal : 0m,
                order.ItemRows,
                order.StockStatus,
                order.FinanceStatus,
                order.StockOutStatus,
                order.InvoiceStatus,
                order.DeliveryAddress,
                order.DeliveryDate,
                order.Comment,
                order.InnerComment,
                order.CreateTime,
                order.ModifyTime,
                Items = (order.Items ?? Enumerable.Empty<CRM.Core.Models.Purchase.PurchaseOrderItem>()).Select(i => new
                {
                    i.Id,
                    i.PurchaseOrderId,
                    i.SellOrderItemId,
                    VendorId = canViewVendorInfo ? i.VendorId : null,
                    i.ProductId,
                    i.PN,
                    i.Brand,
                    i.Qty,
                    Cost = canViewPurchaseAmount ? i.Cost : 0m,
                    i.Currency,
                    i.Status,
                    i.StockInStatus,
                    i.FinancePaymentStatus,
                    i.StockOutStatus,
                    i.ErrStatus,
                    i.DeliveryDate,
                    i.Comment,
                    i.InnerComment,
                    i.CreateTime,
                    i.ModifyTime
                }).ToList()
            };
        }
    }

    public class PurchaseOrderUpdateStatusRequest
    {
        public short Status { get; set; }
    }
}
