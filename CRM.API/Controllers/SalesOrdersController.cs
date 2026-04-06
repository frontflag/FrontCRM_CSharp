using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Sales;
using CRM.API.Authorization;
using System.Collections.Generic;
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
        private readonly ISalesOrderJourneyService _journeyService;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IRbacService _rbacService;
        private readonly IRepository<SellOrderItemExtend> _soItemExtendRepo;
        private readonly ILogger<SalesOrdersController> _logger;

        public SalesOrdersController(
            ISalesOrderService service,
            ISalesOrderJourneyService journeyService,
            IDataPermissionService dataPermissionService,
            IRbacService rbacService,
            IRepository<SellOrderItemExtend> soItemExtendRepo,
            ILogger<SalesOrdersController> logger)
        {
            _service = service;
            _journeyService = journeyService;
            _dataPermissionService = dataPermissionService;
            _rbacService = rbacService;
            _soItemExtendRepo = soItemExtendRepo;
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

        /// <summary>销售订单明细分页（须在 {id} 路由之前注册）</summary>
        [HttpGet("lines")]
        public async Task<IActionResult> GetSellOrderLines(
            [FromQuery] string? orderCreateStart,
            [FromQuery] string? orderCreateEnd,
            [FromQuery] string? customerName,
            [FromQuery] string? salesUserName,
            [FromQuery] string? sellOrderCode,
            [FromQuery] string? pn,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var request = new SellOrderItemLineQueryRequest
                {
                    OrderCreateStart = DateTime.TryParse(orderCreateStart, out var ds) ? ds : null,
                    OrderCreateEnd = DateTime.TryParse(orderCreateEnd, out var de) ? de : null,
                    CustomerName = customerName,
                    SalesUserName = salesUserName,
                    SellOrderCode = sellOrderCode,
                    Pn = pn,
                    Page = page,
                    PageSize = pageSize,
                    CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                };
                var result = await _service.GetSellOrderItemLinesPagedAsync(request);
                var summary = await GetPermissionSummaryAsync(request.CurrentUserId);
                var canViewCustomer = summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("customer.info.read") ?? false);
                var canViewAmount = summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("sales.amount.read") ?? false);
                var items = result.Items.Select(r => MaskSellOrderLine(r, canViewCustomer, canViewAmount)).ToList();
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        items,
                        total = result.TotalCount,
                        page = result.PageIndex,
                        pageSize = result.PageSize
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取销售订单明细列表失败");
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
                IReadOnlyDictionary<string, SellOrderItemExtend>? itemExtends = null;
                if (order.Items != null && order.Items.Count > 0)
                {
                    try
                    {
                        var ids = order.Items.Select(i => i.Id).ToList();
                        var extRows = (await _soItemExtendRepo.FindAsync(e => ids.Contains(e.Id))).ToList();
                        itemExtends = extRows.ToDictionary(e => e.Id, e => e, StringComparer.OrdinalIgnoreCase);
                    }
                    catch (Exception ex)
                    {
                        // 扩展表未迁移或与模型不一致时不阻断主单详情（进度列按 0 展示）
                        _logger.LogWarning(ex, "加载销售明细扩展失败，已跳过: SellOrderId={SellOrderId}", order.Id);
                    }
                }

                IReadOnlyDictionary<string, bool> stockOutGate =
                    new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                if (order.Items != null && order.Items.Count > 0)
                {
                    stockOutGate = await _service.GetStockOutApplyPurchaseGateBySellLineIdsAsync(
                        order.Items.Select(i => i.Id));
                }

                return Ok(new { success = true, data = MaskSalesOrder(order, summary, itemExtends, stockOutGate) });
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

        [HttpGet("{id}/journey")]
        public async Task<IActionResult> GetJourney(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var dto = await _journeyService.GetJourneyAsync(id, userId);
                return Ok(new { success = true, data = dto });
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new { success = false, message = "无权限访问该销售订单" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取订单旅程失败: {Id}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("sales-order.write")]
        public async Task<IActionResult> Create([FromBody] CreateSalesOrderRequest request)
        {
            try
            {
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var order = await _service.CreateAsync(request, actorId);
                return CreatedAtAction(nameof(GetById), new { id = order.Id },
                    new { success = true, data = order });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "创建销售订单业务冲突: {Message}", ex.Message);
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
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var order = await _service.UpdateAsync(id, request, actorId);
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
                var status = (SellOrderMainStatus)request.Status;
                if (!Enum.IsDefined(typeof(SellOrderMainStatus), status))
                    return BadRequest(new { success = false, message = "无效的销售订单主状态" });
                if (status == SellOrderMainStatus.Approved || status == SellOrderMainStatus.AuditFailed)
                    return BadRequest(new { success = false, message = "审核通过/拒绝请通过「待审批」菜单处理" });
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.UpdateStatusAsync(id, status, null, actorId);
                return Ok(new { success = true, message = "状态更新成功" });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
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

        private static object MaskSellOrderLine(SellOrderItemLineDto r, bool canViewCustomer, bool canViewAmount)
        {
            return new
            {
                r.SellOrderItemId,
                r.SellOrderId,
                r.SellOrderCode,
                r.SellOrderItemCode,
                r.OrderStatus,
                r.OrderCreateTime,
                CustomerId = canViewCustomer ? r.CustomerId : null,
                CustomerName = canViewCustomer ? r.CustomerName : null,
                r.SalesUserName,
                r.PN,
                r.Brand,
                r.Qty,
                Price = canViewAmount ? r.Price : 0m,
                LineTotal = canViewAmount ? r.LineTotal : 0m,
                r.Currency,
                UsdUnitPrice = canViewAmount ? r.UsdUnitPrice : null,
                UsdLineTotal = canViewAmount ? r.UsdLineTotal : null,
                SalesProfitExpected = canViewAmount ? (decimal?)r.SalesProfitExpected : null,
                ProfitOutBizUsd = canViewAmount ? (decimal?)r.ProfitOutBizUsd : null,
                ProfitOutRateBiz = canViewAmount ? (decimal?)r.ProfitOutRateBiz : null,
                r.ItemStatus,
                r.PurchaseProgressStatus,
                r.StockInProgressStatus,
                r.StockOutProgressStatus,
                r.ReceiptProgressStatus,
                r.InvoiceProgressStatus,
                r.StockOutApplyPurchaseGateOk
            };
        }

        /// <summary>与 <see cref="ISalesOrderService.GetSellOrderItemLinesPagedAsync"/> 中明细行 USD 折算口径一致。</summary>
        private static (decimal? UsdUnit, decimal? UsdLine) GetSellOrderItemUsdSnapshot(SellOrderItem i)
        {
            decimal? usdUnit;
            decimal? usdLine;
            if (i.Currency == (short)CurrencyCode.USD)
            {
                usdUnit = i.ConvertPrice;
                usdLine = Math.Round(i.Qty * i.ConvertPrice, 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                usdUnit = i.ConvertPrice != 0m ? i.ConvertPrice : null;
                usdLine = usdUnit.HasValue
                    ? Math.Round(i.Qty * usdUnit.Value, 2, MidpointRounding.AwayFromZero)
                    : null;
            }

            return (usdUnit, usdLine);
        }

        private object MaskSalesOrder(CRM.Core.Models.Sales.SellOrder order, UserPermissionSummaryDto? summary,
            IReadOnlyDictionary<string, SellOrderItemExtend>? itemExtends = null,
            IReadOnlyDictionary<string, bool>? stockOutApplyPurchaseGate = null)
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
                order.AuditRemark,
                order.CreateTime,
                order.ModifyTime,
                order.CreateByUserId,
                order.ModifyByUserId,
                Items = (order.Items ?? Enumerable.Empty<CRM.Core.Models.Sales.SellOrderItem>()).Select(i =>
                {
                    SellOrderItemExtend? ext = null;
                    itemExtends?.TryGetValue(i.Id, out ext);
                    var (usdUnit, usdLine) = GetSellOrderItemUsdSnapshot(i);
                    return new
                    {
                        i.Id,
                        i.SellOrderId,
                        i.SellOrderItemCode,
                        i.QuoteId,
                        i.ProductId,
                        i.PN,
                        i.Brand,
                        CustomerPnNo = canViewCustomerInfo ? i.CustomerPnNo : null,
                        i.Qty,
                        i.PurchasedQty,
                        Price = canViewSalesAmount ? i.Price : 0m,
                        ConvertPrice = canViewSalesAmount ? i.ConvertPrice : 0m,
                        UsdUnitPrice = canViewSalesAmount ? usdUnit : null,
                        UsdLineTotal = canViewSalesAmount ? usdLine : null,
                        SalesProfitExpected = canViewSalesAmount
                            ? (decimal?)(ext?.SalesProfitExpected ?? 0m)
                            : null,
                        ProfitOutBizUsd = canViewSalesAmount
                            ? (decimal?)(ext?.ProfitOutBizUsd ?? 0m)
                            : null,
                        ProfitOutRateBiz = canViewSalesAmount
                            ? (decimal?)(ext?.ProfitOutRateBiz ?? 0m)
                            : null,
                        i.Currency,
                        i.DateCode,
                        i.DeliveryDate,
                        i.Status,
                        i.Comment,
                        i.CreateTime,
                        i.ModifyTime,
                        purchaseProgressStatus = ext?.PurchaseProgressStatus ?? (short)0,
                        stockInProgressStatus = ext?.StockInProgressStatus ?? (short)0,
                        stockOutProgressStatus = ext?.StockOutProgressStatus ?? (short)0,
                        receiptProgressStatus = ext?.ReceiptProgressStatus ?? (short)0,
                        invoiceProgressStatus = ext?.InvoiceProgressStatus ?? (short)0,
                        stockOutApplyPurchaseGateOk = stockOutApplyPurchaseGate != null &&
                            !string.IsNullOrWhiteSpace(i.Id) &&
                            stockOutApplyPurchaseGate.TryGetValue(i.Id.Trim(), out var gateOk) &&
                            gateOk
                    };
                }).ToList()
            };
        }
    }

    public class SalesOrderUpdateStatusRequest
    {
        public short Status { get; set; }
    }
}
