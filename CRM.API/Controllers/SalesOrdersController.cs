using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;
using CRM.API.Authorization;
using CRM.API.Services;
using CRM.Infrastructure.Data;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CRM.API.Controllers
{
    [RequirePermission("sales-order.read")]
    [ApiController]
    [Route("api/v1/sales-orders")]
    public class SalesOrdersController : ControllerBase
    {
        private readonly ISalesOrderService _service;
        private readonly ISalesOrderListQuery _salesOrderListQuery;
        private readonly ISalesOrderJourneyService _journeyService;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IRbacService _rbacService;
        private readonly IRepository<SellOrderItemExtend> _soItemExtendRepo;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SalesOrdersController> _logger;

        public SalesOrdersController(
            ISalesOrderService service,
            ISalesOrderListQuery salesOrderListQuery,
            ISalesOrderJourneyService journeyService,
            IDataPermissionService dataPermissionService,
            IRbacService rbacService,
            IRepository<SellOrderItemExtend> soItemExtendRepo,
            ApplicationDbContext db,
            ILogger<SalesOrdersController> logger)
        {
            _service = service;
            _salesOrderListQuery = salesOrderListQuery;
            _journeyService = journeyService;
            _dataPermissionService = dataPermissionService;
            _rbacService = rbacService;
            _soItemExtendRepo = soItemExtendRepo;
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? keyword,
            [FromQuery] string? code,
            [FromQuery] string? customer,
            [FromQuery] short? status,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var summary = await GetPermissionSummaryAsync(userId);
                var mask521 = SaleSensitiveFieldMask521.ShouldMask(summary);
                var canViewCustomerInfo = !mask521 && (summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("customer.info.read") ?? false));

                var request = new SalesOrderQueryRequest
                {
                    Keyword = keyword,
                    SellOrderCodeFilter = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
                    CustomerNameFilter = canViewCustomerInfo && !string.IsNullOrWhiteSpace(customer) ? customer.Trim() : null,
                    Status = status,
                    StartDate = DateTime.TryParse(startDate, out var start) ? start : null,
                    EndDate = DateTime.TryParse(endDate, out var end) ? end : null,
                    Page = page,
                    PageSize = pageSize,
                    CurrentUserId = userId
                };
                var result = await _service.GetPagedAsync(request);
                var items = result.Items.Select(x => MaskSalesOrder(x, summary)).ToList();
                var aggregates = await _salesOrderListQuery.GetAggregatesAsync(request, cancellationToken);
                var canViewSalesAmount = !mask521 && (summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("sales.amount.read") ?? false));
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        items,
                        total = result.TotalCount,
                        page = result.PageIndex,
                        pageSize = result.PageSize,
                        aggregates = new
                        {
                            totalCount = aggregates.TotalCount,
                            pendingCount = aggregates.PendingCount,
                            approvedPlusCount = aggregates.ApprovedPlusCount,
                            totalAmountSum = canViewSalesAmount ? aggregates.TotalAmountSum : (decimal?)null
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取销售订单列表失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>销售订单明细分页（字面路由 <c>items</c>；与 <c>{id:guid}</c> 子路由并存，避免 <c>items</c> 被误解析为订单主键）。</summary>
        [HttpGet("items")]
        public async Task<IActionResult> GetSellOrderItemLines(
            [FromQuery] string? orderCreateStart,
            [FromQuery] string? orderCreateEnd,
            [FromQuery] string? customerName,
            [FromQuery] string? salesUserName,
            [FromQuery] string? sellOrderCode,
            [FromQuery] string? pn,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var summary = await GetPermissionSummaryAsync(userId);
                var mask521 = SaleSensitiveFieldMask521.ShouldMask(summary);
                var canViewCustomer = !mask521 && (summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("customer.info.read") ?? false));
                var canViewSalesUser = summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("sales.user.read") ?? false)
                    || (summary?.PermissionCodes?.Contains("sales-order.read") ?? false);

                var request = new SellOrderItemLineQueryRequest
                {
                    OrderCreateStart = DateTime.TryParse(orderCreateStart, out var ds) ? ds : null,
                    OrderCreateEnd = DateTime.TryParse(orderCreateEnd, out var de) ? de : null,
                    CustomerName = canViewCustomer && !string.IsNullOrWhiteSpace(customerName) ? customerName.Trim() : null,
                    SalesUserName = canViewSalesUser && !string.IsNullOrWhiteSpace(salesUserName) ? salesUserName.Trim() : null,
                    SellOrderCode = sellOrderCode,
                    Pn = pn,
                    Page = page,
                    PageSize = pageSize,
                    CurrentUserId = userId
                };
                var result = await _service.GetSellOrderItemLinesPagedAsync(request);
                var canViewAmount = !mask521 && (summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("sales.amount.read") ?? false));
                var items = result.Items.Select(r => MaskSellOrderLine(r, canViewCustomer, canViewAmount, mask521)).ToList();
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

        /// <summary>销售订单报表页：一次返回订单详情（与详情权限、脱敏一致）与公司参数。</summary>
        [HttpGet("{id:guid}/report-data")]
        public async Task<IActionResult> GetReportData(string id, CancellationToken cancellationToken)
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
                    var ids = order.Items
                        .Select(i => i.Id)
                        .Where(id => !string.IsNullOrWhiteSpace(id))
                        .Select(id => id.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    itemExtends = await LoadSellOrderItemExtendsByItemIdsAsync(ids, order.Id);
                }

                IReadOnlyDictionary<string, bool> stockOutGate =
                    new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                if (order.Items != null && order.Items.Count > 0)
                {
                    stockOutGate = await _service.GetStockOutApplyPurchaseGateBySellLineIdsAsync(
                        order.Items.Select(i => i.Id));
                }

                var companyProfile = await CompanyProfileBundleLoader.LoadAsync(_db, _logger, cancellationToken);
                CompanyProfileBundleLoader.StripSmtpEmail(companyProfile);
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        order = MaskSalesOrder(order, summary, itemExtends, stockOutGate),
                        companyProfile
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取销售订单报表数据失败: {Id}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>销售订单详情页：底部页签用下游列表（采购申请/入库/库存/出库通知/出库/收款/销项发票）。</summary>
        [HttpGet("{id:guid}/detail-tab-aggregates")]
        public async Task<IActionResult> GetDetailTabAggregates(string id)
        {
            try
            {
                var order = await _service.GetByIdAsync(id);
                if (order == null) return NotFound(new { success = false, message = "销售订单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessSalesOrderAsync(userId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该销售订单" });
                var summary = await GetPermissionSummaryAsync(userId);
                var mask521 = SaleSensitiveFieldMask521.ShouldMask(summary);

                var itemIds = (order.Items ?? new List<SellOrderItem>())
                    .Select(i => i.Id)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var data = await BuildSalesOrderDetailTabAggregatesPayloadAsync(id, itemIds, sellOrderItemIdScope: null, mask521);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取销售订单页签数据失败: {Id}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>销售订单「单条明细」下游列表：与 <c>detail-tab-aggregates</c> 字段一致，按销售明细主键过滤。</summary>
        [HttpGet("{id:guid}/sell-order-items/{sellOrderItemId:guid}/detail-tab-aggregates")]
        public async Task<IActionResult> GetSellOrderItemDetailTabAggregates(string id, string sellOrderItemId)
        {
            try
            {
                var lineId = (sellOrderItemId ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(lineId))
                    return BadRequest(new { success = false, message = "销售订单明细主键无效" });

                var order = await _service.GetByIdAsync(id);
                if (order == null) return NotFound(new { success = false, message = "销售订单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessSalesOrderAsync(userId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该销售订单" });

                var orderLineIds = (order.Items ?? new List<SellOrderItem>())
                    .Select(i => i.Id)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (!orderLineIds.Contains(lineId, StringComparer.OrdinalIgnoreCase))
                    return NotFound(new { success = false, message = "销售订单明细不属于该订单" });

                var summary = await GetPermissionSummaryAsync(userId);
                var mask521 = SaleSensitiveFieldMask521.ShouldMask(summary);

                var data = await BuildSalesOrderDetailTabAggregatesPayloadAsync(id, orderLineIds, sellOrderItemIdScope: lineId, mask521);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取销售订单明细页签数据失败: {OrderId} {ItemId}", id, sellOrderItemId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <param name="sellOrderItemIdScope">非 null 时：采购申请、出库通知、收款明细仅保留该销售明细；在库/出库/销项发票链仅使用该明细。</param>
        private async Task<object> BuildSalesOrderDetailTabAggregatesPayloadAsync(
            string orderId,
            IReadOnlyList<string> allOrderLineIds,
            string? sellOrderItemIdScope,
            bool mask521)
        {
            var itemIds = sellOrderItemIdScope != null
                ? new List<string> { sellOrderItemIdScope }
                : allOrderLineIds.ToList();

            var prQuery = _db.PurchaseRequisitions.AsNoTracking().Where(p => p.SellOrderId == orderId);
            if (sellOrderItemIdScope != null)
                prQuery = prQuery.Where(p => p.SellOrderItemId == sellOrderItemIdScope);
            var prRows = await prQuery
                .OrderByDescending(p => p.CreateTime)
                .Select(p => new
                {
                    id = p.Id,
                    billCode = p.BillCode,
                    p.Status,
                    sellOrderItemId = p.SellOrderItemId,
                    p.PN,
                    p.Brand,
                    p.Qty,
                    p.ExpectedPurchaseTime,
                    p.CreateTime
                })
                .ToListAsync();

            List<string> stockInIdList;
            if (itemIds.Count == 0)
                stockInIdList = new List<string>();
            else
                stockInIdList = await _db.StockInItemExtends.AsNoTracking()
                    .Where(e => e.SellOrderItemId != null && itemIds.Contains(e.SellOrderItemId!))
                    .Select(e => e.StockInId)
                    .Distinct()
                    .ToListAsync();

            var stockInRows = await _db.StockIns.AsNoTracking()
                .Where(si => stockInIdList.Contains(si.Id))
                .OrderByDescending(si => si.CreateTime)
                .Select(si => new
                {
                    id = si.Id,
                    stockInCode = si.StockInCode,
                    si.StockInType,
                    si.Status,
                    si.StockInDate,
                    si.CreateTime
                })
                .ToListAsync();

            List<object> stockItemRows;
            if (itemIds.Count == 0)
            {
                stockItemRows = new List<object>();
            }
            else
            {
                var rawStockItems = await _db.StockItems.AsNoTracking()
                    .Where(s => s.SellOrderItemId != null && itemIds.Contains(s.SellOrderItemId!))
                    .OrderByDescending(s => s.CreateTime)
                    .Select(s => new
                    {
                        s.Id,
                        s.StockItemCode,
                        s.StockAggregateId,
                        s.RegionType,
                        s.PurchasePn,
                        s.PurchaseBrand,
                        s.StockOutStatus,
                        s.QtyInbound,
                        s.QtyStockOut,
                        s.QtyRepertory,
                        s.QtyRepertoryAvailable,
                        s.SellOrderItemId,
                        s.SellOrderItemCode,
                        s.WarehouseId,
                        s.StockInId,
                        s.PurchaseOrderItemCode,
                        s.BatchNo,
                        s.LocationId
                    })
                    .ToListAsync();

                var stockInIds = rawStockItems
                    .Select(x => x.StockInId)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var stockInMap = await _db.StockIns.AsNoTracking()
                    .Where(x => stockInIds.Contains(x.Id))
                    .Select(x => new { x.Id, x.StockInCode, x.StockInDate })
                    .ToDictionaryAsync(x => x.Id, StringComparer.OrdinalIgnoreCase);

                var warehouseIds = rawStockItems
                    .Select(x => x.WarehouseId)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var warehouseNameMap = await _db.Warehouses.AsNoTracking()
                    .Where(x => warehouseIds.Contains(x.Id))
                    .Select(x => new { x.Id, x.WarehouseName })
                    .ToDictionaryAsync(x => x.Id, x => x.WarehouseName, StringComparer.OrdinalIgnoreCase);

                stockItemRows = rawStockItems
                    .Select(s =>
                    {
                        var stockInId = s.StockInId?.Trim();
                        var stockInCode = default(string);
                        DateTime? stockInDate = null;
                        if (!string.IsNullOrWhiteSpace(stockInId) && stockInMap.TryGetValue(stockInId, out var sin))
                        {
                            stockInCode = string.IsNullOrWhiteSpace(sin.StockInCode) ? null : sin.StockInCode.Trim();
                            stockInDate = sin.StockInDate;
                        }

                        var warehouseId = s.WarehouseId?.Trim();
                        var warehouseName = default(string);
                        if (!string.IsNullOrWhiteSpace(warehouseId) && warehouseNameMap.TryGetValue(warehouseId, out var wn))
                            warehouseName = string.IsNullOrWhiteSpace(wn) ? null : wn.Trim();

                        return (object)new
                        {
                            s.Id,
                            s.StockItemCode,
                            s.StockAggregateId,
                            stockInCode,
                            stockInDate,
                            warehouseName,
                            s.RegionType,
                            s.PurchasePn,
                            s.PurchaseBrand,
                            stockOutStatus = s.StockOutStatus,
                            qtyInbound = s.QtyInbound,
                            qtyStockOut = s.QtyStockOut,
                            s.QtyRepertory,
                            s.QtyRepertoryAvailable,
                            s.SellOrderItemId,
                            s.SellOrderItemCode,
                            s.WarehouseId,
                            s.PurchaseOrderItemCode,
                            s.BatchNo,
                            s.LocationId
                        };
                    })
                    .ToList();
            }

            var outReqQuery = _db.StockOutRequests.AsNoTracking().Where(r => r.SalesOrderId == orderId);
            if (sellOrderItemIdScope != null)
                outReqQuery = outReqQuery.Where(r => r.SalesOrderItemId == sellOrderItemIdScope);
            var outReqRows = await outReqQuery
                .OrderByDescending(r => r.CreateTime)
                .Select(r => new
                {
                    r.Id,
                    r.RequestCode,
                    r.MaterialCode,
                    r.Quantity,
                    r.Status,
                    r.RequestDate,
                    r.CreateTime
                })
                .ToListAsync();

            List<object> stockOutRows;
            if (itemIds.Count == 0)
            {
                stockOutRows = new List<object>();
            }
            else
            {
                stockOutRows = (await _db.StockOuts.AsNoTracking()
                    .Where(so => so.SellOrderItemId != null && itemIds.Contains(so.SellOrderItemId!))
                    .OrderByDescending(so => so.CreateTime)
                    .Select(so => new
                    {
                        so.Id,
                        stockOutCode = so.StockOutCode,
                        so.Status,
                        so.TotalQuantity,
                        so.StockOutDate,
                        so.SellOrderItemId,
                        so.CreateTime
                    })
                    .ToListAsync()).Cast<object>().ToList();
            }

            var receiptItemQuery = _db.FinanceReceiptItems.AsNoTracking();
            if (sellOrderItemIdScope != null)
                receiptItemQuery = receiptItemQuery.Where(i => i.SellOrderItemId == sellOrderItemIdScope);
            else
                receiptItemQuery = receiptItemQuery.Where(i => i.SellOrderId == orderId
                    || (i.SellOrderItemId != null && itemIds.Contains(i.SellOrderItemId!)));
            var receiptHeaderIds = await receiptItemQuery
                .Select(i => i.FinanceReceiptId)
                .Distinct()
                .ToListAsync();

            var receiptEntities = await _db.FinanceReceipts.AsNoTracking()
                .Where(r => receiptHeaderIds.Contains(r.Id))
                .OrderByDescending(r => r.CreateTime)
                .ToListAsync();

            var receiptRows = receiptEntities
                .Select(r =>
                {
                    var cname = r.CustomerName;
                    var amt = r.ReceiptAmount;
                    if (mask521)
                    {
                        cname = null;
                        amt = 0m;
                    }
                    return new
                    {
                        id = r.Id,
                        financeReceiptCode = r.FinanceReceiptCode,
                        r.Status,
                        customerName = cname,
                        receiptAmount = amt,
                        r.ReceiptCurrency,
                        r.ReceiptDate,
                        r.CreateTime
                    };
                })
                .ToList();

            List<string> sellInvIds;
            if (itemIds.Count == 0)
            {
                sellInvIds = new List<string>();
            }
            else
            {
                var soIds = await _db.StockOuts.AsNoTracking()
                    .Where(so => so.SellOrderItemId != null && itemIds.Contains(so.SellOrderItemId!))
                    .Select(so => so.Id)
                    .ToListAsync();
                var outItemIds = await _db.StockOutItems.AsNoTracking()
                    .Where(oi => soIds.Contains(oi.StockOutId))
                    .Select(oi => oi.Id)
                    .ToListAsync();
                sellInvIds = await _db.SellInvoiceItems.AsNoTracking()
                    .Where(sii => sii.StockOutItemId != null && outItemIds.Contains(sii.StockOutItemId!))
                    .Select(sii => sii.FinanceSellInvoiceId)
                    .Distinct()
                    .ToListAsync();
            }

            var invEntities = await _db.FinanceSellInvoices.AsNoTracking()
                .Where(x => sellInvIds.Contains(x.Id))
                .OrderByDescending(x => x.CreateTime)
                .ToListAsync();

            var sellInvRows = invEntities
                .Select(inv =>
                {
                    var cname = inv.CustomerName;
                    var total = inv.InvoiceTotal;
                    var done = inv.ReceiveDone;
                    var tobe = inv.ReceiveToBe;
                    if (mask521)
                    {
                        cname = null;
                        total = 0m;
                        done = 0m;
                        tobe = 0m;
                    }
                    return new
                    {
                        id = inv.Id,
                        inv.InvoiceCode,
                        inv.InvoiceNo,
                        customerName = cname,
                        invoiceTotal = total,
                        inv.MakeInvoiceDate,
                        inv.InvoiceStatus,
                        receiveDone = done,
                        receiveToBe = tobe,
                        inv.Currency,
                        inv.CreateTime
                    };
                })
                .ToList();

            return new
            {
                purchaseRequisitions = prRows,
                stockIns = stockInRows,
                stockItems = stockItemRows,
                stockOutRequests = outReqRows,
                stockOuts = stockOutRows,
                receipts = receiptRows,
                sellInvoices = sellInvRows
            };
        }

        [HttpGet("{id:guid}")]
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
                    var ids = order.Items
                        .Select(i => i.Id)
                        .Where(oid => !string.IsNullOrWhiteSpace(oid))
                        .Select(oid => oid.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    itemExtends = await LoadSellOrderItemExtendsByItemIdsAsync(ids, order.Id);
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

        [HttpGet("{id:guid}/purchase-orders")]
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

        [HttpGet("{id:guid}/journey")]
        public async Task<IActionResult> GetJourney(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var dto = await _journeyService.GetJourneyAsync(id, userId);
                var summary = await GetPermissionSummaryAsync(userId);
                if (SaleSensitiveFieldMask521.ShouldMask(summary))
                    SaleSensitiveFieldMask521.ApplySalesOrderJourney(dto, true);
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

        [HttpPost("{id:guid}/refresh-item-extends")]
        [RequirePermission("sales-order.write")]
        public async Task<IActionResult> RefreshItemExtends(string id, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _service.GetByIdAsync(id);
                if (order == null) return NotFound(new { success = false, message = "销售订单不存在" });

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessSalesOrderAsync(userId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该销售订单" });

                var result = await _service.RefreshItemExtendsAsync(id, cancellationToken);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新销售订单明细扩展失败: {Id}", id);
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
                var loaded = await _service.GetByIdAsync(order.Id) ?? order;
                var summary = await GetPermissionSummaryAsync(actorId);
                IReadOnlyDictionary<string, SellOrderItemExtend>? itemExtends = null;
                if (loaded.Items != null && loaded.Items.Count > 0)
                {
                    var ids = loaded.Items
                        .Select(i => i.Id)
                        .Where(oid => !string.IsNullOrWhiteSpace(oid))
                        .Select(oid => oid.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    itemExtends = await LoadSellOrderItemExtendsByItemIdsAsync(ids, loaded.Id);
                }

                IReadOnlyDictionary<string, bool> stockOutGate =
                    new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                if (loaded.Items != null && loaded.Items.Count > 0)
                {
                    stockOutGate = await _service.GetStockOutApplyPurchaseGateBySellLineIdsAsync(
                        loaded.Items.Select(i => i.Id));
                }

                return CreatedAtAction(nameof(GetById), new { id = loaded.Id },
                    new { success = true, data = MaskSalesOrder(loaded, summary, itemExtends, stockOutGate) });
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

        [HttpPut("{id:guid}")]
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

        [HttpDelete("{id:guid}")]
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

        [HttpPatch("{id:guid}/status")]
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

        /// <summary>
        /// 加载销售明细扩展（含备货在库可用量）。批量查询失败时逐条回退，避免前端「申请出库」门槛与弹窗数量全为 0。
        /// </summary>
        private async Task<IReadOnlyDictionary<string, SellOrderItemExtend>?> LoadSellOrderItemExtendsByItemIdsAsync(
            IReadOnlyList<string> sellOrderItemIds,
            string sellOrderIdForLog)
        {
            if (sellOrderItemIds.Count == 0)
                return null;

            try
            {
                var extRows = (await _soItemExtendRepo.FindAsync(e => sellOrderItemIds.Contains(e.Id))).ToList();
                return extRows.ToDictionary(e => e.Id, e => e, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "批量加载销售明细扩展失败，改为逐条加载: SellOrderId={SellOrderId}", sellOrderIdForLog);
                try
                {
                    var map = new Dictionary<string, SellOrderItemExtend>(StringComparer.OrdinalIgnoreCase);
                    foreach (var rawId in sellOrderItemIds)
                    {
                        if (string.IsNullOrWhiteSpace(rawId))
                            continue;
                        var id = rawId.Trim();
                        var ext = await _soItemExtendRepo.GetByIdAsync(id);
                        if (ext != null)
                            map[id] = ext;
                    }

                    return map.Count > 0 ? map : null;
                }
                catch (Exception ex2)
                {
                    _logger.LogWarning(ex2, "逐条加载销售明细扩展仍失败，已跳过: SellOrderId={SellOrderId}", sellOrderIdForLog);
                    return null;
                }
            }
        }

        private static object MaskSellOrderLine(SellOrderItemLineDto r, bool canViewCustomer, bool canViewAmount, bool mask521)
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
                SalesUserName = mask521 ? null : r.SalesUserName,
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
                r.StockOutNotifyProgressStatus,
                r.ReceiptProgressStatus,
                r.InvoiceProgressStatus,
                r.StockOutApplyPurchaseGateOk,
                r.PurchasedStockAvailableQty,
                r.PurchaseRemainingQty
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
            var mask521 = SaleSensitiveFieldMask521.ShouldMask(summary);
            var canViewCustomerInfo = !mask521 && (summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("customer.info.read") ?? false));
            var canViewSalesAmount = !mask521 && (summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("sales.amount.read") ?? false));

            return new
            {
                order.Id,
                order.SellOrderCode,
                CustomerId = canViewCustomerInfo ? order.CustomerId : null,
                CustomerName = canViewCustomerInfo ? order.CustomerName : null,
                SalesUserId = mask521 ? null : order.SalesUserId,
                SalesUserName = mask521 ? null : order.SalesUserName,
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
                productKind = order.ProductKind,
                customerContactName = order.CustomerContactName,
                invoiceInfo = order.InvoiceInfo,
                paymentTermsText = order.PaymentTermsText,
                comment = order.Comment,
                headerRemarkDisplay = SellOrderHeaderRemarkCodec.BuildDisplayComment(order),
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
                        customerSo = canViewCustomerInfo ? i.CustomerSo : null,
                        customerPn = canViewCustomerInfo ? i.CustomerPn : null,
                        customerBrand = canViewCustomerInfo ? i.CustomerBrand : null,
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
                        stockOutNotifyProgressStatus = ext == null
                            ? (short)0
                            : ext.QtyStockOutNotify <= 0m
                                ? (short)0
                                : ext.QtyStockOutNotify + 1e-9m >= i.Qty
                                    ? (short)2
                                    : (short)1,
                        stockOutProgressStatus = ext?.StockOutProgressStatus ?? (short)0,
                        receiptProgressStatus = ext?.ReceiptProgressStatus ?? (short)0,
                        invoiceProgressStatus = ext?.InvoiceProgressStatus ?? (short)0,
                        stockOutApplyPurchaseGateOk = stockOutApplyPurchaseGate != null &&
                            !string.IsNullOrWhiteSpace(i.Id) &&
                            stockOutApplyPurchaseGate.TryGetValue(i.Id.Trim(), out var gateOk) &&
                            gateOk,
                        purchasedStockAvailableQty = ext?.PurchasedStock_AvailableQty ?? 0,
                        purchaseQuoteCost = canViewSalesAmount ? ext?.QuoteCost : null,
                        purchaseQuoteCurrency = ext != null ? ext.QuoteCurrency : (short?)null
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
