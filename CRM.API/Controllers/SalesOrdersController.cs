using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Dtos;
using CRM.Core.Models.Quote;
using CRM.Core.Models.Sales;
using CRM.Core.Services;
using CRM.Core.Utilities;
using CRM.API.Authorization;
using CRM.API.Services;
using CRM.API.Services.Interfaces;
using CRM.Infrastructure.Data;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

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
        private readonly IOperationLogQueryService _operationLogQuery;
        private readonly ILogOperationAppendService _logOperationAppend;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SalesOrdersController> _logger;

        public SalesOrdersController(
            ISalesOrderService service,
            ISalesOrderListQuery salesOrderListQuery,
            ISalesOrderJourneyService journeyService,
            IDataPermissionService dataPermissionService,
            IRbacService rbacService,
            IRepository<SellOrderItemExtend> soItemExtendRepo,
            IOperationLogQueryService operationLogQuery,
            ILogOperationAppendService logOperationAppend,
            ApplicationDbContext db,
            ILogger<SalesOrdersController> logger)
        {
            _service = service;
            _salesOrderListQuery = salesOrderListQuery;
            _journeyService = journeyService;
            _dataPermissionService = dataPermissionService;
            _rbacService = rbacService;
            _soItemExtendRepo = soItemExtendRepo;
            _operationLogQuery = operationLogQuery;
            _logOperationAppend = logOperationAppend;
            _db = db;
            _logger = logger;
        }

        public sealed class SalesOrderBatchLogExportBody
        {
            public int ExportedCount { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? keyword,
            [FromQuery] string? code,
            [FromQuery] string? customer,
            [FromQuery] string? salesUserName,
            [FromQuery] string? comment,
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
                var canViewCustomerInfo = CanViewSalesOrderCustomerInfo(summary, mask521);

                var request = new SalesOrderQueryRequest
                {
                    Keyword = keyword,
                    SellOrderCodeFilter = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
                    CustomerNameFilter = canViewCustomerInfo && !string.IsNullOrWhiteSpace(customer) ? customer.Trim() : null,
                    SalesUserNameFilter = !mask521 && !string.IsNullOrWhiteSpace(salesUserName) ? salesUserName.Trim() : null,
                    CommentFilter = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                    Status = status,
                    StartDate = DateTime.TryParse(startDate, out var start) ? start : null,
                    EndDate = DateTime.TryParse(endDate, out var end) ? end : null,
                    Page = page,
                    PageSize = pageSize,
                    CurrentUserId = userId
                };
                var result = await _service.GetPagedAsync(request);
                var assistorNameMap = await BuildUserDisplayNameMapAsync(result.Items.SelectMany(x => new[] { x.Assistor, x.CreateByUserId }));
                var customerMap = await LoadCustomerMapForSellOrdersAsync(result.Items, cancellationToken);
                var items = result.Items
                    .Select(x =>
                    {
                        CustomerInfo? customer = null;
                        var cid = x.CustomerId?.Trim();
                        if (!string.IsNullOrEmpty(cid))
                            customer = customerMap.GetValueOrDefault(cid);
                        return MaskSalesOrder(
                            x,
                            summary,
                            assistorUserName: ResolveAssistorDisplayName(x.Assistor, assistorNameMap),
                            createUserName: ResolveAssistorDisplayName(x.CreateByUserId, assistorNameMap),
                            customer: customer);
                    })
                    .ToList();
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
            [FromQuery] string? salesUserId,
            [FromQuery] string? customerId,
            [FromQuery] string? sellOrderCode,
            [FromQuery] string? pn,
            [FromQuery] string? customerSo,
            [FromQuery] string? customerPn,
            [FromQuery] string? transactionCurrency,
            [FromQuery] bool stockOutPending = false,
            [FromQuery] bool invoicePending = false,
            [FromQuery] short? purchaseProgressStatus = null,
            [FromQuery] short? stockInProgressStatus = null,
            [FromQuery] short? stockOutNotifyProgressStatus = null,
            [FromQuery] short? stockOutProgressStatus = null,
            [FromQuery] short? receiptProgressStatus = null,
            [FromQuery] short? invoiceProgressStatus = null,
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
                    SalesUserId = canViewSalesUser && !string.IsNullOrWhiteSpace(salesUserId) ? salesUserId.Trim() : null,
                    CustomerId = canViewCustomer && !string.IsNullOrWhiteSpace(customerId) ? customerId.Trim() : null,
                    SellOrderCode = sellOrderCode,
                    Pn = pn,
                    CustomerSo = canViewCustomer && !string.IsNullOrWhiteSpace(customerSo) ? customerSo.Trim() : null,
                    CustomerPn = canViewCustomer && !string.IsNullOrWhiteSpace(customerPn) ? customerPn.Trim() : null,
                    TransactionCurrency = transactionCurrency,
                    StockOutPending = stockOutPending,
                    InvoicePending = invoicePending,
                    PurchaseProgressStatus = purchaseProgressStatus,
                    StockInProgressStatus = stockInProgressStatus,
                    StockOutNotifyProgressStatus = stockOutNotifyProgressStatus,
                    StockOutProgressStatus = stockOutProgressStatus,
                    ReceiptProgressStatus = receiptProgressStatus,
                    InvoiceProgressStatus = invoiceProgressStatus,
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

                IReadOnlyDictionary<string, StockOutApplyPurchaseGateDetailDto> stockOutGate =
                    new Dictionary<string, StockOutApplyPurchaseGateDetailDto>(StringComparer.OrdinalIgnoreCase);
                if (order.Items != null && order.Items.Count > 0)
                {
                    stockOutGate = await _service.GetStockOutApplyPurchaseGateDetailsBySellLineIdsAsync(
                        order.Items.Select(i => i.Id));
                }

                var companyProfile = await CompanyProfileBundleLoader.LoadAsync(_db, _logger, cancellationToken);
                CompanyProfileBundleLoader.StripSmtpEmail(companyProfile);
                var reportUserMap = await BuildUserDisplayNameMapAsync(new[] { order.Assistor, order.CreateByUserId });
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        order = MaskSalesOrder(
                            order,
                            summary,
                            itemExtends,
                            stockOutGate,
                            ResolveAssistorDisplayName(order.Assistor, reportUserMap),
                            ResolveAssistorDisplayName(order.CreateByUserId, reportUserMap)),
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

        /// <summary>销售订单详情页：底部页签用下游列表（需求明细/采购申请/采购订单明细/入库/库存/出库通知/出库/收款/销项发票）。</summary>
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
                var mask511 = PurchaseSensitiveFieldMask511.ShouldMask(summary);

                var itemIds = (order.Items ?? new List<SellOrderItem>())
                    .Select(i => i.Id)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var data = await BuildSalesOrderDetailTabAggregatesPayloadAsync(id, itemIds, sellOrderItemIdScope: null, mask521, mask511);
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
                var mask511 = PurchaseSensitiveFieldMask511.ShouldMask(summary);

                var data = await BuildSalesOrderDetailTabAggregatesPayloadAsync(id, orderLineIds, sellOrderItemIdScope: lineId, mask521, mask511);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取销售订单明细页签数据失败: {OrderId} {ItemId}", id, sellOrderItemId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <param name="sellOrderItemIdScope">非 null 时：采购申请、采购订单明细、出库通知、收款明细仅保留该销售明细；在库/出库/销项发票链仅使用该明细。</param>
        private async Task<object> BuildSalesOrderDetailTabAggregatesPayloadAsync(
            string orderId,
            IReadOnlyList<string> allOrderLineIds,
            string? sellOrderItemIdScope,
            bool mask521,
            bool mask511)
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
                var rawBound = await _db.StockItems.AsNoTracking()
                    .Where(s => s.SellOrderItemId != null && itemIds.Contains(s.SellOrderItemId!))
                    .OrderByDescending(s => s.CreateTime)
                    .Select(s => new SellOrderTabStockItemRow
                    {
                        Id = s.Id,
                        StockItemCode = s.StockItemCode,
                        StockAggregateId = s.StockAggregateId,
                        RegionType = s.RegionType,
                        StockType = s.StockType,
                        PurchasePn = s.PurchasePn,
                        PurchaseBrand = s.PurchaseBrand,
                        StockOutStatus = s.StockOutStatus,
                        QtyInbound = s.QtyInbound,
                        QtyStockOut = s.QtyStockOut,
                        QtyRepertory = s.QtyRepertory,
                        QtyRepertoryAvailable = s.QtyRepertoryAvailable,
                        SellOrderItemId = s.SellOrderItemId,
                        SellOrderItemCode = s.SellOrderItemCode,
                        WarehouseId = s.WarehouseId,
                        StockInId = s.StockInId,
                        PurchaseOrderItemCode = s.PurchaseOrderItemCode,
                        BatchNo = s.BatchNo,
                        LocationId = s.LocationId,
                        CreateTime = s.CreateTime,
                        IsStockingPoolMatch = false
                    })
                    .ToListAsync();

                var pnBrandKeys = await BuildSellLinePnBrandKeysAsync(itemIds);
                var boundIds = new HashSet<string>(
                    rawBound.Select(x => x.Id.Trim()),
                    StringComparer.OrdinalIgnoreCase);
                var rawStocking = new List<SellOrderTabStockItemRow>();
                if (pnBrandKeys.Count > 0)
                {
                    var stockingCandidates = await _db.StockItems.AsNoTracking()
                        .Where(s => s.StockType == StockInventoryTypeCodes.Stocking)
                        .OrderByDescending(s => s.CreateTime)
                        .Select(s => new SellOrderTabStockItemRow
                        {
                            Id = s.Id,
                            StockItemCode = s.StockItemCode,
                            StockAggregateId = s.StockAggregateId,
                            RegionType = s.RegionType,
                            StockType = s.StockType,
                            PurchasePn = s.PurchasePn,
                            PurchaseBrand = s.PurchaseBrand,
                            StockOutStatus = s.StockOutStatus,
                            QtyInbound = s.QtyInbound,
                            QtyStockOut = s.QtyStockOut,
                            QtyRepertory = s.QtyRepertory,
                            QtyRepertoryAvailable = s.QtyRepertoryAvailable,
                            SellOrderItemId = s.SellOrderItemId,
                            SellOrderItemCode = s.SellOrderItemCode,
                            WarehouseId = s.WarehouseId,
                            StockInId = s.StockInId,
                            PurchaseOrderItemCode = s.PurchaseOrderItemCode,
                            BatchNo = s.BatchNo,
                            LocationId = s.LocationId,
                            CreateTime = s.CreateTime,
                            IsStockingPoolMatch = true
                        })
                        .ToListAsync();
                    foreach (var s in stockingCandidates)
                    {
                        if (boundIds.Contains(s.Id.Trim()))
                            continue;
                        var key = NormPnBrandKey(s.PurchasePn, s.PurchaseBrand);
                        if (string.IsNullOrEmpty(key) || !pnBrandKeys.Contains(key))
                            continue;
                        rawStocking.Add(s);
                    }
                }

                var rawStockItems = rawBound.Concat(rawStocking).OrderByDescending(s => s.CreateTime).ToList();

                var stockInIds = rawStockItems
                    .Select(x => x.StockInId)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                Dictionary<string, (string? StockInCode, DateTime? StockInDate)> stockInMap;
                if (stockInIds.Count == 0)
                {
                    stockInMap = new Dictionary<string, (string? StockInCode, DateTime? StockInDate)>(StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    var stockInRowsForMap = await _db.StockIns.AsNoTracking()
                        .Where(x => stockInIds.Contains(x.Id))
                        .Select(x => new { x.Id, x.StockInCode, x.StockInDate })
                        .ToListAsync();
                    stockInMap = stockInRowsForMap.ToDictionary(
                        x => x.Id,
                        x => (
                            string.IsNullOrWhiteSpace(x.StockInCode) ? null : x.StockInCode.Trim(),
                            (DateTime?)x.StockInDate),
                        StringComparer.OrdinalIgnoreCase);
                }

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
                    .Select(s => (object)MapSellOrderTabStockItemRow(s, stockInMap, warehouseNameMap))
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

            List<object> purchaseOrderItemRows;
            if (itemIds.Count == 0)
            {
                purchaseOrderItemRows = new List<object>();
            }
            else
            {
                var rawPoRows = await (
                        from poi in _db.PurchaseOrderItems.AsNoTracking()
                        join po in _db.PurchaseOrders.AsNoTracking() on poi.PurchaseOrderId equals po.Id
                        where poi.SellOrderItemId != null && itemIds.Contains(poi.SellOrderItemId!)
                        orderby poi.CreateTime descending
                        select new
                        {
                            id = poi.Id,
                            purchaseOrderId = po.Id,
                            purchaseOrderCode = po.PurchaseOrderCode,
                            purchaseOrderItemCode = poi.PurchaseOrderItemCode,
                            poStatus = po.Status,
                            sellOrderItemId = poi.SellOrderItemId,
                            poi.PN,
                            poi.Brand,
                            poi.Qty,
                            cost = poi.Cost,
                            currency = poi.Currency,
                            itemStatus = poi.Status,
                            vendorName = po.VendorName,
                            purchaseUserName = po.PurchaseUserName,
                            poi.CreateTime
                        })
                    .ToListAsync();

                purchaseOrderItemRows = rawPoRows
                    .Select(r => (object)new
                    {
                        r.id,
                        r.purchaseOrderId,
                        r.purchaseOrderCode,
                        r.purchaseOrderItemCode,
                        r.poStatus,
                        r.sellOrderItemId,
                        r.PN,
                        r.Brand,
                        r.Qty,
                        cost = mask511 ? 0m : r.cost,
                        currency = mask511 ? (short?)null : r.currency,
                        r.itemStatus,
                        vendorName = mask511 ? null : r.vendorName,
                        r.purchaseUserName,
                        r.CreateTime
                    })
                    .ToList();
            }

            List<object> qcImageRows;
            if (itemIds.Count == 0)
            {
                qcImageRows = new List<object>();
            }
            else
            {
                var notifyIds = await _db.StockInNotifies.AsNoTracking()
                    .Where(n => n.SellOrderItemId != null && itemIds.Contains(n.SellOrderItemId!))
                    .Select(n => n.Id)
                    .ToListAsync();

                if (notifyIds.Count == 0)
                {
                    qcImageRows = new List<object>();
                }
                else
                {
                    var qcList = await _db.QCInfos.AsNoTracking()
                        .Where(q => notifyIds.Contains(q.StockInNotifyId))
                        .OrderByDescending(q => q.CreateTime)
                        .Select(q => new { q.Id, q.QcCode, q.StockInNotifyCode })
                        .ToListAsync();

                    var qcIds = qcList.Select(q => q.Id).ToList();
                    var qcMeta = qcList.ToDictionary(q => q.Id, StringComparer.OrdinalIgnoreCase);

                    var imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ".jpg", ".jpeg", ".png", ".gif", ".webp"
                    };

                    var docs = await _db.UploadDocuments.AsNoTracking()
                        .Where(d => d.BizType == "QC" && qcIds.Contains(d.BizId))
                        .OrderBy(d => d.CreateTime)
                        .Select(d => new
                        {
                            documentId = d.Id,
                            qcId = d.BizId,
                            d.OriginalFileName,
                            d.MimeType,
                            d.FileExtension,
                            d.CreateTime
                        })
                        .ToListAsync();

                    qcImageRows = docs
                        .Where(d =>
                        {
                            var t = (d.MimeType ?? string.Empty).Trim().ToLowerInvariant();
                            var e = (d.FileExtension ?? string.Empty).Trim().ToLowerInvariant();
                            return t.StartsWith("image/", StringComparison.Ordinal)
                                || imageExtensions.Contains(e);
                        })
                        .Select(d =>
                        {
                            qcMeta.TryGetValue(d.qcId, out var meta);
                            return (object)new
                            {
                                d.documentId,
                                d.qcId,
                                qcCode = meta?.QcCode,
                                stockInNotifyCode = meta?.StockInNotifyCode,
                                d.OriginalFileName,
                                mimeType = d.MimeType,
                                fileExtension = d.FileExtension,
                                d.CreateTime
                            };
                        })
                        .ToList();
                }
            }

            var rfqItemRows = await BuildRfqItemTabRowsAsync(itemIds, mask521);
            var quoteRows = await BuildQuoteTabRowsAsync(itemIds, mask521, mask511);

            return new
            {
                rfqItems = rfqItemRows,
                quotes = quoteRows,
                purchaseRequisitions = prRows,
                purchaseOrderItems = purchaseOrderItemRows,
                stockIns = stockInRows,
                stockItems = stockItemRows,
                stockOutRequests = outReqRows,
                stockOuts = stockOutRows,
                receipts = receiptRows,
                sellInvoices = sellInvRows,
                qcImages = qcImageRows
            };
        }

        /// <summary>销售明细 → 报价单 → 需求明细行（与创建销售单/采购申请链路一致）。</summary>
        private async Task<List<object>> BuildRfqItemTabRowsAsync(IReadOnlyList<string> itemIds, bool mask521)
        {
            if (itemIds.Count == 0)
                return new List<object>();

            var lineQuotes = await _db.SellOrderItems.AsNoTracking()
                .Where(i => itemIds.Contains(i.Id) && i.QuoteId != null && i.QuoteId != "")
                .Select(i => new { SellOrderItemId = i.Id, SellOrderItemCode = i.SellOrderItemCode, QuoteId = i.QuoteId! })
                .ToListAsync();

            if (lineQuotes.Count == 0)
                return new List<object>();

            var quoteIds = lineQuotes
                .Select(x => x.QuoteId.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var quoteRfqLinks = await _db.Quotes.AsNoTracking()
                .Where(q => quoteIds.Contains(q.Id) && q.RFQItemId != null && q.RFQItemId != "")
                .Select(q => new { QuoteId = q.Id, RfqItemId = q.RFQItemId!, QuoteCode = q.QuoteCode })
                .ToListAsync();

            if (quoteRfqLinks.Count == 0)
                return new List<object>();

            var rfqItemIds = quoteRfqLinks
                .Select(x => x.RfqItemId.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var rfqRows = await (
                from item in _db.RFQItems.AsNoTracking()
                join rfq in _db.RFQs.AsNoTracking() on item.RfqId equals rfq.Id
                join cust in _db.Customers.AsNoTracking() on rfq.CustomerId equals cust.Id into custGroup
                from cust in custGroup.DefaultIfEmpty()
                join su in _db.Users.AsNoTracking() on rfq.SalesUserId equals su.Id into suGroup
                from su in suGroup.DefaultIfEmpty()
                where rfqItemIds.Contains(item.Id) && !item.IsDeleted && !rfq.IsDeleted
                orderby rfq.CreateTime descending, item.LineNo
                select new { item, rfq, cust, su }
            ).ToListAsync();

            var puIds = rfqRows
                .SelectMany(x => new[] { x.item.AssignedPurchaserUserId1, x.item.AssignedPurchaserUserId2 })
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var puUsers = puIds.Count == 0
                ? new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase)
                : await _db.Users.AsNoTracking()
                    .Where(u => puIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u, StringComparer.OrdinalIgnoreCase);

            var quoteToLine = lineQuotes.ToDictionary(
                x => x.QuoteId.Trim(),
                x => x,
                StringComparer.OrdinalIgnoreCase);

            var quotedRfqItemIds = quoteRfqLinks
                .Select(x => x.RfqItemId.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var result = new List<object>(rfqRows.Count);
            foreach (var row in rfqRows)
            {
                var lineStatus = row.item.Status;
                if (lineStatus == 0 && quotedRfqItemIds.Contains(row.item.Id.Trim()))
                    lineStatus = 1;

                var customerName = row.cust != null
                    ? (row.cust.OfficialName ?? row.cust.NickName)
                    : null;
                if (mask521)
                    customerName = null;

                var salesUserName = mask521 ? null : EntityLookupService.FormatUserLoginName(row.su);

                var link = quoteRfqLinks.FirstOrDefault(q =>
                    string.Equals(q.RfqItemId.Trim(), row.item.Id.Trim(), StringComparison.OrdinalIgnoreCase));
                string? sellOrderItemId = null;
                string? sellOrderItemCode = null;
                string? quoteCode = null;
                if (link != null)
                {
                    quoteCode = link.QuoteCode;
                    if (quoteToLine.TryGetValue(link.QuoteId.Trim(), out var soLine))
                    {
                        sellOrderItemId = soLine.SellOrderItemId;
                        sellOrderItemCode = soLine.SellOrderItemCode;
                    }
                }

                puUsers.TryGetValue(row.item.AssignedPurchaserUserId1 ?? "", out var pu1);
                puUsers.TryGetValue(row.item.AssignedPurchaserUserId2 ?? "", out var pu2);

                result.Add(new
                {
                    id = row.item.Id,
                    rfqId = row.item.RfqId,
                    rfqCode = row.rfq.RfqCode,
                    lineNo = row.item.LineNo,
                    mpn = row.item.Mpn,
                    customerMpn = row.item.CustomerMpn,
                    customerBrand = row.item.CustomerBrand,
                    brand = row.item.Brand,
                    quantity = row.item.Quantity,
                    status = lineStatus,
                    productionDate = row.item.ProductionDate,
                    customerName,
                    salesUserName,
                    sellOrderItemId,
                    sellOrderItemCode,
                    quoteCode,
                    assignedPurchaserName1 = EntityLookupService.FormatUserLoginName(pu1),
                    assignedPurchaserName2 = EntityLookupService.FormatUserLoginName(pu2),
                    rfqCreateTime = row.rfq.CreateTime,
                    createTime = row.item.CreateTime
                });
            }

            return result;
        }

        /// <summary>销售明细 <c>quote_id</c> 关联的报价主表（一条销售明细通常对应一张报价单）。</summary>
        private async Task<List<object>> BuildQuoteTabRowsAsync(
            IReadOnlyList<string> itemIds,
            bool mask521,
            bool mask511)
        {
            if (itemIds.Count == 0)
                return new List<object>();

            var lineQuotes = await _db.SellOrderItems.AsNoTracking()
                .Where(i => itemIds.Contains(i.Id) && i.QuoteId != null && i.QuoteId != "" && !i.IsDeleted)
                .Select(i => new { SellOrderItemId = i.Id, SellOrderItemCode = i.SellOrderItemCode, QuoteId = i.QuoteId! })
                .ToListAsync();

            if (lineQuotes.Count == 0)
                return new List<object>();

            var quoteIds = lineQuotes
                .Select(x => x.QuoteId.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var quotes = await _db.Quotes.AsNoTracking()
                .Where(q => quoteIds.Contains(q.Id) && !q.IsDeleted)
                .OrderByDescending(q => q.CreateTime)
                .ToListAsync();

            if (quotes.Count == 0)
                return new List<object>();

            var userIds = quotes
                .SelectMany(q => new[] { q.SalesUserId, q.PurchaseUserId })
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var users = userIds.Count == 0
                ? new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase)
                : await _db.Users.AsNoTracking()
                    .Where(u => userIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u, StringComparer.OrdinalIgnoreCase);

            var rfqIds = quotes
                .Select(q => q.RFQId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var rfqCodes = rfqIds.Count == 0
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : await _db.RFQs.AsNoTracking()
                    .Where(r => rfqIds.Contains(r.Id))
                    .ToDictionaryAsync(r => r.Id, r => r.RfqCode, StringComparer.OrdinalIgnoreCase);

            var quoteItemEntities = await _db.QuoteItems.AsNoTracking()
                .Where(qi => quoteIds.Contains(qi.QuoteId) && !qi.IsDeleted)
                .OrderBy(qi => qi.CreateTime)
                .ToListAsync();

            var itemsByQuote = quoteItemEntities
                .GroupBy(qi => qi.QuoteId.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var brandByQuote = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var qi in quoteItemEntities)
            {
                var qid = qi.QuoteId.Trim();
                if (!brandByQuote.ContainsKey(qid) && !string.IsNullOrWhiteSpace(qi.Brand))
                    brandByQuote[qid] = qi.Brand.Trim();
            }

            var lineByQuote = lineQuotes
                .GroupBy(x => x.QuoteId.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var result = new List<object>(quotes.Count);
            foreach (var q in quotes)
            {
                var qid = q.Id.Trim();
                lineByQuote.TryGetValue(qid, out var soLine);

                string? rfqCode = null;
                if (!string.IsNullOrWhiteSpace(q.RFQId)
                    && rfqCodes.TryGetValue(q.RFQId.Trim(), out var code))
                    rfqCode = code;

                brandByQuote.TryGetValue(qid, out var brand);

                users.TryGetValue(q.SalesUserId ?? "", out var su);
                users.TryGetValue(q.PurchaseUserId ?? "", out var pu);

                itemsByQuote.TryGetValue(qid, out var quoteItems);
                var itemRows = (quoteItems ?? new List<QuoteItem>())
                    .Select(qi => (object)new
                    {
                        quantity = qi.Quantity,
                        unitPrice = qi.UnitPrice,
                        currency = qi.Currency,
                        vendorName = mask511 || string.IsNullOrWhiteSpace(qi.VendorName)
                            ? null
                            : qi.VendorName.Trim()
                    })
                    .ToList();

                result.Add(new
                {
                    id = q.Id,
                    quoteCode = q.QuoteCode,
                    mpn = q.Mpn,
                    brand,
                    status = q.Status,
                    rfqCode,
                    salesUserName = mask521 ? null : EntityLookupService.FormatUserLoginName(su),
                    purchaseUserName = EntityLookupService.FormatUserLoginName(pu),
                    quoteDate = q.QuoteDate,
                    sellOrderItemId = soLine?.SellOrderItemId,
                    sellOrderItemCode = soLine?.SellOrderItemCode,
                    items = itemRows,
                    createTime = q.CreateTime
                });
            }

            return result;
        }

        /// <summary>销售订单主表字段变更日志（log_change_fldval）。</summary>
        [HttpGet("{id:guid}/change-logs")]
        public async Task<IActionResult> GetChangeLogs(string id)
        {
            try
            {
                var order = await _service.GetByIdAsync(id);
                if (order == null) return NotFound(new { success = false, message = "销售订单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessSalesOrderAsync(userId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该销售订单" });
                var logs = await _service.GetFieldChangeLogsAsync(id);
                return Ok(new { success = true, data = logs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取销售订单变更日志失败: {Id}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>销售订单详情「出库批次」面板导出记录（仅 SO 页发起的导出）。</summary>
        [HttpGet("{id:guid}/batch-export-logs")]
        public async Task<IActionResult> GetBatchExportLogs(
            string id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var order = await _service.GetByIdAsync(id);
                if (order == null) return NotFound(new { success = false, message = "销售订单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessSalesOrderAsync(userId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该销售订单" });

                var data = await _operationLogQuery.QueryAsync(new OperationLogQuery
                {
                    BizType = BusinessLogTypes.SalesOrder,
                    RecordId = id.Trim(),
                    ActionType = SalesOrderBatchExportActionTypes.Export,
                    Page = page,
                    PageSize = pageSize
                }, cancellationToken);

                return Ok(new { success = true, data, message = "ok" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取销售订单出库批次导出记录失败 SalesOrderId={Id}", id);
                return StatusCode(500, new { success = false, message = "获取导出记录失败" });
            }
        }

        /// <summary>记录销售订单详情页导出出库批次 CSV 的操作日志。</summary>
        [HttpPost("{id:guid}/batch-log-export")]
        public async Task<IActionResult> LogBatchExport(
            string id,
            [FromBody] SalesOrderBatchLogExportBody body,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var order = await _service.GetByIdAsync(id);
                if (order == null) return NotFound(new { success = false, message = "销售订单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessSalesOrderAsync(userId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该销售订单" });

                var count = Math.Max(0, body?.ExportedCount ?? 0);
                var code = (order.SellOrderCode ?? string.Empty).Trim();
                var desc = string.IsNullOrEmpty(code)
                    ? $"导出销售订单出库批次 {count} 条"
                    : $"导出销售订单 {code} 出库批次 {count} 条";
                var extraInfo = JsonSerializer.Serialize(new { exportedCount = count });

                await _logOperationAppend.AppendAsync(
                    BusinessLogTypes.SalesOrder,
                    order.Id,
                    code,
                    SalesOrderBatchExportActionTypes.Export,
                    userId,
                    User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value,
                    desc,
                    null,
                    extraInfo,
                    cancellationToken);

                return Ok(new { success = true, data = (object?)null, message = "已记录导出日志" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录销售订单出库批次导出日志失败 SalesOrderId={Id}", id);
                return StatusCode(500, new { success = false, message = "记录导出日志失败" });
            }
        }

        /// <summary>已软删除的销售订单明细行。</summary>
        [HttpGet("{id:guid}/deleted-items")]
        public async Task<IActionResult> GetDeletedItems(string id)
        {
            try
            {
                var order = await _service.GetByIdAsync(id);
                if (order == null) return NotFound(new { success = false, message = "销售订单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessSalesOrderAsync(userId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该销售订单" });
                var items = await _service.GetDeletedOrderItemsAsync(id);
                return Ok(new { success = true, data = items });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取销售订单删除明细失败: {Id}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
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

                IReadOnlyDictionary<string, StockOutApplyPurchaseGateDetailDto> stockOutGate =
                    new Dictionary<string, StockOutApplyPurchaseGateDetailDto>(StringComparer.OrdinalIgnoreCase);
                if (order.Items != null && order.Items.Count > 0)
                {
                    stockOutGate = await _service.GetStockOutApplyPurchaseGateDetailsBySellLineIdsAsync(
                        order.Items.Select(i => i.Id));
                }

                var userDisplayMap = await BuildUserDisplayNameMapAsync(new[] { order.Assistor, order.CreateByUserId });
                return Ok(new
                {
                    success = true,
                    data = MaskSalesOrder(
                        order,
                        summary,
                        itemExtends,
                        stockOutGate,
                        ResolveAssistorDisplayName(order.Assistor, userDisplayMap),
                        ResolveAssistorDisplayName(order.CreateByUserId, userDisplayMap))
                });
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
                var assistorNameMap = await BuildUserDisplayNameMapAsync(orders.SelectMany(x => new[] { x.Assistor, x.CreateByUserId }));
                return Ok(new
                {
                    success = true,
                    data = orders
                        .Select(x => MaskSalesOrder(
                            x,
                            summary,
                            assistorUserName: ResolveAssistorDisplayName(x.Assistor, assistorNameMap),
                            createUserName: ResolveAssistorDisplayName(x.CreateByUserId, assistorNameMap)))
                        .ToList()
                });
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

                IReadOnlyDictionary<string, StockOutApplyPurchaseGateDetailDto> stockOutGate =
                    new Dictionary<string, StockOutApplyPurchaseGateDetailDto>(StringComparer.OrdinalIgnoreCase);
                if (loaded.Items != null && loaded.Items.Count > 0)
                {
                    stockOutGate = await _service.GetStockOutApplyPurchaseGateDetailsBySellLineIdsAsync(
                        loaded.Items.Select(i => i.Id));
                }

                var createUserMap = await BuildUserDisplayNameMapAsync(new[] { loaded.Assistor, loaded.CreateByUserId });
                return CreatedAtAction(nameof(GetById), new { id = loaded.Id },
                    new
                    {
                        success = true,
                        data = MaskSalesOrder(
                            loaded,
                            summary,
                            itemExtends,
                            stockOutGate,
                            ResolveAssistorDisplayName(loaded.Assistor, createUserMap),
                            ResolveAssistorDisplayName(loaded.CreateByUserId, createUserMap))
                    });
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
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.DeleteAsync(id, actorId);
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
                CustomerSo = canViewCustomer ? r.CustomerSo : null,
                CustomerPn = canViewCustomer ? r.CustomerPn : null,
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
                stockOutApplyPurchaseGateDetail = MaskStockOutApplyPurchaseGateDetail(r.StockOutApplyPurchaseGateDetail),
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

        private async Task<Dictionary<string, string>> BuildUserDisplayNameMapAsync(IEnumerable<string?> userIds)
        {
            var ids = userIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (ids.Count == 0)
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var users = await _db.Users.AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName, u.RealName })
                .ToListAsync();
            return users.ToDictionary(
                u => u.Id,
                u => EntityLookupService.FormatUserLoginName(new User { UserName = u.UserName })
                    ?? u.Id,
                StringComparer.OrdinalIgnoreCase);
        }

        private static bool SummaryHasPermission(UserPermissionSummaryDto? summary, string code)
        {
            if (summary?.PermissionCodes == null) return false;
            return summary.PermissionCodes.Any(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>与前端销售订单列表 <c>canViewCustomerInfo</c> 及采购订单供应商列口径对齐。</summary>
        private static bool CanViewSalesOrderCustomerInfo(UserPermissionSummaryDto? summary, bool mask521)
        {
            if (mask521) return false;
            if (summary?.IsSysAdmin == true) return true;
            return SummaryHasPermission(summary, "customer.info.read")
                || SummaryHasPermission(summary, "sales-order.read")
                || SummaryHasPermission(summary, "sales-order.write");
        }

        private async Task<Dictionary<string, CustomerInfo>> LoadCustomerMapForSellOrdersAsync(
            IEnumerable<SellOrder> orders,
            CancellationToken cancellationToken)
        {
            var ids = orders
                .Select(o => o.CustomerId)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (ids.Count == 0)
                return new Dictionary<string, CustomerInfo>(StringComparer.OrdinalIgnoreCase);

            var rows = await _db.Customers.AsNoTracking()
                .Where(c => ids.Contains(c.Id))
                .ToListAsync(cancellationToken);
            return rows.ToDictionary(c => c.Id, c => c, StringComparer.OrdinalIgnoreCase);
        }

        private static string? ResolveSellOrderCustomerNameZh(CustomerInfo? customer, SellOrder order)
        {
            if (customer != null)
            {
                var zh = string.IsNullOrWhiteSpace(customer.OfficialName) ? customer.CustomerName : customer.OfficialName;
                if (!string.IsNullOrWhiteSpace(zh))
                    return zh.Trim();
            }
            return string.IsNullOrWhiteSpace(order.CustomerName) ? null : order.CustomerName.Trim();
        }

        private static string? ResolveSellOrderCustomerEnglishName(CustomerInfo? customer, SellOrder order)
        {
            if (customer != null && !string.IsNullOrWhiteSpace(customer.EnglishOfficialName))
                return customer.EnglishOfficialName.Trim();
            return string.IsNullOrWhiteSpace(order.CustomerEnglishName) ? null : order.CustomerEnglishName!.Trim();
        }

        private static string? ResolveSellOrderCustomerCode(CustomerInfo? customer, SellOrder order)
        {
            if (customer != null && !string.IsNullOrWhiteSpace(customer.CustomerCode))
                return customer.CustomerCode.Trim();
            return string.IsNullOrWhiteSpace(order.CustomerCode) ? null : order.CustomerCode!.Trim();
        }

        private static string? ResolveAssistorDisplayName(
            string? assistorUserId,
            IReadOnlyDictionary<string, string> nameMap)
        {
            var id = assistorUserId?.Trim();
            if (string.IsNullOrEmpty(id))
                return null;
            return nameMap.TryGetValue(id, out var name) ? name : null;
        }

        private static object? MaskStockOutApplyPurchaseGateDetail(StockOutApplyPurchaseGateDetailDto? detail)
        {
            if (detail == null)
                return null;
            return new
            {
                ok = detail.Ok,
                hasPoItems = detail.HasPoItems,
                blockingPurchaseOrders = detail.BlockingPurchaseOrders.Select(po => new
                {
                    purchaseOrderId = po.PurchaseOrderId,
                    orderCode = po.OrderCode,
                    status = po.Status,
                    missing = po.Missing
                }).ToList()
            };
        }

        private object MaskSalesOrder(CRM.Core.Models.Sales.SellOrder order, UserPermissionSummaryDto? summary,
            IReadOnlyDictionary<string, SellOrderItemExtend>? itemExtends = null,
            IReadOnlyDictionary<string, StockOutApplyPurchaseGateDetailDto>? stockOutApplyPurchaseGateDetails = null,
            string? assistorUserName = null,
            string? createUserName = null,
            CustomerInfo? customer = null)
        {
            var mask521 = SaleSensitiveFieldMask521.ShouldMask(summary);
            var canViewCustomerInfo = CanViewSalesOrderCustomerInfo(summary, mask521);
            var canViewSalesAmount = !mask521 && (summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("sales.amount.read") ?? false));

            return new
            {
                order.Id,
                order.SellOrderCode,
                CustomerId = canViewCustomerInfo ? order.CustomerId : null,
                CustomerName = canViewCustomerInfo ? ResolveSellOrderCustomerNameZh(customer, order) : null,
                CustomerEnglishName = canViewCustomerInfo ? ResolveSellOrderCustomerEnglishName(customer, order) : null,
                CustomerCode = canViewCustomerInfo ? ResolveSellOrderCustomerCode(customer, order) : null,
                SalesUserId = mask521 ? null : order.SalesUserId,
                SalesUserName = mask521 ? null : order.SalesUserName,
                order.Assistor,
                AssistorUserName = assistorUserName,
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
                createUserName = createUserName,
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
                        stockOutApplyPurchaseGateOk = stockOutApplyPurchaseGateDetails != null &&
                            !string.IsNullOrWhiteSpace(i.Id) &&
                            stockOutApplyPurchaseGateDetails.TryGetValue(i.Id.Trim(), out var gateDetail) &&
                            gateDetail.Ok,
                        stockOutApplyPurchaseGateDetail = stockOutApplyPurchaseGateDetails != null &&
                            !string.IsNullOrWhiteSpace(i.Id) &&
                            stockOutApplyPurchaseGateDetails.TryGetValue(i.Id.Trim(), out gateDetail)
                            ? MaskStockOutApplyPurchaseGateDetail(gateDetail)
                            : null,
                        purchasedStockAvailableQty = ext?.PurchasedStock_AvailableQty ?? 0,
                        purchaseQuoteCost = canViewSalesAmount ? ext?.QuoteCost : null,
                        purchaseQuoteCurrency = ext != null ? ext.QuoteCurrency : (short?)null
                    };
                }).ToList()
            };
        }

        private async Task<HashSet<string>> BuildSellLinePnBrandKeysAsync(IReadOnlyList<string> sellOrderItemIds)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (sellOrderItemIds.Count == 0)
                return keys;

            var lines = await _db.SellOrderItems.AsNoTracking()
                .Where(i => sellOrderItemIds.Contains(i.Id))
                .Select(i => new { i.PN, i.Brand })
                .ToListAsync();
            foreach (var line in lines)
            {
                var key = NormPnBrandKey(line.PN, line.Brand);
                if (!string.IsNullOrEmpty(key))
                    keys.Add(key);
            }

            return keys;
        }

        private static string NormPnBrandKey(string? pn, string? brand)
        {
            var p = string.IsNullOrWhiteSpace(pn) ? string.Empty : pn.Trim();
            var b = string.IsNullOrWhiteSpace(brand) ? string.Empty : brand.Trim();
            if (string.IsNullOrEmpty(p) || string.IsNullOrEmpty(b))
                return string.Empty;
            return $"{p.ToUpperInvariant()}\0{b.ToUpperInvariant()}";
        }

        private static object MapSellOrderTabStockItemRow(
            SellOrderTabStockItemRow s,
            IReadOnlyDictionary<string, (string? StockInCode, DateTime? StockInDate)> stockInMap,
            IReadOnlyDictionary<string, string?> warehouseNameMap)
        {
            var stockInId = s.StockInId?.Trim();
            string? stockInCode = null;
            DateTime? stockInDate = null;
            if (!string.IsNullOrWhiteSpace(stockInId) && stockInMap.TryGetValue(stockInId, out var sin))
            {
                stockInCode = sin.StockInCode;
                stockInDate = sin.StockInDate;
            }

            var warehouseId = s.WarehouseId?.Trim();
            string? warehouseName = null;
            if (!string.IsNullOrWhiteSpace(warehouseId) && warehouseNameMap.TryGetValue(warehouseId, out var wn))
                warehouseName = string.IsNullOrWhiteSpace(wn) ? null : wn.Trim();

            return new
            {
                s.Id,
                s.StockItemCode,
                s.StockAggregateId,
                stockInCode,
                stockInDate,
                warehouseName,
                s.RegionType,
                stockType = s.StockType,
                isStockingPoolMatch = s.IsStockingPoolMatch,
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
        }

        private sealed class SellOrderTabStockItemRow
        {
            public string Id { get; set; } = string.Empty;
            public string? StockItemCode { get; set; }
            public string StockAggregateId { get; set; } = string.Empty;
            public short RegionType { get; set; }
            public short StockType { get; set; }
            public string? PurchasePn { get; set; }
            public string? PurchaseBrand { get; set; }
            public short StockOutStatus { get; set; }
            public int QtyInbound { get; set; }
            public int QtyStockOut { get; set; }
            public int QtyRepertory { get; set; }
            public int QtyRepertoryAvailable { get; set; }
            public string? SellOrderItemId { get; set; }
            public string? SellOrderItemCode { get; set; }
            public string WarehouseId { get; set; } = string.Empty;
            public string? StockInId { get; set; }
            public string? PurchaseOrderItemCode { get; set; }
            public string? BatchNo { get; set; }
            public string? LocationId { get; set; }
            public DateTime CreateTime { get; set; }
            public bool IsStockingPoolMatch { get; set; }
        }
    }

    public class SalesOrderUpdateStatusRequest
    {
        public short Status { get; set; }
    }
}
