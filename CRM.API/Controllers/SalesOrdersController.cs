using CRM.Core.Constants;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Dtos;
using CRM.Core.Models.Quote;
using CRM.Core.Models.Sales;
using CRM.Core.Services;
using CRM.Core.Utilities;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
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
        private readonly ISalesOrderItemLineListQuery _salesOrderItemLineListQuery;
        private readonly ISalesOrderJourneyService _journeyService;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IRbacService _rbacService;
        private readonly IRepository<SellOrderItemExtend> _soItemExtendRepo;
        private readonly IOperationLogQueryService _operationLogQuery;
        private readonly ILogOperationAppendService _logOperationAppend;
        private readonly IFinanceReceivableService _financeReceivableService;
        private readonly IPackingService _packingService;
        private readonly IStockOutService _stockOutService;
        private readonly IStockInService _stockInService;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SalesOrdersController> _logger;

        public SalesOrdersController(
            ISalesOrderService service,
            ISalesOrderListQuery salesOrderListQuery,
            ISalesOrderItemLineListQuery salesOrderItemLineListQuery,
            ISalesOrderJourneyService journeyService,
            IDataPermissionService dataPermissionService,
            IRbacService rbacService,
            IRepository<SellOrderItemExtend> soItemExtendRepo,
            IOperationLogQueryService operationLogQuery,
            ILogOperationAppendService logOperationAppend,
            IFinanceReceivableService financeReceivableService,
            IPackingService packingService,
            IStockOutService stockOutService,
            IStockInService stockInService,
            ApplicationDbContext db,
            ILogger<SalesOrdersController> logger)
        {
            _service = service;
            _salesOrderListQuery = salesOrderListQuery;
            _salesOrderItemLineListQuery = salesOrderItemLineListQuery;
            _journeyService = journeyService;
            _dataPermissionService = dataPermissionService;
            _rbacService = rbacService;
            _soItemExtendRepo = soItemExtendRepo;
            _operationLogQuery = operationLogQuery;
            _logOperationAppend = logOperationAppend;
            _financeReceivableService = financeReceivableService;
            _packingService = packingService;
            _stockOutService = stockOutService;
            _stockInService = stockInService;
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

        [HttpGet("analytics/dashboard")]
        public async Task<IActionResult> GetListAnalyticsDashboard(
            [FromQuery] string? keyword,
            [FromQuery] string? code,
            [FromQuery] string? customer,
            [FromQuery] string? salesUserName,
            [FromQuery] string? comment,
            [FromQuery] short? status,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            CancellationToken cancellationToken = default)
        {
            var (request, maskAmounts) = await BuildListAnalyticsQueryRequestAsync(
                keyword, code, customer, salesUserName, comment, status, startDate, endDate, cancellationToken);
            var data = await _salesOrderListQuery.GetListAnalyticsDashboardAsync(request, maskAmounts, cancellationToken);
            return Ok(ApiResponse<SalesOrderListAnalyticsDashboardDto>.Ok(data));
        }

        [HttpGet("analytics/trends")]
        public async Task<IActionResult> GetListAnalyticsTrends(
            [FromQuery] string? keyword,
            [FromQuery] string? code,
            [FromQuery] string? customer,
            [FromQuery] string? salesUserName,
            [FromQuery] string? comment,
            [FromQuery] short? status,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] string? groupBy,
            CancellationToken cancellationToken = default)
        {
            var (request, maskAmounts) = await BuildListAnalyticsQueryRequestAsync(
                keyword, code, customer, salesUserName, comment, status, startDate, endDate, cancellationToken);
            var data = await _salesOrderListQuery.GetListAnalyticsTrendsAsync(
                request,
                string.IsNullOrWhiteSpace(groupBy) ? "month" : groupBy.Trim(),
                maskAmounts,
                cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<SalesOrderListAnalyticsTrendPointDto>>.Ok(data));
        }

        [HttpGet("analytics/breakdowns")]
        public async Task<IActionResult> GetListAnalyticsBreakdowns(
            [FromQuery] string? keyword,
            [FromQuery] string? code,
            [FromQuery] string? customer,
            [FromQuery] string? salesUserName,
            [FromQuery] string? comment,
            [FromQuery] short? status,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            CancellationToken cancellationToken = default)
        {
            var (request, maskAmounts) = await BuildListAnalyticsQueryRequestAsync(
                keyword, code, customer, salesUserName, comment, status, startDate, endDate, cancellationToken);
            var data = await _salesOrderListQuery.GetListAnalyticsBreakdownsAsync(request, maskAmounts, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>>.Ok(data));
        }

        [HttpGet("analytics/rankings")]
        public async Task<IActionResult> GetListAnalyticsRankings(
            [FromQuery] string? keyword,
            [FromQuery] string? code,
            [FromQuery] string? customer,
            [FromQuery] string? salesUserName,
            [FromQuery] string? comment,
            [FromQuery] short? status,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            CancellationToken cancellationToken = default)
        {
            var (request, maskAmounts) = await BuildListAnalyticsQueryRequestAsync(
                keyword, code, customer, salesUserName, comment, status, startDate, endDate, cancellationToken);
            var data = await _salesOrderListQuery.GetListAnalyticsRankingsAsync(request, maskAmounts, cancellationToken);
            return Ok(ApiResponse<SalesOrderListAnalyticsRankingsDto>.Ok(data));
        }

        [HttpGet("items/analytics/dashboard")]
        public async Task<IActionResult> GetItemListAnalyticsDashboard(
            [FromQuery] string? orderCreateStart,
            [FromQuery] string? orderCreateEnd,
            [FromQuery] string? customerName,
            [FromQuery] string? salesUserName,
            [FromQuery] string? salesUserId,
            [FromQuery] string? purchaseUserAccount,
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
            CancellationToken cancellationToken = default)
        {
            var (request, maskAmounts) = await BuildItemListAnalyticsQueryRequestAsync(
                orderCreateStart, orderCreateEnd, customerName, salesUserName, salesUserId, purchaseUserAccount, customerId,
                sellOrderCode, pn, customerSo, customerPn, transactionCurrency, stockOutPending, invoicePending,
                purchaseProgressStatus, stockInProgressStatus, stockOutNotifyProgressStatus, stockOutProgressStatus,
                receiptProgressStatus, invoiceProgressStatus, cancellationToken);
            var data = await _salesOrderItemLineListQuery.GetListAnalyticsDashboardAsync(request, maskAmounts, cancellationToken);
            return Ok(ApiResponse<SalesOrderItemListAnalyticsDashboardDto>.Ok(data));
        }

        [HttpGet("items/analytics/trends")]
        public async Task<IActionResult> GetItemListAnalyticsTrends(
            [FromQuery] string? orderCreateStart,
            [FromQuery] string? orderCreateEnd,
            [FromQuery] string? customerName,
            [FromQuery] string? salesUserName,
            [FromQuery] string? salesUserId,
            [FromQuery] string? purchaseUserAccount,
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
            [FromQuery] string? groupBy = null,
            CancellationToken cancellationToken = default)
        {
            var (request, maskAmounts) = await BuildItemListAnalyticsQueryRequestAsync(
                orderCreateStart, orderCreateEnd, customerName, salesUserName, salesUserId, purchaseUserAccount, customerId,
                sellOrderCode, pn, customerSo, customerPn, transactionCurrency, stockOutPending, invoicePending,
                purchaseProgressStatus, stockInProgressStatus, stockOutNotifyProgressStatus, stockOutProgressStatus,
                receiptProgressStatus, invoiceProgressStatus, cancellationToken);
            var data = await _salesOrderItemLineListQuery.GetListAnalyticsTrendsAsync(
                request,
                string.IsNullOrWhiteSpace(groupBy) ? "month" : groupBy.Trim(),
                maskAmounts,
                cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<SalesOrderItemListAnalyticsTrendPointDto>>.Ok(data));
        }

        [HttpGet("items/analytics/breakdowns")]
        public async Task<IActionResult> GetItemListAnalyticsBreakdowns(
            [FromQuery] string? orderCreateStart,
            [FromQuery] string? orderCreateEnd,
            [FromQuery] string? customerName,
            [FromQuery] string? salesUserName,
            [FromQuery] string? salesUserId,
            [FromQuery] string? purchaseUserAccount,
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
            CancellationToken cancellationToken = default)
        {
            var (request, maskAmounts) = await BuildItemListAnalyticsQueryRequestAsync(
                orderCreateStart, orderCreateEnd, customerName, salesUserName, salesUserId, purchaseUserAccount, customerId,
                sellOrderCode, pn, customerSo, customerPn, transactionCurrency, stockOutPending, invoicePending,
                purchaseProgressStatus, stockInProgressStatus, stockOutNotifyProgressStatus, stockOutProgressStatus,
                receiptProgressStatus, invoiceProgressStatus, cancellationToken);
            var data = await _salesOrderItemLineListQuery.GetListAnalyticsBreakdownsAsync(request, maskAmounts, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>>.Ok(data));
        }

        [HttpGet("items/analytics/rankings")]
        public async Task<IActionResult> GetItemListAnalyticsRankings(
            [FromQuery] string? orderCreateStart,
            [FromQuery] string? orderCreateEnd,
            [FromQuery] string? customerName,
            [FromQuery] string? salesUserName,
            [FromQuery] string? salesUserId,
            [FromQuery] string? purchaseUserAccount,
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
            CancellationToken cancellationToken = default)
        {
            var (request, maskAmounts) = await BuildItemListAnalyticsQueryRequestAsync(
                orderCreateStart, orderCreateEnd, customerName, salesUserName, salesUserId, purchaseUserAccount, customerId,
                sellOrderCode, pn, customerSo, customerPn, transactionCurrency, stockOutPending, invoicePending,
                purchaseProgressStatus, stockInProgressStatus, stockOutNotifyProgressStatus, stockOutProgressStatus,
                receiptProgressStatus, invoiceProgressStatus, cancellationToken);
            var data = await _salesOrderItemLineListQuery.GetListAnalyticsRankingsAsync(request, maskAmounts, cancellationToken);
            return Ok(ApiResponse<SalesOrderItemListAnalyticsRankingsDto>.Ok(data));
        }

        /// <summary>销售订单明细分页（字面路由 <c>items</c>；与 <c>{id:guid}</c> 子路由并存，避免 <c>items</c> 被误解析为订单主键）。</summary>
        [HttpGet("items")]
        public async Task<IActionResult> GetSellOrderItemLines(
            [FromQuery] string? orderCreateStart,
            [FromQuery] string? orderCreateEnd,
            [FromQuery] string? customerName,
            [FromQuery] string? salesUserName,
            [FromQuery] string? salesUserId,
            [FromQuery] string? purchaseUserAccount,
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
                    PurchaseUserAccount = !string.IsNullOrWhiteSpace(purchaseUserAccount) ? purchaseUserAccount.Trim() : null,
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

        /// <summary>销售订单详情页：底部页签用下游列表（需求明细/采购申请/采购订单明细/入库/库存/出库通知/出库/收款核销/销项发票）。</summary>
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

                var data = await BuildSalesOrderDetailTabAggregatesPayloadAsync(id, itemIds, sellOrderItemIdScope: null, mask521, mask511, userId);
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

                var data = await BuildSalesOrderDetailTabAggregatesPayloadAsync(id, orderLineIds, sellOrderItemIdScope: lineId, mask521, mask511, userId);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取销售订单明细页签数据失败: {OrderId} {ItemId}", id, sellOrderItemId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>销售订单「单条明细」绩效面板：三层利润（报价 / 预计销售 / 出库）；展开面板时按需加载。</summary>
        [HttpGet("{id:guid}/sell-order-items/{sellOrderItemId:guid}/line-profit")]
        public async Task<IActionResult> GetSellOrderItemLineProfit(string id, string sellOrderItemId)
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

                var data = await BuildSellOrderLineProfitAsync(lineId, mask521);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取销售订单明细绩效失败: {OrderId} {ItemId}", id, sellOrderItemId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <param name="sellOrderItemIdScope">非 null 时：采购申请、采购订单明细、出库通知、收款核销仅保留该销售明细；在库/出库/销项发票链仅使用该明细。</param>
        private async Task<object> BuildSalesOrderDetailTabAggregatesPayloadAsync(
            string orderId,
            IReadOnlyList<string> allOrderLineIds,
            string? sellOrderItemIdScope,
            bool mask521,
            bool mask511,
            string? currentUserId)
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

            var stockInRows = await BuildSellOrderTabStockInsAsync(itemIds, mask511);

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

            var outReqRows = await BuildSellOrderTabStockOutRequestsAsync(itemIds, mask521);

            var stockOutRows = await BuildSellOrderTabStockOutsAsync(itemIds, mask521);

            var receiptWriteOffEntities = itemIds.Count == 0
                ? Array.Empty<FinanceReceivableWriteOffLedgerItem>()
                : await _financeReceivableService.GetWriteOffLedgerBySellOrderItemIdsAsync(itemIds, currentUserId);

            var receiptWriteOffRows = receiptWriteOffEntities
                .Select(row =>
                {
                    var amt = row.Amount;
                    var cname = row.CustomerName;
                    var cnameEn = row.CustomerEnglishName;
                    if (mask521)
                    {
                        amt = 0m;
                        cname = null;
                        cnameEn = null;
                    }
                    return (object)new
                    {
                        id = row.Id,
                        amount = amt,
                        writeOffSource = row.WriteOffSource,
                        createTime = row.CreateTime,
                        financeReceiptId = row.FinanceReceiptId,
                        financeReceiptCode = row.FinanceReceiptCode,
                        financeReceivableId = row.FinanceReceivableId,
                        receivableCode = row.ReceivableCode,
                        stockOutId = row.StockOutId,
                        stockOutCode = row.StockOutCode,
                        sellOrderId = row.SellOrderId,
                        sellOrderCode = row.SellOrderCode,
                        customerName = cname,
                        customerEnglishName = cnameEn,
                        pn = row.PN,
                        brand = row.Brand,
                        currency = row.Currency,
                        operatorUserName = row.OperatorUserName,
                        remark = row.Remark
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

            object? lineOverview = null;
            if (sellOrderItemIdScope != null)
            {
                lineOverview = await BuildSellOrderLineOverviewAsync(
                    sellOrderItemIdScope,
                    prRows.Select(p => (p.Status, p.Qty)).ToList(),
                    mask521);
            }

            object? stockingUsage = null;
            if (sellOrderItemIdScope != null)
                stockingUsage = await BuildSellOrderStockingUsageAsync(sellOrderItemIdScope);

            var packingRows = await BuildSellOrderTabPackingsAsync(itemIds, mask521);

            return new
            {
                rfqItems = rfqItemRows,
                quotes = quoteRows,
                purchaseRequisitions = prRows,
                purchaseOrderItems = purchaseOrderItemRows,
                stockIns = stockInRows,
                stockItems = stockItemRows,
                packings = packingRows,
                stockOutRequests = outReqRows,
                stockOuts = stockOutRows,
                receiptWriteOffs = receiptWriteOffRows,
                sellInvoices = sellInvRows,
                qcImages = qcImageRows,
                lineOverview,
                stockingUsage
            };
        }

        /// <summary>
        /// 销售明细下游「入库」：经入库明细扩展 <c>sell_order_item_id</c> 反查入库单，字段与入库单列表（<see cref="StockInListItemDto"/>）一致。
        /// </summary>
        private async Task<List<object>> BuildSellOrderTabStockInsAsync(IReadOnlyList<string> itemIds, bool mask511)
        {
            if (itemIds.Count == 0)
                return new List<object>();

            var stockInIdList = await _db.StockInItemExtends.AsNoTracking()
                .Where(e => e.SellOrderItemId != null && itemIds.Contains(e.SellOrderItemId!))
                .Select(e => e.StockInId)
                .Distinct()
                .ToListAsync();

            if (stockInIdList.Count == 0)
                return new List<object>();

            var orderedIds = await _db.StockIns.AsNoTracking()
                .Where(si => stockInIdList.Contains(si.Id))
                .OrderByDescending(si => si.CreateTime)
                .ThenByDescending(si => si.Id)
                .Select(si => si.Id)
                .ToListAsync();

            if (orderedIds.Count == 0)
                return new List<object>();

            var dtos = await _stockInService.GetStockInListItemsByIdsAsync(orderedIds);
            if (mask511)
                PurchaseSensitiveFieldMask511.ApplyStockInListItems(dtos, true);

            return dtos.Select(d => (object)new
            {
                id = d.Id,
                stockInCode = d.StockInCode,
                stockInType = d.StockInType,
                sourceDisplayNo = d.SourceDisplayNo,
                warehouseId = d.WarehouseId,
                vendorId = d.VendorId,
                vendorName = d.VendorName,
                vendorEnglishName = d.VendorEnglishName,
                vendorCode = d.VendorCode,
                purchaseOrderCode = d.PurchaseOrderCode,
                freightForwarderOrderNo = d.FreightForwarderOrderNo,
                salesOrderCode = d.SalesOrderCode,
                materialModelSummary = d.MaterialModelSummary,
                materialBrandSummary = d.MaterialBrandSummary,
                stockInDate = d.StockInDate,
                totalQuantity = d.TotalQuantity,
                totalAmount = d.TotalAmount,
                currencyCode = d.CurrencyCode,
                status = d.Status,
                remark = d.Remark,
                createTime = d.CreateTime,
                createUserName = d.CreateUserName,
                hasBatchEntered = d.HasBatchEntered,
                customsDeclarationId = d.CustomsDeclarationId,
                customsDeclarationCode = d.CustomsDeclarationCode
            }).ToList();
        }

        /// <summary>
        /// 销售明细下游「出库通知」：按 <c>sales_order_item_id</c> 关联出库通知，字段与出库通知列表（<see cref="StockOutRequestListItemDto"/>）一致。
        /// </summary>
        private async Task<List<object>> BuildSellOrderTabStockOutRequestsAsync(IReadOnlyList<string> itemIds, bool mask521)
        {
            if (itemIds.Count == 0)
                return new List<object>();

            var orderedIds = await _db.StockOutRequests.AsNoTracking()
                .Where(r => r.SalesOrderItemId != null && itemIds.Contains(r.SalesOrderItemId!))
                .OrderByDescending(r => r.CreateTime)
                .ThenByDescending(r => r.Id)
                .Select(r => r.Id)
                .ToListAsync();

            if (orderedIds.Count == 0)
                return new List<object>();

            var dtos = await _stockOutService.GetStockOutRequestListItemsByIdsAsync(orderedIds);
            if (mask521)
            {
                foreach (var d in dtos)
                    SaleSensitiveFieldMask521.ApplyStockOutRequestListItem(d, true);
            }

            return dtos.Select(d => (object)new
            {
                id = d.Id,
                requestCode = d.RequestCode,
                salesOrderId = d.SalesOrderId,
                salesOrderItemId = d.SalesOrderItemId,
                salesOrderCode = d.SalesOrderCode,
                materialModel = d.MaterialModel,
                brand = d.Brand,
                outQuantity = d.OutQuantity,
                expectedStockOutDate = d.ExpectedStockOutDate,
                salesUserName = d.SalesUserName,
                customerId = d.CustomerId,
                customerName = d.CustomerName,
                requestUserId = d.RequestUserId,
                requestUserName = d.RequestUserName,
                requestDate = d.RequestDate,
                status = d.Status,
                customsStatus = d.CustomsStatus,
                remark = d.Remark,
                shipmentMethod = d.ShipmentMethod,
                expressCompany = d.ExpressCompany,
                packingId = d.PackingId,
                packingCode = d.PackingCode,
                regionType = d.RegionType,
                stockOutType = d.StockOutType,
                salesStockOutNotifyId = d.SalesStockOutNotifyId,
                salesStockOutNotifyCode = d.SalesStockOutNotifyCode,
                currency = d.Currency,
                createTime = d.CreateTime,
                customsDeclarationId = d.CustomsDeclarationId,
                customsDeclarationCode = d.CustomsDeclarationCode,
                customsBrokerName = d.CustomsBrokerName
            }).ToList();
        }

        /// <summary>
        /// 销售明细下游「出库」：按 <c>sell_order_item_id</c> 关联出库单，字段与出库单列表（<see cref="StockOutListItemDto"/>）一致。
        /// </summary>
        private async Task<List<object>> BuildSellOrderTabStockOutsAsync(IReadOnlyList<string> itemIds, bool mask521)
        {
            if (itemIds.Count == 0)
                return new List<object>();

            var orderedIds = await _db.StockOuts.AsNoTracking()
                .Where(so => so.SellOrderItemId != null && itemIds.Contains(so.SellOrderItemId!))
                .OrderByDescending(so => so.CreateTime)
                .ThenByDescending(so => so.Id)
                .Select(so => so.Id)
                .ToListAsync();

            if (orderedIds.Count == 0)
                return new List<object>();

            var dtos = await _stockOutService.GetStockOutListItemsByIdsAsync(orderedIds);
            return dtos.Select(d => (object)new
            {
                id = d.Id,
                stockOutCode = d.StockOutCode,
                stockOutType = d.StockOutType,
                sourceCode = d.SourceCode,
                stockOutDate = d.StockOutDate,
                expectedStockOutDate = d.ExpectedStockOutDate,
                packingCount = d.PackingCount,
                packingCodes = d.PackingCodes,
                totalQuantity = d.TotalQuantity,
                totalAmount = mask521 ? 0m : d.TotalAmount,
                status = d.Status,
                remark = d.Remark,
                createTime = d.CreateTime,
                createUserName = d.CreateUserName,
                customerName = mask521 ? null : d.CustomerName,
                customerEnglishName = mask521 ? null : d.CustomerEnglishName,
                customerCode = mask521 ? null : d.CustomerCode,
                salesUserName = mask521 ? null : d.SalesUserName,
                sellOrderItemCode = d.SellOrderItemCode,
                shipmentMethod = d.ShipmentMethod,
                expressCompany = d.ExpressCompany,
                courierTrackingNo = d.CourierTrackingNo,
                freightForwarderOrderNo = d.FreightForwarderOrderNo,
                salesStockOutNotifyId = d.SalesStockOutNotifyId,
                salesStockOutNotifyCode = d.SalesStockOutNotifyCode,
                customsDeclarationId = d.CustomsDeclarationId,
                customsDeclarationCode = d.CustomsDeclarationCode
            }).ToList();
        }

        /// <summary>
        /// 销售明细下游「装箱单」：经装箱明细 <c>sell_order_item_id</c> 或关联出库通知反查装箱单主单（字段与装箱单列表一致）。
        /// </summary>
        private async Task<List<object>> BuildSellOrderTabPackingsAsync(IReadOnlyList<string> itemIds, bool mask521)
        {
            if (itemIds.Count == 0)
                return new List<object>();

            var notifyIds = await _db.StockOutRequests.AsNoTracking()
                .Where(r => r.SalesOrderItemId != null && itemIds.Contains(r.SalesOrderItemId))
                .Select(r => r.Id)
                .ToListAsync();

            var packingIds = await (
                    from pi in _db.PackingItems.AsNoTracking()
                    join pk in _db.Packings.AsNoTracking() on pi.PackingId equals pk.Id
                    where !pi.IsDeleted
                          && !pk.IsDeleted
                          && ((pi.SellOrderItemId != null && itemIds.Contains(pi.SellOrderItemId))
                              || (pi.StockOutNotifyId != null && notifyIds.Contains(pi.StockOutNotifyId)))
                    select pk.Id)
                .Distinct()
                .ToListAsync();

            if (packingIds.Count == 0)
                return new List<object>();

            var orderedIds = await _db.Packings.AsNoTracking()
                .Where(p => packingIds.Contains(p.Id) && !p.IsDeleted)
                .OrderByDescending(p => p.CreateTime)
                .ThenByDescending(p => p.Id)
                .Select(p => p.Id)
                .ToListAsync();

            var dtos = await _packingService.GetPackingListItemsByIdsAsync(orderedIds);
            return dtos.Select(d => (object)new
            {
                id = d.Id,
                code = d.Code,
                status = d.Status,
                stockOutType = d.StockOutType,
                materialType = d.MaterialType,
                customerId = mask521 ? null : d.CustomerId,
                customerName = mask521 ? null : d.CustomerName,
                salesId = mask521 ? null : d.SalesId,
                salesUserName = mask521 ? null : d.SalesUserName,
                storageId = d.StorageId,
                warehouseName = d.WarehouseName,
                itemRows = d.ItemRows,
                comment = d.Comment,
                scheduleShipDate = d.ScheduleShipDate,
                requestDate = d.RequestDate,
                shipmentMethod = d.ShipmentMethod,
                expressCompany = d.ExpressCompany,
                createTime = d.CreateTime,
                createByUserId = d.CreateByUserId,
                createUserName = d.CreateUserName,
                customsDeclarationId = d.CustomsDeclarationId,
                customsDeclarationCode = d.CustomsDeclarationCode
            }).ToList();
        }

        /// <summary>
        /// 销售明细「使用备货」：按采购主单汇总备货补充拣货 <c>PickedQty</c>（<c>IsStockingSupplement</c>）。
        /// </summary>
        private async Task<object> BuildSellOrderStockingUsageAsync(string sellOrderItemId)
        {
            var lineId = sellOrderItemId.Trim();

            var notifyIds = await _db.StockOutRequests.AsNoTracking()
                .Where(r => r.SalesOrderItemId == lineId)
                .Select(r => r.Id)
                .ToListAsync();

            var packingItemIds = await _db.PackingItems.AsNoTracking()
                .Where(pi => !pi.IsDeleted
                             && ((pi.SellOrderItemId != null && pi.SellOrderItemId == lineId)
                                 || (pi.StockOutNotifyId != null && notifyIds.Contains(pi.StockOutNotifyId))))
                .Select(pi => pi.Id)
                .ToListAsync();

            if (packingItemIds.Count == 0)
                return new { totalUsedQty = 0, items = Array.Empty<object>() };

            var pickRows = await (
                    from pti in _db.PickingTaskItems.AsNoTracking()
                    join pt in _db.PickingTasks.AsNoTracking() on pti.PickingTaskId equals pt.Id
                    where !pti.IsDeleted
                          && !pt.IsDeleted
                          && pti.IsStockingSupplement
                          && pti.PickedQty > 0
                          && pti.PackingItemId != null
                          && packingItemIds.Contains(pti.PackingItemId!)
                    select new
                    {
                        pti.PickedQty,
                        pti.StockItemId
                    })
                .ToListAsync();

            if (pickRows.Count == 0)
                return new { totalUsedQty = 0, items = Array.Empty<object>() };

            var stockIds = pickRows
                .Select(x => x.StockItemId?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList();

            var stockPoLookup = new Dictionary<string, (string? PoId, string? PoCode, DateTime? PoCreate, string? UserName)>(
                StringComparer.OrdinalIgnoreCase);

            if (stockIds.Count > 0)
            {
                var layers = await _db.StockItems.AsNoTracking()
                    .Where(si => stockIds.Contains(si.Id))
                    .Select(si => new
                    {
                        si.Id,
                        si.PurchaseOrderItemId,
                        si.PurchaserName
                    })
                    .ToListAsync();

                var poiIds = layers
                    .Select(x => x.PurchaseOrderItemId?.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Cast<string>()
                    .ToList();

                var poByPoiId = poiIds.Count == 0
                    ? new Dictionary<string, (string PoId, string PoCode, DateTime CreateTime, string? UserName)>(
                        StringComparer.OrdinalIgnoreCase)
                    : (await (
                            from poi in _db.PurchaseOrderItems.AsNoTracking()
                            join po in _db.PurchaseOrders.AsNoTracking() on poi.PurchaseOrderId equals po.Id
                            where poiIds.Contains(poi.Id)
                            select new
                            {
                                PoiId = poi.Id,
                                PoId = po.Id,
                                PoCode = po.PurchaseOrderCode,
                                po.CreateTime,
                                po.PurchaseUserName
                            })
                        .ToListAsync())
                    .ToDictionary(
                        x => x.PoiId.Trim(),
                        x => (x.PoId, x.PoCode, x.CreateTime, UserName: x.PurchaseUserName),
                        StringComparer.OrdinalIgnoreCase);

                foreach (var layer in layers)
                {
                    var key = layer.Id.Trim();
                    string? poId = null;
                    string? poCode = null;
                    DateTime? poCreate = null;
                    var userName = layer.PurchaserName;
                    var poiId = layer.PurchaseOrderItemId?.Trim();
                    if (!string.IsNullOrEmpty(poiId) && poByPoiId.TryGetValue(poiId, out var po))
                    {
                        poId = po.PoId;
                        poCode = po.PoCode;
                        poCreate = po.CreateTime;
                        userName = po.UserName ?? userName;
                    }

                    stockPoLookup[key] = (poId, poCode, poCreate, userName);
                }
            }

            var grouped = new Dictionary<string, (string PoId, string PoCode, DateTime? PoCreate, string? UserName, int Qty)>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var pick in pickRows)
            {
                var qty = Math.Max(0, pick.PickedQty);
                if (qty <= 0)
                    continue;

                var sid = pick.StockItemId?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(sid)
                    || !stockPoLookup.TryGetValue(sid, out var meta)
                    || string.IsNullOrWhiteSpace(meta.PoId)
                    || string.IsNullOrWhiteSpace(meta.PoCode))
                    continue;

                var groupKey = meta.PoId!.Trim();
                if (grouped.TryGetValue(groupKey, out var existing))
                {
                    grouped[groupKey] = (
                        existing.PoId,
                        existing.PoCode,
                        existing.PoCreate ?? meta.PoCreate,
                        existing.UserName ?? meta.UserName,
                        existing.Qty + qty);
                }
                else
                {
                    grouped[groupKey] = (
                        meta.PoId!,
                        meta.PoCode!,
                        meta.PoCreate,
                        meta.UserName,
                        qty);
                }
            }

            var items = grouped.Values
                .Where(x => x.Qty > 0)
                .OrderByDescending(x => x.PoCreate ?? DateTime.MinValue)
                .Select(x => (object)new
                {
                    purchaseOrderId = x.PoId,
                    purchaseOrderCode = x.PoCode,
                    purchaseOrderCreateTime = x.PoCreate,
                    purchaseUserName = x.UserName,
                    usedQty = x.Qty
                })
                .ToList();

            var totalUsedQty = grouped.Values.Sum(x => x.Qty);
            return new { totalUsedQty, items };
        }

        private const short PrStatusCancelled = 3;
        private const short StockInCompletedStatus = 2;

        /// <summary>销售订单明细详情「概况」页签：4×10 执行进度矩阵（仅单条明细 scope 时返回）。</summary>
        private async Task<object?> BuildSellOrderLineOverviewAsync(
            string lineId,
            IReadOnlyList<(short Status, decimal Qty)> prEntries,
            bool mask521)
        {
            var soItem = await _db.SellOrderItems.AsNoTracking()
                .Where(i => i.Id == lineId)
                .Select(i => new { i.Qty, i.Price, i.Currency })
                .FirstOrDefaultAsync();
            if (soItem == null)
                return null;

            var ext = await _db.SellOrderItemExtends.AsNoTracking()
                .Where(e => e.Id == lineId)
                .Select(e => new
                {
                    e.QtyAlreadyPurchased,
                    e.QtyNotPurchase,
                    e.QtyStockOutNotify,
                    e.QtyStockOutNotifyNot,
                    e.QtyStockOutActual,
                    e.ReceiptAmount,
                    e.ReceiptAmountFinish,
                    e.ReceiptAmountNot,
                    e.InvoiceAmount,
                    e.InvoiceAmountFinish,
                    e.InvoiceAmountNot
                })
                .FirstOrDefaultAsync();

            var qtyLine = soItem.Qty;
            var lineAmount = Math.Round(qtyLine * soItem.Price, 2, MidpointRounding.AwayFromZero);
            var currency = soItem.Currency;

            var prDone = prEntries
                .Where(p => p.Status != PrStatusCancelled)
                .Sum(p => p.Qty);
            var prPending = Math.Max(0m, qtyLine - prDone);

            decimal stockInDone = 0m;
            var extMatches = await _db.StockInItemExtends.AsNoTracking()
                .Where(x => x.SellOrderItemId == lineId)
                .Select(x => x.Id)
                .ToListAsync();
            if (extMatches.Count > 0)
            {
                var siItems = await _db.StockInItems.AsNoTracking()
                    .Where(x => extMatches.Contains(x.Id))
                    .Select(x => new { x.StockInId, x.Quantity })
                    .ToListAsync();
                var siIds = siItems.Select(x => x.StockInId).Distinct().ToList();
                var completedSiIds = await _db.StockIns.AsNoTracking()
                    .Where(s => siIds.Contains(s.Id)
                        && s.Status == StockInCompletedStatus
                        && s.StockInType == StockInTypeCode.Purchase)
                    .Select(s => s.Id)
                    .ToListAsync();
                var completedSet = completedSiIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
                stockInDone = siItems
                    .Where(i => completedSet.Contains(i.StockInId))
                    .Sum(i => (decimal)i.Quantity);
            }
            var stockInPending = Math.Max(0m, qtyLine - stockInDone);

            var poDone = ext?.QtyAlreadyPurchased ?? 0m;
            var poPending = ext?.QtyNotPurchase ?? Math.Max(0m, qtyLine - poDone);
            var notifyDone = ext?.QtyStockOutNotify ?? 0m;
            var notifyPending = ext?.QtyStockOutNotifyNot ?? Math.Max(0m, qtyLine - notifyDone);
            var stockOutDone = ext?.QtyStockOutActual ?? 0m;
            var stockOutPending = Math.Max(0m, qtyLine - stockOutDone);

            var receiptTotal = ext?.ReceiptAmount ?? lineAmount;
            var receiptDone = ext?.ReceiptAmountFinish ?? 0m;
            var receiptPending = ext?.ReceiptAmountNot ?? Math.Max(0m, receiptTotal - receiptDone);
            var invoiceTotal = ext?.InvoiceAmount ?? lineAmount;
            var invoiceDone = ext?.InvoiceAmountFinish ?? 0m;
            var invoicePending = ext?.InvoiceAmountNot ?? Math.Max(0m, invoiceTotal - invoiceDone);

            if (mask521)
            {
                lineAmount = 0m;
                receiptTotal = 0m;
                receiptDone = 0m;
                receiptPending = 0m;
                invoiceTotal = 0m;
                invoiceDone = 0m;
                invoicePending = 0m;
            }

            static object QtyMetric(decimal total, decimal done, decimal pending) => new
            {
                total,
                done,
                pending
            };

            static object AmtMetric(decimal total, decimal done, decimal pending, short currencyCode) => new
            {
                total,
                done,
                pending,
                currency = currencyCode
            };

            return new
            {
                lineAmount = new { total = lineAmount, currency },
                lineQty = new { total = qtyLine },
                purchaseRequisition = QtyMetric(qtyLine, prDone, prPending),
                purchaseOrder = QtyMetric(qtyLine, poDone, poPending),
                stockIn = QtyMetric(qtyLine, stockInDone, stockInPending),
                stockOutNotify = QtyMetric(qtyLine, notifyDone, notifyPending),
                stockOut = QtyMetric(qtyLine, stockOutDone, stockOutPending),
                receiptWriteOff = AmtMetric(receiptTotal, receiptDone, receiptPending, currency),
                invoice = AmtMetric(invoiceTotal, invoiceDone, invoicePending, currency)
            };
        }

        /// <summary>销售订单明细详情「绩效」面板：三层利润（报价 / 预计销售 / 出库），仅单条明细 scope 时返回。</summary>
        private async Task<object?> BuildSellOrderLineProfitAsync(string lineId, bool mask521)
        {
            if (mask521)
                return null;

            var soItem = await _db.SellOrderItems.AsNoTracking()
                .Where(i => i.Id == lineId)
                .Select(i => new { i.Qty, i.Price, i.Currency, i.ConvertPrice })
                .FirstOrDefaultAsync();
            if (soItem == null)
                return null;

            var ext = await _db.SellOrderItemExtends.AsNoTracking()
                .Where(e => e.Id == lineId)
                .Select(e => new
                {
                    e.QuoteCost,
                    e.QuoteCurrency,
                    e.QuoteConvertCost,
                    e.FxUsdToCnySnapshot,
                    e.FxUsdToHkdSnapshot,
                    e.FxUsdToEurSnapshot,
                    e.QuoteProfitExpected,
                    e.QuoteProfitRateExpected,
                    e.ReQuoteProfitExpected,
                    e.ReQuoteProfitRateExpected,
                    e.PoCostUsdTotal,
                    e.PoCostUsdConfirmed,
                    e.PurchaseProfitExpected,
                    e.SalesProfitExpected,
                    e.ProfitOutBizUsd,
                    e.ProfitOutRateBiz,
                    e.QtyStockOutActual,
                    e.PurchaseProgressStatus,
                    e.StockOutProgressStatus
                })
                .FirstOrDefaultAsync();
            if (ext == null)
                return null;

            var revenueUsd = Math.Round(soItem.Qty * soItem.ConvertPrice, 2, MidpointRounding.AwayFromZero);
            var quoteCostUsd = Math.Round(soItem.Qty * ext.QuoteConvertCost, 2, MidpointRounding.AwayFromZero);
            var useReQuote = ext.QuoteConvertCost > 0m;
            var quoteProfit = useReQuote ? ext.ReQuoteProfitExpected : ext.QuoteProfitExpected;
            var quoteRateStored = useReQuote ? ext.ReQuoteProfitRateExpected : ext.QuoteProfitRateExpected;
            var quoteRate = SellOrderItemProfitDisplay.ResolveStoredRateForDisplay(
                quoteRateStored,
                quoteCostUsd,
                quoteProfit);

            var salesRate = SellOrderItemProfitDisplay.ResolveSalesExpectedRateForDisplay(
                revenueUsd,
                ext.PoCostUsdConfirmed);
            var poItems = await _db.PurchaseOrderItems.AsNoTracking()
                .Where(p => p.SellOrderItemId == lineId)
                .Select(p => new { p.Id, p.PurchaseOrderItemCode, p.Qty, p.ConvertPrice })
                .ToListAsync();
            var poQtyTotal = poItems.Sum(p => p.Qty);
            var avgPoCostUsd = poQtyTotal > 0m
                ? Math.Round(poItems.Sum(p => p.Qty * p.ConvertPrice) / poQtyTotal, 6, MidpointRounding.AwayFromZero)
                : 0m;
            var poCostLines = poItems
                .GroupBy(p => new
                {
                    PoId = p.Id.Trim(),
                    PoCode = (p.PurchaseOrderItemCode ?? string.Empty).Trim(),
                    p.ConvertPrice
                })
                .Select(g => new
                {
                    purchaseOrderItemId = g.Key.PoId,
                    purchaseOrderItemCode = string.IsNullOrEmpty(g.Key.PoCode) ? null : g.Key.PoCode,
                    convertPriceUsd = g.Key.ConvertPrice,
                    qty = g.Sum(x => x.Qty),
                    costUsd = Math.Round(g.Sum(x => x.Qty * x.ConvertPrice), 2, MidpointRounding.AwayFromZero)
                })
                .OrderBy(l => l.purchaseOrderItemCode ?? l.purchaseOrderItemId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(l => l.convertPriceUsd)
                .ToList();
            var outQty = ext.QtyStockOutActual;
            var outboundRevenueUsd = Math.Round(outQty * soItem.ConvertPrice, 2, MidpointRounding.AwayFromZero);

            var outboundCostLineRows = await LoadSellOrderOutboundCostLinesAsync(lineId);
            var outboundSnapshot = SellOrderOutboundProfitCalc.Compute(
                outboundRevenueUsd,
                outQty,
                outboundCostLineRows,
                avgPoCostUsd);
            var outboundCostDetailRows = outboundSnapshot.UseActualBatchCost
                ? SellOrderOutboundProfitCalc.OrderCostDetailsForDisplay(
                    await LoadSellOrderOutboundCostDetailsAsync(lineId))
                : Array.Empty<SellOrderOutboundCostDetailLine>();
            var outboundCostUsd = outboundSnapshot.OutboundCostUsd;
            var effectiveOutboundAvgCostUsd = outboundSnapshot.EffectiveAvgCostUsd;
            var outboundProfitUsd = outboundSnapshot.ProfitOutBizUsd;
            var outboundRateStored = outboundSnapshot.ProfitOutRateBiz;
            var outboundRate = SellOrderItemProfitDisplay.ResolveProfitOutRateBizForDisplay(
                outboundRateStored,
                outboundProfitUsd);

            return new
            {
                qty = soItem.Qty,
                sellPrice = soItem.Price,
                sellCurrency = soItem.Currency,
                convertPrice = soItem.ConvertPrice,
                quoteCost = ext.QuoteCost,
                quoteCurrency = ext.QuoteCurrency,
                quoteConvertCost = ext.QuoteConvertCost,
                fxUsdToCnySnapshot = ext.FxUsdToCnySnapshot,
                fxUsdToHkdSnapshot = ext.FxUsdToHkdSnapshot,
                fxUsdToEurSnapshot = ext.FxUsdToEurSnapshot,
                useReQuote,
                revenueUsd,
                quoteCostUsd,
                poCostUsdTotal = ext.PoCostUsdTotal,
                poCostUsdConfirmed = ext.PoCostUsdConfirmed,
                purchaseProfitExpected = ext.PurchaseProfitExpected,
                qtyStockOutActual = outQty,
                poQtyTotal,
                avgPoCostUsd,
                poCostLines,
                useActualOutboundCost = outboundSnapshot.UseActualBatchCost,
                effectiveOutboundAvgCostUsd,
                outboundCostLines = outboundSnapshot.CostLines.Select(l => new
                {
                    purchaseOrderItemId = l.PurchaseOrderItemId,
                    purchaseOrderItemCode = l.PurchaseOrderItemCode,
                    purchasePriceUsd = l.PurchasePriceUsd,
                    qty = l.Qty,
                    costUsd = Math.Round(l.Qty * l.PurchasePriceUsd, 2, MidpointRounding.AwayFromZero),
                    profitOutBizUsd = l.ProfitOutBizUsd
                }),
                outboundCostDetails = outboundCostDetailRows.Select(d => new
                {
                    stockOutId = d.StockOutId,
                    stockOutCode = d.StockOutCode,
                    stockOutItemId = d.StockOutItemId,
                    purchaseOrderItemId = d.PurchaseOrderItemId,
                    purchaseOrderItemCode = d.PurchaseOrderItemCode,
                    purchasePriceUsd = d.PurchasePriceUsd,
                    qty = d.Qty,
                    costUsd = d.CostUsd
                }),
                outboundRevenueUsd,
                outboundCostUsd,
                purchaseProgressStatus = ext.PurchaseProgressStatus,
                stockOutProgressStatus = ext.StockOutProgressStatus,
                quote = new
                {
                    profitUsd = quoteProfit,
                    profitRate = quoteRate
                },
                salesExpected = new
                {
                    profitUsd = ext.SalesProfitExpected,
                    profitRate = salesRate
                },
                outbound = new
                {
                    profitUsd = outboundProfitUsd,
                    profitRate = outboundRate
                }
            };
        }

        private async Task<List<SellOrderOutboundCostLine>> LoadSellOrderOutboundCostLinesAsync(string sellOrderItemId)
        {
            var lineId = sellOrderItemId.Trim();
            const short stockOutCompleted = 2;
            const short stockOutFinished = 4;

            var raw = await (
                from so in _db.StockOuts.AsNoTracking()
                join soi in _db.StockOutItems.AsNoTracking() on so.Id equals soi.StockOutId
                join ext in _db.StockOutItemExtends.AsNoTracking() on soi.Id equals ext.Id
                where !so.IsDeleted
                      && !soi.IsDeleted
                      && !ext.IsDeleted
                      && (so.Status == stockOutCompleted || so.Status == stockOutFinished)
                      && so.StockOutType == StockOutTypeCode.Sales
                      && so.SellOrderItemId == lineId
                select new
                {
                    ext.PurchaseOrderItemId,
                    ext.PurchaseOrderItemCode,
                    ext.PurchasePriceUsd,
                    ext.QtyStockOut,
                    ext.ProfitOutBizUsd,
                    QtyFallback = soi.ActualQty > 0 ? soi.ActualQty : soi.Quantity
                }).ToListAsync();

            return raw
                .Select(r =>
                {
                    var qty = r.QtyStockOut > 0 ? r.QtyStockOut : r.QtyFallback;
                    return new SellOrderOutboundCostLine
                    {
                        PurchaseOrderItemId = r.PurchaseOrderItemId,
                        PurchaseOrderItemCode = r.PurchaseOrderItemCode,
                        PurchasePriceUsd = r.PurchasePriceUsd,
                        Qty = qty,
                        ProfitOutBizUsd = r.ProfitOutBizUsd
                    };
                })
                .Where(l => l.Qty > 0)
                .ToList();
        }

        private async Task<List<SellOrderOutboundCostDetailLine>> LoadSellOrderOutboundCostDetailsAsync(string sellOrderItemId)
        {
            var lineId = sellOrderItemId.Trim();
            const short stockOutCompleted = 2;
            const short stockOutFinished = 4;

            var raw = await (
                from so in _db.StockOuts.AsNoTracking()
                join soi in _db.StockOutItems.AsNoTracking() on so.Id equals soi.StockOutId
                join ext in _db.StockOutItemExtends.AsNoTracking() on soi.Id equals ext.Id
                where !so.IsDeleted
                      && !soi.IsDeleted
                      && !ext.IsDeleted
                      && (so.Status == stockOutCompleted || so.Status == stockOutFinished)
                      && so.StockOutType == StockOutTypeCode.Sales
                      && so.SellOrderItemId == lineId
                select new
                {
                    so.Id,
                    so.StockOutCode,
                    StockOutItemId = soi.Id,
                    ext.PurchaseOrderItemId,
                    ext.PurchaseOrderItemCode,
                    ext.PurchasePriceUsd,
                    ext.QtyStockOut,
                    QtyFallback = soi.ActualQty > 0 ? soi.ActualQty : soi.Quantity
                }).ToListAsync();

            return raw
                .Select(r =>
                {
                    var qty = r.QtyStockOut > 0 ? r.QtyStockOut : r.QtyFallback;
                    return new SellOrderOutboundCostDetailLine
                    {
                        StockOutId = r.Id,
                        StockOutCode = r.StockOutCode,
                        StockOutItemId = r.StockOutItemId,
                        PurchaseOrderItemId = r.PurchaseOrderItemId,
                        PurchaseOrderItemCode = r.PurchaseOrderItemCode,
                        PurchasePriceUsd = r.PurchasePriceUsd,
                        Qty = qty,
                        CostUsd = Math.Round(qty * r.PurchasePriceUsd, 2, MidpointRounding.AwayFromZero)
                    };
                })
                .Where(l => l.Qty > 0)
                .ToList();
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

        private async Task<(SellOrderItemLineQueryRequest Request, bool MaskAmounts)> BuildItemListAnalyticsQueryRequestAsync(
            string? orderCreateStart,
            string? orderCreateEnd,
            string? customerName,
            string? salesUserName,
            string? salesUserId,
            string? purchaseUserAccount,
            string? customerId,
            string? sellOrderCode,
            string? pn,
            string? customerSo,
            string? customerPn,
            string? transactionCurrency,
            bool stockOutPending,
            bool invoicePending,
            short? purchaseProgressStatus,
            short? stockInProgressStatus,
            short? stockOutNotifyProgressStatus,
            short? stockOutProgressStatus,
            short? receiptProgressStatus,
            short? invoiceProgressStatus,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var summary = await GetPermissionSummaryAsync(userId);
            var mask521 = SaleSensitiveFieldMask521.ShouldMask(summary);
            var canViewCustomer = !mask521 && (summary?.IsSysAdmin == true
                || (summary?.PermissionCodes?.Contains("customer.info.read") ?? false));
            var canViewSalesUser = summary?.IsSysAdmin == true
                || (summary?.PermissionCodes?.Contains("sales.user.read") ?? false)
                || (summary?.PermissionCodes?.Contains("sales-order.read") ?? false);
            var canViewSalesAmount = !mask521 && (summary?.IsSysAdmin == true
                || (summary?.PermissionCodes?.Contains("sales.amount.read") ?? false));
            var maskAmounts = !canViewSalesAmount;

            var request = new SellOrderItemLineQueryRequest
            {
                OrderCreateStart = DateTime.TryParse(orderCreateStart, out var ds) ? ds : null,
                OrderCreateEnd = DateTime.TryParse(orderCreateEnd, out var de) ? de : null,
                CustomerName = canViewCustomer && !string.IsNullOrWhiteSpace(customerName) ? customerName.Trim() : null,
                SalesUserName = canViewSalesUser && !string.IsNullOrWhiteSpace(salesUserName) ? salesUserName.Trim() : null,
                SalesUserId = canViewSalesUser && !string.IsNullOrWhiteSpace(salesUserId) ? salesUserId.Trim() : null,
                PurchaseUserAccount = !string.IsNullOrWhiteSpace(purchaseUserAccount) ? purchaseUserAccount.Trim() : null,
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
                CurrentUserId = userId
            };

            return (request, maskAmounts);
        }

        private async Task<(SalesOrderQueryRequest Request, bool MaskAmounts)> BuildListAnalyticsQueryRequestAsync(
            string? keyword,
            string? code,
            string? customer,
            string? salesUserName,
            string? comment,
            short? status,
            string? startDate,
            string? endDate,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var summary = await GetPermissionSummaryAsync(userId);
            var mask521 = SaleSensitiveFieldMask521.ShouldMask(summary);
            var canViewCustomerInfo = CanViewSalesOrderCustomerInfo(summary, mask521);
            var canViewSalesAmount = !mask521 && (summary?.IsSysAdmin == true
                || (summary?.PermissionCodes?.Contains("sales.amount.read") ?? false));
            var maskAmounts = !canViewSalesAmount;

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
                CurrentUserId = userId
            };

            return (request, maskAmounts);
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
                CustomerEnglishName = canViewCustomer ? r.CustomerEnglishName : null,
                SalesUserName = mask521 ? null : r.SalesUserName,
                r.PurchaseUserAccountDisplay,
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
                        ProfitOutRateBiz = canViewSalesAmount && ext != null
                            ? SellOrderItemProfitDisplay.ResolveProfitOutRateBizForDisplay(
                                ext.ProfitOutRateBiz,
                                ext.ProfitOutBizUsd)
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
