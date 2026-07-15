using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Models.Vendor;
using CRM.Core.Utilities;
using CRM.API.Authorization;
using CRM.API.Services;
using CRM.API.Services.Interfaces;
using CRM.API.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Claims;
using System.Linq;
using System.Text.Json;
using CRM.Core.Constants;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Dtos;
using CRM.API.Models.DTOs;

namespace CRM.API.Controllers
{
    [RequirePermission("purchase-order.read")]
    [ApiController]
    [Route("api/v1/purchase-orders")]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly IPurchaseOrderService _service;
        private readonly IPurchaseOrderListQuery _purchaseOrderListQuery;
        private readonly IPurchaseOrderItemListQuery _purchaseOrderItemListQuery;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IRbacService _rbacService;
        private readonly IEntityLookupService _entityLookup;
        private readonly IEmailSender _emailSender;
        private readonly IOperationLogQueryService _operationLogQuery;
        private readonly ILogOperationAppendService _logOperationAppend;
        private readonly IStockInService _stockInService;
        private readonly IArrivalNoticeListQuery _arrivalNoticeListQuery;
        private readonly IInventoryStockItemListQuery _inventoryStockItemListQuery;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PurchaseOrdersController> _logger;

        public PurchaseOrdersController(
            IPurchaseOrderService service,
            IPurchaseOrderListQuery purchaseOrderListQuery,
            IPurchaseOrderItemListQuery purchaseOrderItemListQuery,
            IDataPermissionService dataPermissionService,
            IRbacService rbacService,
            IEntityLookupService entityLookup,
            IEmailSender emailSender,
            IOperationLogQueryService operationLogQuery,
            ILogOperationAppendService logOperationAppend,
            IStockInService stockInService,
            IArrivalNoticeListQuery arrivalNoticeListQuery,
            IInventoryStockItemListQuery inventoryStockItemListQuery,
            ApplicationDbContext db,
            ILogger<PurchaseOrdersController> logger)
        {
            _service = service;
            _purchaseOrderListQuery = purchaseOrderListQuery;
            _purchaseOrderItemListQuery = purchaseOrderItemListQuery;
            _dataPermissionService = dataPermissionService;
            _rbacService = rbacService;
            _entityLookup = entityLookup;
            _emailSender = emailSender;
            _operationLogQuery = operationLogQuery;
            _logOperationAppend = logOperationAppend;
            _stockInService = stockInService;
            _arrivalNoticeListQuery = arrivalNoticeListQuery;
            _inventoryStockItemListQuery = inventoryStockItemListQuery;
            _db = db;
            _logger = logger;
        }

        public sealed class PurchaseOrderBatchLogExportBody
        {
            public int ExportedCount { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? keyword,
            [FromQuery] string? code,
            [FromQuery] string? vendor,
            [FromQuery] string? freightForwarderOrderNo,
            [FromQuery] string? purchaseUserName,
            [FromQuery] string? comment,
            [FromQuery] short? orderType,
            [FromQuery] short? status,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new PurchaseOrderQueryRequest
                {
                    Keyword = keyword,
                    PurchaseOrderCodeFilter = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
                    VendorNameFilter = string.IsNullOrWhiteSpace(vendor) ? null : vendor.Trim(),
                    FreightForwarderOrderNoFilter = string.IsNullOrWhiteSpace(freightForwarderOrderNo) ? null : freightForwarderOrderNo.Trim(),
                    PurchaseUserNameFilter = string.IsNullOrWhiteSpace(purchaseUserName) ? null : purchaseUserName.Trim(),
                    CommentFilter = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                    OrderType = orderType,
                    Status = status,
                    StartDate = DateTime.TryParse(startDate, out var start) ? start : null,
                    EndDate = DateTime.TryParse(endDate, out var end) ? end : null,
                    Page = page,
                    PageSize = pageSize,
                    CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                };
                var result = await _service.GetPagedAsync(request);
                var summary = await GetPermissionSummaryAsync(request.CurrentUserId);
                var assistorNameMap = await BuildUserDisplayNameMapAsync(result.Items.Select(x => x.Assistor));
                var vendorMap = await LoadVendorMapForPurchaseOrdersAsync(result.Items, cancellationToken);
                var items = result.Items
                    .Select(x =>
                    {
                        VendorInfo? vendor = null;
                        var vid = x.VendorId?.Trim();
                        if (!string.IsNullOrEmpty(vid))
                            vendor = vendorMap.GetValueOrDefault(vid);
                        return MaskPurchaseOrder(
                            x,
                            summary,
                            vendor: vendor,
                            assistorUserName: ResolveAssistorDisplayName(x.Assistor, assistorNameMap));
                    })
                    .ToList();
                var aggregates = await _purchaseOrderListQuery.GetAggregatesAsync(request, cancellationToken);
                var mask511 = PurchaseSensitiveFieldMask511.ShouldMask(summary);
                var canViewPurchaseAmount = !mask511 && (summary?.IsSysAdmin == true || SummaryHasPermission(summary, "purchase.amount.read"));
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
                            pendingConfirmCount = aggregates.PendingConfirmCount,
                            inProgressCount = aggregates.InProgressCount,
                            totalAmountSum = canViewPurchaseAmount ? aggregates.TotalAmountSum : (decimal?)null
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取采购订单列表失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("analytics/dashboard")]
        public async Task<IActionResult> GetListAnalyticsDashboard(
            [FromQuery] string? keyword,
            [FromQuery] string? code,
            [FromQuery] string? vendor,
            [FromQuery] string? freightForwarderOrderNo,
            [FromQuery] string? purchaseUserName,
            [FromQuery] string? comment,
            [FromQuery] short? orderType,
            [FromQuery] short? status,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            CancellationToken cancellationToken = default)
        {
            var (request, maskAmounts) = await BuildListAnalyticsQueryRequestAsync(
                keyword, code, vendor, freightForwarderOrderNo, purchaseUserName, comment, orderType, status, startDate, endDate, cancellationToken);
            var data = await _purchaseOrderListQuery.GetListAnalyticsDashboardAsync(request, maskAmounts, cancellationToken);
            return Ok(ApiResponse<PurchaseOrderListAnalyticsDashboardDto>.Ok(data));
        }

        [HttpGet("analytics/trends")]
        public async Task<IActionResult> GetListAnalyticsTrends(
            [FromQuery] string? keyword,
            [FromQuery] string? code,
            [FromQuery] string? vendor,
            [FromQuery] string? freightForwarderOrderNo,
            [FromQuery] string? purchaseUserName,
            [FromQuery] string? comment,
            [FromQuery] short? orderType,
            [FromQuery] short? status,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] string? groupBy,
            CancellationToken cancellationToken = default)
        {
            var (request, maskAmounts) = await BuildListAnalyticsQueryRequestAsync(
                keyword, code, vendor, freightForwarderOrderNo, purchaseUserName, comment, orderType, status, startDate, endDate, cancellationToken);
            var data = await _purchaseOrderListQuery.GetListAnalyticsTrendsAsync(
                request,
                string.IsNullOrWhiteSpace(groupBy) ? "month" : groupBy.Trim(),
                maskAmounts,
                cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<PurchaseOrderListAnalyticsTrendPointDto>>.Ok(data));
        }

        [HttpGet("analytics/breakdowns")]
        public async Task<IActionResult> GetListAnalyticsBreakdowns(
            [FromQuery] string? keyword,
            [FromQuery] string? code,
            [FromQuery] string? vendor,
            [FromQuery] string? freightForwarderOrderNo,
            [FromQuery] string? purchaseUserName,
            [FromQuery] string? comment,
            [FromQuery] short? orderType,
            [FromQuery] short? status,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            CancellationToken cancellationToken = default)
        {
            var (request, maskAmounts) = await BuildListAnalyticsQueryRequestAsync(
                keyword, code, vendor, freightForwarderOrderNo, purchaseUserName, comment, orderType, status, startDate, endDate, cancellationToken);
            var data = await _purchaseOrderListQuery.GetListAnalyticsBreakdownsAsync(request, maskAmounts, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>>.Ok(data));
        }

        [HttpGet("analytics/rankings")]
        public async Task<IActionResult> GetListAnalyticsRankings(
            [FromQuery] string? keyword,
            [FromQuery] string? code,
            [FromQuery] string? vendor,
            [FromQuery] string? freightForwarderOrderNo,
            [FromQuery] string? purchaseUserName,
            [FromQuery] string? comment,
            [FromQuery] short? orderType,
            [FromQuery] short? status,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            CancellationToken cancellationToken = default)
        {
            var (request, maskAmounts) = await BuildListAnalyticsQueryRequestAsync(
                keyword, code, vendor, freightForwarderOrderNo, purchaseUserName, comment, orderType, status, startDate, endDate, cancellationToken);
            var data = await _purchaseOrderListQuery.GetListAnalyticsRankingsAsync(request, maskAmounts, cancellationToken);
            return Ok(ApiResponse<PurchaseOrderListAnalyticsRankingsDto>.Ok(data));
        }

        [HttpGet("items/analytics/dashboard")]
        public async Task<IActionResult> GetItemListAnalyticsDashboard(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] string? purchaseOrderCode,
            [FromQuery] string? freightForwarderOrderNo,
            [FromQuery] string? vendorName,
            [FromQuery] string? purchaseUserName,
            [FromQuery] string? pn,
            [FromQuery] short? orderType,
            [FromQuery] string? transactionCurrency,
            [FromQuery] short? paymentProgressStatus = null,
            [FromQuery] short? purchaseProgressStatus = null,
            [FromQuery] short? stockInProgressStatus = null,
            [FromQuery] short? invoiceProgressStatus = null,
            CancellationToken cancellationToken = default)
        {
            var (request, maskAmounts) = await BuildItemListAnalyticsQueryRequestAsync(
                startDate, endDate, purchaseOrderCode, freightForwarderOrderNo, vendorName, purchaseUserName,
                pn, orderType, transactionCurrency, paymentProgressStatus, purchaseProgressStatus,
                stockInProgressStatus, invoiceProgressStatus, cancellationToken);
            var data = await _purchaseOrderItemListQuery.GetListAnalyticsDashboardAsync(request, maskAmounts, cancellationToken);
            return Ok(ApiResponse<PurchaseOrderItemListAnalyticsDashboardDto>.Ok(data));
        }

        [HttpGet("items/analytics/trends")]
        public async Task<IActionResult> GetItemListAnalyticsTrends(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] string? purchaseOrderCode,
            [FromQuery] string? freightForwarderOrderNo,
            [FromQuery] string? vendorName,
            [FromQuery] string? purchaseUserName,
            [FromQuery] string? pn,
            [FromQuery] short? orderType,
            [FromQuery] string? transactionCurrency,
            [FromQuery] short? paymentProgressStatus = null,
            [FromQuery] short? purchaseProgressStatus = null,
            [FromQuery] short? stockInProgressStatus = null,
            [FromQuery] short? invoiceProgressStatus = null,
            [FromQuery] string? groupBy = null,
            CancellationToken cancellationToken = default)
        {
            var (request, maskAmounts) = await BuildItemListAnalyticsQueryRequestAsync(
                startDate, endDate, purchaseOrderCode, freightForwarderOrderNo, vendorName, purchaseUserName,
                pn, orderType, transactionCurrency, paymentProgressStatus, purchaseProgressStatus,
                stockInProgressStatus, invoiceProgressStatus, cancellationToken);
            var data = await _purchaseOrderItemListQuery.GetListAnalyticsTrendsAsync(
                request,
                string.IsNullOrWhiteSpace(groupBy) ? "month" : groupBy.Trim(),
                maskAmounts,
                cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<PurchaseOrderItemListAnalyticsTrendPointDto>>.Ok(data));
        }

        [HttpGet("items/analytics/breakdowns")]
        public async Task<IActionResult> GetItemListAnalyticsBreakdowns(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] string? purchaseOrderCode,
            [FromQuery] string? freightForwarderOrderNo,
            [FromQuery] string? vendorName,
            [FromQuery] string? purchaseUserName,
            [FromQuery] string? pn,
            [FromQuery] short? orderType,
            [FromQuery] string? transactionCurrency,
            [FromQuery] short? paymentProgressStatus = null,
            [FromQuery] short? purchaseProgressStatus = null,
            [FromQuery] short? stockInProgressStatus = null,
            [FromQuery] short? invoiceProgressStatus = null,
            CancellationToken cancellationToken = default)
        {
            var (request, maskAmounts) = await BuildItemListAnalyticsQueryRequestAsync(
                startDate, endDate, purchaseOrderCode, freightForwarderOrderNo, vendorName, purchaseUserName,
                pn, orderType, transactionCurrency, paymentProgressStatus, purchaseProgressStatus,
                stockInProgressStatus, invoiceProgressStatus, cancellationToken);
            var data = await _purchaseOrderItemListQuery.GetListAnalyticsBreakdownsAsync(request, maskAmounts, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>>.Ok(data));
        }

        [HttpGet("items/analytics/rankings")]
        public async Task<IActionResult> GetItemListAnalyticsRankings(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] string? purchaseOrderCode,
            [FromQuery] string? freightForwarderOrderNo,
            [FromQuery] string? vendorName,
            [FromQuery] string? purchaseUserName,
            [FromQuery] string? pn,
            [FromQuery] short? orderType,
            [FromQuery] string? transactionCurrency,
            [FromQuery] short? paymentProgressStatus = null,
            [FromQuery] short? purchaseProgressStatus = null,
            [FromQuery] short? stockInProgressStatus = null,
            [FromQuery] short? invoiceProgressStatus = null,
            CancellationToken cancellationToken = default)
        {
            var (request, maskAmounts) = await BuildItemListAnalyticsQueryRequestAsync(
                startDate, endDate, purchaseOrderCode, freightForwarderOrderNo, vendorName, purchaseUserName,
                pn, orderType, transactionCurrency, paymentProgressStatus, purchaseProgressStatus,
                stockInProgressStatus, invoiceProgressStatus, cancellationToken);
            var data = await _purchaseOrderItemListQuery.GetListAnalyticsRankingsAsync(request, maskAmounts, cancellationToken);
            return Ok(ApiResponse<PurchaseOrderItemListAnalyticsRankingsDto>.Ok(data));
        }

        /// <summary>采购订单明细行列表（数据库分页；与前端 <c>/purchase-order-items</c> 对齐）。</summary>
        [HttpGet("items")]
        public async Task<IActionResult> GetPurchaseOrderItemLines(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] string? purchaseOrderCode,
            [FromQuery] string? freightForwarderOrderNo,
            [FromQuery] string? vendorName,
            [FromQuery] string? purchaseUserName,
            [FromQuery] string? pn,
            [FromQuery] short? orderType,
            [FromQuery] string? transactionCurrency,
            [FromQuery] short? paymentProgressStatus = null,
            [FromQuery] short? purchaseProgressStatus = null,
            [FromQuery] short? stockInProgressStatus = null,
            [FromQuery] short? invoiceProgressStatus = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var summary = await GetPermissionSummaryAsync(userId);
                var mask511 = PurchaseSensitiveFieldMask511.ShouldMask(summary);
                var canViewVendorInfo = !mask511 && (summary?.IsSysAdmin == true
                    || SummaryHasPermission(summary, "vendor.info.read")
                    || SummaryHasPermission(summary, "vendor.read")
                    || SummaryHasPermission(summary, "purchase-order.read")
                    || SummaryHasPermission(summary, "purchase-order.write"));
                var canViewPurchaseUser = summary?.IsSysAdmin == true
                    || SummaryHasPermission(summary, "purchase.user.read")
                    || SummaryHasPermission(summary, "purchase-order.read");

                var request = new PurchaseOrderItemListQueryRequest
                {
                    CurrentUserId = userId,
                    StartDate = DateTime.TryParse(startDate, out var sd) ? sd : null,
                    EndDate = DateTime.TryParse(endDate, out var ed) ? ed : null,
                    PurchaseOrderCode = string.IsNullOrWhiteSpace(purchaseOrderCode) ? null : purchaseOrderCode.Trim(),
                    FreightForwarderOrderNo = string.IsNullOrWhiteSpace(freightForwarderOrderNo) ? null : freightForwarderOrderNo.Trim(),
                    VendorName = canViewVendorInfo && !string.IsNullOrWhiteSpace(vendorName) ? vendorName.Trim() : null,
                    PurchaseUserName = canViewPurchaseUser && !string.IsNullOrWhiteSpace(purchaseUserName) ? purchaseUserName.Trim() : null,
                    Pn = string.IsNullOrWhiteSpace(pn) ? null : pn.Trim(),
                    OrderType = orderType,
                    TransactionCurrency = transactionCurrency,
                    PaymentProgressStatus = paymentProgressStatus,
                    PurchaseProgressStatus = purchaseProgressStatus,
                    StockInProgressStatus = stockInProgressStatus,
                    InvoiceProgressStatus = invoiceProgressStatus,
                    Page = page,
                    PageSize = pageSize
                };

                var result = await _purchaseOrderItemListQuery.GetPagedAsync(request, cancellationToken);
                var loginMap = await LoadCreateUserLoginNamesForPoLinesAsync(result.Items, cancellationToken);
                var paymentRequestFlags = await LoadPoItemIdsWithActivePaymentRequestAsync(result.Items, cancellationToken);
                IReadOnlyDictionary<string, string> vendorEnglishMap = canViewVendorInfo
                    ? await LoadVendorEnglishNameMapForPoLinesAsync(result.Items, cancellationToken)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var items = MapPurchaseOrderItemListLines(result.Items, summary, loginMap, paymentRequestFlags, vendorEnglishMap);
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
                _logger.LogError(ex, "获取采购订单明细行列表失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>采购订单报表页：一次返回订单详情（含供应商扩展字段）与公司参数，避免单独请求 company-profile/report-bundle。</summary>
        [HttpGet("{id:guid}/report-data")]
        public async Task<IActionResult> GetReportData(string id, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _service.GetByIdAsync(id);
                if (order == null) return NotFound(new { success = false, message = "采购订单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessPurchaseOrderAsync(userId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该采购订单" });
                var summary = await GetPermissionSummaryAsync(userId);
                VendorContactInfo? contact = null;
                VendorInfo? vendor = null;
                var canVendor = summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("vendor.info.read") ?? false);
                if (canVendor)
                {
                    if (!string.IsNullOrWhiteSpace(order.VendorContactId))
                        contact = await _entityLookup.GetVendorContactByIdAsync(order.VendorContactId, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(order.VendorId))
                        vendor = await _entityLookup.GetVendorByIdAsync(order.VendorId, cancellationToken);
                }
                var companyProfile = await CompanyProfileBundleLoader.LoadAsync(_db, _logger, cancellationToken);
                var reportItemExtends = await LoadPoItemExtendsAsync(order.Items, cancellationToken);
                var sellOrderItemCodes = await LoadSellOrderItemCodesAsync(order.Items, cancellationToken);
                var assistorNameMap = await BuildUserDisplayNameMapAsync(new[] { order.Assistor });
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        order = MaskPurchaseOrder(
                            order,
                            summary,
                            contact,
                            vendor,
                            reportItemExtends,
                            sellOrderItemCodes,
                            ResolveAssistorDisplayName(order.Assistor, assistorNameMap)),
                        companyProfile
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>采购订单详情页：底部页签下游列表（采购申请/付款/到货通知/入库/库存/进项发票）。</summary>
        [HttpGet("{id:guid}/detail-tab-aggregates")]
        public async Task<IActionResult> GetDetailTabAggregates(string id)
        {
            try
            {
                var order = await _service.GetByIdAsync(id);
                if (order == null) return NotFound(new { success = false, message = "采购订单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessPurchaseOrderAsync(userId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该采购订单" });
                var summary = await GetPermissionSummaryAsync(userId);
                var mask511 = PurchaseSensitiveFieldMask511.ShouldMask(summary);
                var mask521 = SaleSensitiveFieldMask521.ShouldMask(summary);

                var data = await BuildPurchaseOrderDetailTabAggregatesPayloadAsync(id, order, purchaseOrderItemIdScope: null, mask511, mask521, userId);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取采购订单页签数据失败: {Id}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>采购订单「单条明细」下游列表：与 <c>detail-tab-aggregates</c> 字段一致，按采购明细主键过滤。</summary>
        [HttpGet("{id:guid}/purchase-order-items/{purchaseOrderItemId}/detail-tab-aggregates")]
        public async Task<IActionResult> GetPurchaseOrderItemDetailTabAggregates(string id, string purchaseOrderItemId)
        {
            try
            {
                var lineId = (purchaseOrderItemId ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(lineId))
                    return BadRequest(new { success = false, message = "采购订单明细主键无效" });

                var order = await _service.GetByIdAsync(id);
                if (order == null) return NotFound(new { success = false, message = "采购订单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessPurchaseOrderAsync(userId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该采购订单" });

                var orderLineIds = (order.Items ?? new List<PurchaseOrderItem>())
                    .Select(i => i.Id)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (!orderLineIds.Contains(lineId, StringComparer.OrdinalIgnoreCase))
                    return NotFound(new { success = false, message = "采购订单明细不属于该订单" });

                var summary = await GetPermissionSummaryAsync(userId);
                var mask511 = PurchaseSensitiveFieldMask511.ShouldMask(summary);
                var mask521 = SaleSensitiveFieldMask521.ShouldMask(summary);

                var data = await BuildPurchaseOrderDetailTabAggregatesPayloadAsync(id, order, lineId, mask511, mask521, userId);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取采购订单明细页签数据失败: {OrderId} {ItemId}", id, purchaseOrderItemId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <param name="purchaseOrderItemIdScope">非 null 时：采购申请仅保留该明细关联销售行；付款明细仅保留该行；到货通知按采购明细过滤；入库/库存/进项发票链仅使用该明细。</param>
        private async Task<object> BuildPurchaseOrderDetailTabAggregatesPayloadAsync(
            string purchaseOrderId,
            PurchaseOrder order,
            string? purchaseOrderItemIdScope,
            bool mask511,
            bool mask521,
            string? currentUserId = null)
        {
            var allItems = order.Items ?? new List<PurchaseOrderItem>();
            List<string> poItemIds;
            List<string> soItemIds;

            if (!string.IsNullOrWhiteSpace(purchaseOrderItemIdScope))
            {
                var scope = purchaseOrderItemIdScope.Trim();
                var line = allItems.FirstOrDefault(i =>
                    !string.IsNullOrWhiteSpace(i.Id) &&
                    string.Equals(i.Id.Trim(), scope, StringComparison.OrdinalIgnoreCase));
                if (line == null)
                    throw new InvalidOperationException("采购订单明细不属于该订单");

                poItemIds = new List<string> { scope };
                var so = line.SellOrderItemId?.Trim();
                soItemIds = string.IsNullOrEmpty(so) ? new List<string>() : new List<string> { so };
            }
            else
            {
                poItemIds = allItems
                    .Select(i => i.Id)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                soItemIds = allItems
                    .Select(i => i.SellOrderItemId)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            var prRows = soItemIds.Count == 0
                ? new List<object>()
                : (await (
                        from pr in _db.PurchaseRequisitions.AsNoTracking()
                        join so in _db.SellOrders.AsNoTracking() on pr.SellOrderId equals so.Id into soJoin
                        from so in soJoin.DefaultIfEmpty()
                        where soItemIds.Contains(pr.SellOrderItemId)
                        orderby pr.CreateTime descending
                        select new
                        {
                            id = pr.Id,
                            billCode = pr.BillCode,
                            pr.Status,
                            sellOrderItemId = pr.SellOrderItemId,
                            sellOrderId = pr.SellOrderId,
                            sellOrderCode = so != null ? so.SellOrderCode : null,
                            salesUserName = so != null ? so.SalesUserName : null,
                            pr.PN,
                            pr.Brand,
                            pr.Qty,
                            pr.ExpectedPurchaseTime,
                            pr.CreateTime
                        })
                    .ToListAsync()).Cast<object>().ToList();

            List<string> payHeaderIds;
            if (!string.IsNullOrWhiteSpace(purchaseOrderItemIdScope))
            {
                payHeaderIds = await _db.FinancePaymentItems.AsNoTracking()
                    .Where(x => x.PurchaseOrderItemId != null && poItemIds.Contains(x.PurchaseOrderItemId))
                    .Select(x => x.FinancePaymentId)
                    .Distinct()
                    .ToListAsync();
            }
            else
            {
                payHeaderIds = await _db.FinancePaymentItems.AsNoTracking()
                    .Where(x => x.PurchaseOrderId == purchaseOrderId || (x.PurchaseOrderItemId != null && poItemIds.Contains(x.PurchaseOrderItemId)))
                    .Select(x => x.FinancePaymentId)
                    .Distinct()
                    .ToListAsync();
            }

            var paymentRows = await _db.FinancePayments.AsNoTracking()
                .Where(x => payHeaderIds.Contains(x.Id))
                .OrderByDescending(x => x.CreateTime)
                .Select(x => new
                {
                    id = x.Id,
                    financePaymentCode = x.FinancePaymentCode,
                    vendorName = mask511 ? null : x.VendorName,
                    x.Status,
                    paymentAmountToBe = x.PaymentAmountToBe,
                    paymentAmount = x.PaymentAmount,
                    x.PaymentCurrency,
                    x.PaymentDate,
                    createByUserId = x.CreateByUserId,
                    x.CreateTime
                })
                .ToListAsync();

            var noticeRows = await BuildPurchaseOrderTabArrivalNoticesAsync(poItemIds, mask511, currentUserId);

            var stockInIds = poItemIds.Count == 0
                ? new List<string>()
                : await _db.StockInItemExtends.AsNoTracking()
                    .Where(x => x.PurchaseOrderItemId != null && poItemIds.Contains(x.PurchaseOrderItemId))
                    .Select(x => x.StockInId)
                    .Distinct()
                    .ToListAsync();

            var stockInRows = await BuildPurchaseOrderTabStockInsAsync(poItemIds, mask511);

            var stockItemRows = await BuildPurchaseOrderTabStockItemsAsync(poItemIds, mask511, mask521, currentUserId);

            var invHeaderIds = stockInIds.Count == 0
                ? new List<string>()
                : await _db.FinancePurchaseInvoiceItems.AsNoTracking()
                    .Where(x => x.StockInId != null && stockInIds.Contains(x.StockInId))
                    .Select(x => x.FinancePurchaseInvoiceId)
                    .Distinct()
                    .ToListAsync();
            var invoiceRows = await _db.FinancePurchaseInvoices.AsNoTracking()
                .Where(x => invHeaderIds.Contains(x.Id))
                .OrderByDescending(x => x.CreateTime)
                .Select(x => new
                {
                    id = x.Id,
                    vendorName = mask511 ? null : x.VendorName,
                    x.InvoiceNo,
                    invoiceAmount = mask511 ? 0m : x.InvoiceAmount,
                    x.InvoiceDate,
                    x.ConfirmStatus,
                    x.RedInvoiceStatus,
                    x.CreateTime
                })
                .ToListAsync();

            List<object> qcImageRows;
            if (poItemIds.Count == 0)
            {
                qcImageRows = new List<object>();
            }
            else
            {
                var notifyIds = await _db.StockInNotifies.AsNoTracking()
                    .Where(x => x.PurchaseOrderId == purchaseOrderId && poItemIds.Contains(x.PurchaseOrderItemId))
                    .Select(x => x.Id)
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

            object? lineOverview = null;
            if (!string.IsNullOrWhiteSpace(purchaseOrderItemIdScope))
            {
                lineOverview = await BuildPurchaseOrderLineOverviewAsync(
                    purchaseOrderItemIdScope.Trim(),
                    mask511);
            }

            return new
            {
                purchaseRequisitions = prRows,
                payments = paymentRows,
                arrivalNotices = noticeRows,
                stockIns = stockInRows,
                stockItems = stockItemRows,
                purchaseInvoices = invoiceRows,
                qcImages = qcImageRows,
                lineOverview
            };
        }

        /// <summary>
        /// 采购明细下游「到货通知」：按 <c>purchase_order_item_id</c> 关联到货通知，字段与到货通知列表（<see cref="StockInNotify"/> 列表序列化）一致。
        /// </summary>
        private async Task<List<object>> BuildPurchaseOrderTabArrivalNoticesAsync(
            IReadOnlyList<string> poItemIds,
            bool mask511,
            string? currentUserId)
        {
            if (poItemIds.Count == 0)
                return new List<object>();

            var orderedIds = await _db.StockInNotifies.AsNoTracking()
                .Where(x => !x.IsDeleted && poItemIds.Contains(x.PurchaseOrderItemId))
                .OrderByDescending(x => x.CreateTime)
                .ThenByDescending(x => x.Id)
                .Select(x => x.Id)
                .ToListAsync();

            if (orderedIds.Count == 0)
                return new List<object>();

            var rows = await _arrivalNoticeListQuery.GetByIdsAsync(
                orderedIds,
                currentUserId,
                applyDataScope: false);

            if (mask511)
                PurchaseSensitiveFieldMask511.ApplyStockInNotifies(rows, true);

            return rows.Select(n => (object)new
            {
                id = n.Id,
                noticeCode = n.NoticeCode,
                purchaseOrderId = n.PurchaseOrderId,
                purchaseOrderCode = n.PurchaseOrderCode,
                freightForwarderOrderNo = n.FreightForwarderOrderNo,
                purchaseOrderItemId = n.PurchaseOrderItemId,
                sellOrderItemId = n.SellOrderItemId,
                vendorId = n.VendorId,
                vendorName = n.VendorName,
                vendorEnglishName = n.VendorEnglishName,
                vendorCode = n.VendorCode,
                purchaseUserName = n.PurchaseUserName,
                status = n.Status,
                expectedArrivalDate = n.ExpectedArrivalDate,
                regionType = n.RegionType,
                stockInType = n.StockInType,
                pn = n.Pn,
                brand = n.Brand,
                expectQty = n.ExpectQty,
                receiveQty = n.ReceiveQty,
                passedQty = n.PassedQty,
                remark = n.Remark,
                shipmentMethod = n.ShipmentMethod,
                courierTrackingNo = n.CourierTrackingNo,
                expressCompany = n.ExpressCompany,
                customsDeclarationId = n.CustomsDeclarationId,
                customsDeclarationCode = n.CustomsDeclarationCode,
                createTime = n.CreateTime,
                items = new[]
                {
                    new
                    {
                        id = n.Id,
                        stockInNotifyId = n.Id,
                        purchaseOrderItemId = n.PurchaseOrderItemId,
                        pn = n.Pn,
                        brand = n.Brand,
                        qty = n.ExpectQty,
                        arrivedQty = n.ReceiveQty,
                        passedQty = n.PassedQty
                    }
                }
            }).ToList();
        }

        /// <summary>
        /// 采购明细下游「入库」：经入库明细扩展 <c>purchase_order_item_id</c> 反查入库单，字段与入库单列表（<see cref="StockInListItemDto"/>）一致。
        /// </summary>
        private async Task<List<object>> BuildPurchaseOrderTabStockInsAsync(IReadOnlyList<string> poItemIds, bool mask511)
        {
            if (poItemIds.Count == 0)
                return new List<object>();

            var stockInIdList = await _db.StockInItemExtends.AsNoTracking()
                .Where(e => e.PurchaseOrderItemId != null && poItemIds.Contains(e.PurchaseOrderItemId!))
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
        /// 采购明细下游「库存」：按 <c>purchase_order_item_id</c> 关联库存明细，字段与全库库存明细列表（<see cref="InventoryStockItemListRowDto"/>）一致。
        /// </summary>
        private async Task<List<object>> BuildPurchaseOrderTabStockItemsAsync(
            IReadOnlyList<string> poItemIds,
            bool mask511,
            bool mask521,
            string? currentUserId)
        {
            if (poItemIds.Count == 0)
                return new List<object>();

            var orderedIds = await _db.StockItems.AsNoTracking()
                .Where(x =>
                    x.PurchaseOrderItemId != null &&
                    poItemIds.Contains(x.PurchaseOrderItemId) &&
                    (x.TransferType == null || x.TransferType != StockItemTransferTypeCodes.ManualTransferSource))
                .OrderByDescending(x => x.CreateTime)
                .ThenByDescending(x => x.Id)
                .Select(x => x.Id)
                .ToListAsync();

            if (orderedIds.Count == 0)
                return new List<object>();

            var rows = await _inventoryStockItemListQuery.GetByIdsAsync(
                orderedIds,
                currentUserId,
                applyDataScope: false);

            if (mask511)
                PurchaseSensitiveFieldMask511.ApplyInventoryStockItemListRows(rows, true);
            if (mask521)
                SaleSensitiveFieldMask521.ApplyInventoryStockItemListRows(rows, true);

            return rows.Select(r => (object)MapPurchaseOrderTabStockItemRow(r)).ToList();
        }

        private static object MapPurchaseOrderTabStockItemRow(InventoryStockItemListRowDto r) =>
            new
            {
                stockItemId = r.StockItemId,
                stockItemCode = r.StockItemCode,
                stockInItemId = r.StockInItemId,
                stockInItemCode = r.StockInItemCode,
                stockInId = r.StockInId,
                stockInCode = r.StockInCode,
                stockInDate = r.StockInDate,
                materialId = r.MaterialId,
                locationId = r.LocationId,
                batchNo = r.BatchNo,
                productionDate = r.ProductionDate,
                purchasePn = r.PurchasePn,
                purchaseBrand = r.PurchaseBrand,
                freightForwarderOrderNo = r.FreightForwarderOrderNo,
                purchaseOrderItemCode = r.PurchaseOrderItemCode,
                sellOrderItemCode = r.SellOrderItemCode,
                qtyInbound = r.QtyInbound,
                qtyStockOut = r.QtyStockOut,
                qtyRepertory = r.QtyRepertory,
                qtyRepertoryAvailable = r.QtyRepertoryAvailable,
                qtyOccupy = r.QtyOccupy,
                qtySales = r.QtySales,
                purchasePrice = r.PurchasePrice,
                purchaseCurrency = r.PurchaseCurrency,
                purchasePriceUsd = r.PurchasePriceUsd,
                salesPrice = r.SalesPrice,
                salesCurrency = r.SalesCurrency,
                salesPriceUsd = r.SalesPriceUsd,
                vendorId = r.VendorId,
                vendorName = r.VendorName,
                vendorEnglishName = r.VendorEnglishName,
                vendorCode = r.VendorCode,
                customerId = r.CustomerId,
                customerName = r.CustomerName,
                regionType = r.RegionType,
                stockType = r.StockType,
                stockInType = r.StockInType,
                customerPn = r.CustomerPn,
                customerBrand = r.CustomerBrand,
                purchaserName = r.PurchaserName,
                salespersonName = r.SalespersonName,
                createTime = r.CreateTime,
                stockAggregateId = r.StockAggregateId,
                warehouseId = r.WarehouseId,
                warehouseCode = r.WarehouseCode,
                warehouseName = r.WarehouseName,
                outboundStatus = r.OutboundStatus,
                profitOutBizUsd = r.ProfitOutBizUsd
            };

        /// <summary>采购订单明细详情「概况」页签：4×7 执行进度矩阵（仅单条明细 scope 时返回）。</summary>
        private async Task<object?> BuildPurchaseOrderLineOverviewAsync(
            string lineId,
            bool mask511)
        {
            var poItem = await _db.PurchaseOrderItems.AsNoTracking()
                .Where(i => i.Id == lineId)
                .Select(i => new { i.Qty, i.Cost, i.Currency })
                .FirstOrDefaultAsync();
            if (poItem == null)
                return null;

            var ext = await _db.PurchaseOrderItemExtends.AsNoTracking()
                .Where(e => e.Id == lineId)
                .Select(e => new
                {
                    e.QtyStockInNotifyExpectSum,
                    e.QtyReceiveTotal,
                    e.PaymentAmount,
                    e.PaymentAmountFinish,
                    e.PaymentAmountNot,
                    e.PurchaseInvoiceAmount,
                    e.PurchaseInvoiceDone,
                    e.PurchaseInvoiceToBe
                })
                .FirstOrDefaultAsync();

            var qtyLine = poItem.Qty;
            var lineAmount = Math.Round(qtyLine * poItem.Cost, 2, MidpointRounding.AwayFromZero);
            var currency = poItem.Currency;

            var arrivalDone = ext?.QtyStockInNotifyExpectSum ?? 0m;
            var arrivalPending = Math.Max(0m, qtyLine - arrivalDone);
            var stockInDone = ext?.QtyReceiveTotal ?? 0m;
            var stockInPending = Math.Max(0m, qtyLine - stockInDone);

            var paymentTotal = ext?.PaymentAmount ?? lineAmount;
            var paymentDone = ext?.PaymentAmountFinish ?? 0m;
            var paymentPending = ext?.PaymentAmountNot ?? Math.Max(0m, paymentTotal - paymentDone);
            var invoiceTotal = ext?.PurchaseInvoiceAmount ?? lineAmount;
            var invoiceDone = ext?.PurchaseInvoiceDone ?? 0m;
            var invoicePending = ext?.PurchaseInvoiceToBe ?? Math.Max(0m, invoiceTotal - invoiceDone);

            if (mask511)
            {
                lineAmount = 0m;
                paymentTotal = 0m;
                paymentDone = 0m;
                paymentPending = 0m;
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
                payment = AmtMetric(paymentTotal, paymentDone, paymentPending, currency),
                arrivalNotice = QtyMetric(qtyLine, arrivalDone, arrivalPending),
                stockIn = QtyMetric(qtyLine, stockInDone, stockInPending),
                purchaseInvoice = AmtMetric(invoiceTotal, invoiceDone, invoicePending, currency)
            };
        }

        /// <summary>采购订单主表字段变更日志（log_change_fldval）。</summary>
        [HttpGet("{id:guid}/change-logs")]
        public async Task<IActionResult> GetChangeLogs(string id)
        {
            try
            {
                var order = await _service.GetByIdAsync(id);
                if (order == null) return NotFound(new { success = false, message = "采购订单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessPurchaseOrderAsync(userId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该采购订单" });
                var logs = await _service.GetFieldChangeLogsAsync(id);
                return Ok(new { success = true, data = logs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取采购订单变更日志失败: {Id}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>已软删除的采购订单明细行。</summary>
        [HttpGet("{id:guid}/deleted-items")]
        public async Task<IActionResult> GetDeletedItems(string id)
        {
            try
            {
                var order = await _service.GetByIdAsync(id);
                if (order == null) return NotFound(new { success = false, message = "采购订单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessPurchaseOrderAsync(userId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该采购订单" });
                var items = await _service.GetDeletedOrderItemsAsync(id);
                return Ok(new { success = true, data = items });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取采购订单删除明细失败: {Id}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>采购订单详情「入库批次」面板导出记录（仅 PO 页发起的导出）。</summary>
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
                if (order == null) return NotFound(new { success = false, message = "采购订单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessPurchaseOrderAsync(userId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该采购订单" });

                var data = await _operationLogQuery.QueryAsync(new OperationLogQuery
                {
                    BizType = BusinessLogTypes.PurchaseOrder,
                    RecordId = id.Trim(),
                    ActionType = PurchaseOrderBatchExportActionTypes.Export,
                    Page = page,
                    PageSize = pageSize
                }, cancellationToken);

                return Ok(new { success = true, data, message = "ok" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取采购订单入库批次导出记录失败 PurchaseOrderId={Id}", id);
                return StatusCode(500, new { success = false, message = "获取导出记录失败" });
            }
        }

        /// <summary>记录采购订单详情页导出入库批次 CSV 的操作日志。</summary>
        [HttpPost("{id:guid}/batch-log-export")]
        public async Task<IActionResult> LogBatchExport(
            string id,
            [FromBody] PurchaseOrderBatchLogExportBody body,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var order = await _service.GetByIdAsync(id);
                if (order == null) return NotFound(new { success = false, message = "采购订单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessPurchaseOrderAsync(userId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该采购订单" });

                var count = Math.Max(0, body?.ExportedCount ?? 0);
                var code = (order.PurchaseOrderCode ?? string.Empty).Trim();
                var desc = string.IsNullOrEmpty(code)
                    ? $"导出采购订单入库批次 {count} 条"
                    : $"导出采购订单 {code} 入库批次 {count} 条";
                var extraInfo = JsonSerializer.Serialize(new { exportedCount = count });

                await _logOperationAppend.AppendAsync(
                    BusinessLogTypes.PurchaseOrder,
                    order.Id,
                    code,
                    PurchaseOrderBatchExportActionTypes.Export,
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
                _logger.LogError(ex, "记录采购订单入库批次导出日志失败 PurchaseOrderId={Id}", id);
                return StatusCode(500, new { success = false, message = "记录导出日志失败" });
            }
        }

        /// <summary>按主键获取采购订单详情；使用 <c>{id:guid}</c> 避免与字面路由 <c>items</c> 冲突。</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _service.GetByIdAsync(id);
                if (order == null) return NotFound(new { success = false, message = "采购订单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessPurchaseOrderAsync(userId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该采购订单" });
                var summary = await GetPermissionSummaryAsync(userId);
                VendorContactInfo? contact = null;
                VendorInfo? vendor = null;
                var canVendor = summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("vendor.info.read") ?? false);
                if (canVendor)
                {
                    if (!string.IsNullOrWhiteSpace(order.VendorContactId))
                        contact = await _entityLookup.GetVendorContactByIdAsync(order.VendorContactId, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(order.VendorId))
                        vendor = await _entityLookup.GetVendorByIdAsync(order.VendorId, cancellationToken);
                }
                var detailItemExtends = await LoadPoItemExtendsAsync(order.Items, cancellationToken);
                var sellOrderItemCodes = await LoadSellOrderItemCodesAsync(order.Items, cancellationToken);
                var assistorNameMap = await BuildUserDisplayNameMapAsync(new[] { order.Assistor });
                return Ok(new
                {
                    success = true,
                    data = MaskPurchaseOrder(
                        order,
                        summary,
                        contact,
                        vendor,
                        detailItemExtends,
                        sellOrderItemCodes,
                        ResolveAssistorDisplayName(order.Assistor, assistorNameMap))
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>将当前预览的采购订单 PDF（Base64）作为附件发送至指定邮箱。</summary>
        [HttpPost("{id:guid}/report/send-email")]
        public async Task<IActionResult> SendReportEmail(string id, [FromBody] SendPurchaseOrderReportEmailRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.To))
                    return BadRequest(new { success = false, message = "收件人邮箱不能为空" });
                if (string.IsNullOrWhiteSpace(request.PdfBase64))
                    return BadRequest(new { success = false, message = "PDF 内容不能为空" });

                var order = await _service.GetByIdAsync(id);
                if (order == null) return NotFound(new { success = false, message = "采购订单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessPurchaseOrderAsync(userId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该采购订单" });

                var raw = request.PdfBase64.Trim();
                var comma = raw.IndexOf(',');
                if (raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0)
                    raw = raw[(comma + 1)..];

                byte[] pdfBytes;
                try
                {
                    pdfBytes = Convert.FromBase64String(raw);
                }
                catch (FormatException)
                {
                    return BadRequest(new { success = false, message = "PDF 数据格式无效" });
                }

                const int maxBytes = 25 * 1024 * 1024;
                if (pdfBytes.Length > maxBytes)
                    return BadRequest(new { success = false, message = "附件过大" });

                var fileName = string.IsNullOrWhiteSpace(request.FileName) ? $"{order.PurchaseOrderCode}.pdf" : request.FileName!.Trim();
                if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    fileName += ".pdf";

                var subject = string.IsNullOrWhiteSpace(request.Subject)
                    ? $"采购订单 {order.PurchaseOrderCode}"
                    : request.Subject!.Trim();

                await _emailSender.SendWithAttachmentAsync(
                    request.To.Trim(),
                    subject,
                    request.Body,
                    pdfBytes,
                    fileName,
                    "application/pdf",
                    cancellationToken);

                return Ok(new { success = true, message = "邮件已发送" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送采购订单报表邮件失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("by-sell-order/{sellOrderCode}")]
        public async Task<IActionResult> GetBySellOrder(string sellOrderCode)
        {
            try
            {
                var orders = (await _service.GetBySellOrderCodeAsync(sellOrderCode)).ToList();
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var summary = await GetPermissionSummaryAsync(userId);
                var assistorNameMap = await BuildUserDisplayNameMapAsync(orders.Select(x => x.Assistor));
                return Ok(new
                {
                    success = true,
                    data = orders
                        .Select(x => MaskPurchaseOrder(
                            x,
                            summary,
                            assistorUserName: ResolveAssistorDisplayName(x.Assistor, assistorNameMap)))
                        .ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderRequest request)
        {
            var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(actorId))
                return Unauthorized(new { success = false, message = "未登录或登录态失效" });
            var createSummary = await GetPermissionSummaryAsync(actorId);
            if (createSummary == null || !PurchaseOrderCreateGate.CanCreate(createSummary))
            {
                return StatusCode(403,
                    new
                    {
                        success = false,
                        message =
                            "无权限创建采购订单。请满足其一：purchase-order.write；或 purchase-requisition.write 且（主部门为采购/采购助理、或具备 purchase-order.read、或角色含 purchase_buyer / purchase_operator / purchase_ops_operator）。"
                    });
            }

            try
            {
                _logger.LogInformation(
                    "PurchaseOrders Create 入口: Type={Type} ItemCount={ItemCount} VendorId={VendorId} PurchaseUserId={PurchaseUserId} ActorId={ActorId}",
                    request.Type, request.Items?.Count ?? 0, request.VendorId, request.PurchaseUserId ?? "(null)", actorId ?? "(null)");
                var order = await _service.CreateAsync(request, actorId);
                _logger.LogInformation("PurchaseOrders Create 成功: Id={Id} Code={Code}", order.Id, order.PurchaseOrderCode);
                return CreatedAtAction(nameof(GetById), new { id = order.Id },
                    new { success = true, data = order });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "PurchaseOrders Create 参数错误: {Message}", ex.Message);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "PurchaseOrders Create 冲突: {Message}", ex.Message);
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "创建采购订单失败: {Message}; Inner={Inner}",
                    ex.Message, ex.InnerException?.Message);
                return StatusCode(500, new { success = false, message = PersistErrorMessage(ex) });
            }
        }

        [HttpPut("{id:guid}")]
        [RequirePermission("purchase-order.write")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdatePurchaseOrderRequest request)
        {
            try
            {
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var existing = await _service.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new { success = false, message = "采购订单不存在" });
                if (!string.IsNullOrWhiteSpace(actorId)
                    && !await _dataPermissionService.CanAccessPurchaseOrderAsync(actorId, existing))
                    return StatusCode(403, new { success = false, message = "无权限编辑该采购订单" });

                _logger.LogInformation(
                    "PurchaseOrders Update 入口: Id={Id} ItemCount={ItemCount} ActorId={ActorId}",
                    id, request.Items?.Count ?? 0, actorId ?? "(null)");
                var order = await _service.UpdateAsync(id, request, actorId);
                return Ok(new { success = true, data = order });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "PurchaseOrders Update 未找到: {Message}", ex.Message);
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PurchaseOrders Update 失败: {Message}; Inner={Inner}", ex.Message, ex.InnerException?.Message);
                return StatusCode(500, new { success = false, message = PersistErrorMessage(ex) });
            }
        }

        [HttpDelete("{id:guid}")]
        [RequirePermission("purchase-order.write")]
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
        [RequirePermission("purchase-order.write")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] PurchaseOrderUpdateStatusRequest request)
        {
            try
            {
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.UpdateStatusAsync(id, request.Status, actorId);
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

        public sealed class UpdateFreightForwarderOrderNoRequest
        {
            public string? FreightForwarderOrderNo { get; set; }
        }

        /// <summary>物流录入/修改/清空货代单号（审核通过后；系统内唯一）。</summary>
        [HttpPatch("{id:guid}/freight-forwarder-order-no")]
        public async Task<IActionResult> UpdateFreightForwarderOrderNo(
            string id,
            [FromBody] UpdateFreightForwarderOrderNoRequest request)
        {
            try
            {
                if (!await LogisticsDataAccessHttp.CanWriteAsync(_rbacService, User))
                    return StatusCode(403, new { success = false, message = "当前账号无物流写权限，无法录入货代单号" });

                var order = await _service.GetByIdAsync(id);
                if (order == null)
                    return NotFound(new { success = false, message = "采购订单不存在" });

                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(actorId) && !await _dataPermissionService.CanAccessPurchaseOrderAsync(actorId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该采购订单" });

                var updated = await _service.UpdateFreightForwarderOrderNoAsync(
                    id,
                    request?.FreightForwarderOrderNo,
                    actorId);
                var summary = await GetPermissionSummaryAsync(actorId);
                var assistorNameMap = await BuildUserDisplayNameMapAsync(new[] { updated.Assistor });
                return Ok(new
                {
                    success = true,
                    data = MaskPurchaseOrder(updated, summary, assistorUserName: ResolveAssistorDisplayName(updated.Assistor, assistorNameMap)),
                    message = "货代单号已保存"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新货代单号失败: {Id}", id);
                return StatusCode(500, new { success = false, message = PersistErrorMessage(ex) });
            }
        }

        [HttpPost("{id:guid}/refresh-item-extends")]
        [RequirePermission("purchase-order.write")]
        public async Task<IActionResult> RefreshItemExtends(string id, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _service.GetByIdAsync(id);
                if (order == null) return NotFound(new { success = false, message = "采购订单不存在" });

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessPurchaseOrderAsync(userId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该采购订单" });

                var result = await _service.RefreshItemExtendsAsync(id, cancellationToken);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新采购订单明细扩展失败: {Id}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{id:guid}/refresh-vendor-name")]
        [RequirePermission("purchase-order.write")]
        public async Task<IActionResult> RefreshVendorName(string id, CancellationToken cancellationToken)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return StatusCode(403, new { success = false, message = "未登录或身份无效" });

                var summary = await _rbacService.GetUserPermissionSummaryAsync(userId.Trim());
                if (summary?.IsSysAdmin != true)
                    return StatusCode(403, new { success = false, message = "仅系统管理员可刷新供应商名称" });

                var order = await _service.GetByIdAsync(id);
                if (order == null) return NotFound(new { success = false, message = "采购订单不存在" });

                if (!await _dataPermissionService.CanAccessPurchaseOrderAsync(userId.Trim(), order))
                    return StatusCode(403, new { success = false, message = "无权限访问该采购订单" });

                cancellationToken.ThrowIfCancellationRequested();
                var result = await _service.RefreshVendorNameAsync(id, userId.Trim());
                return Ok(new { success = true, data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新采购订单供应商名称失败: {Id}", id);
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

        private async Task<IReadOnlyDictionary<string, PurchaseOrderItemExtend>?> LoadPoItemExtendsAsync(
            ICollection<PurchaseOrderItem>? items,
            CancellationToken cancellationToken = default)
        {
            if (items == null || items.Count == 0) return null;
            var ids = items.Select(i => i.Id).ToList();
            var rows = await _db.PurchaseOrderItemExtends.AsNoTracking()
                .Where(e => ids.Contains(e.Id))
                .ToListAsync(cancellationToken);
            if (rows.Count == 0) return null;
            return rows.ToDictionary(e => e.Id, e => e, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<IReadOnlyDictionary<string, string>> LoadSellOrderItemCodesAsync(
            ICollection<PurchaseOrderItem>? items,
            CancellationToken cancellationToken = default)
        {
            if (items == null || items.Count == 0)
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var ids = items
                .Select(i => i.SellOrderItemId?.Trim())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (ids.Count == 0)
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var rows = await _db.SellOrderItems.AsNoTracking()
                .Where(x => ids.Contains(x.Id))
                .Select(x => new { x.Id, x.SellOrderItemCode })
                .ToListAsync(cancellationToken);

            return rows.ToDictionary(
                x => x.Id,
                x => x.SellOrderItemCode ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);
        }

        private async Task<IReadOnlyDictionary<string, string?>> LoadCreateUserLoginNamesForPoLinesAsync(
            IEnumerable<PurchaseOrderItemListLineRaw> lines,
            CancellationToken cancellationToken)
        {
            var ids = lines
                .Select(x => x.CreateByUserId)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (ids.Count == 0)
                return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            var rows = await _db.Users.AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync(cancellationToken);
            return rows.ToDictionary(x => x.Id, x => (string?)x.UserName, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<HashSet<string>> LoadPoItemIdsWithActivePaymentRequestAsync(
            IEnumerable<PurchaseOrderItemListLineRaw> lines,
            CancellationToken cancellationToken)
        {
            var poItemIds = lines
                .Select(x => x.PurchaseOrderItemId?.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (poItemIds.Count == 0)
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            const short paymentAuditFailed = -1;
            const short paymentCancelled = -2;

            var activeIds = await _db.FinancePaymentItems.AsNoTracking()
                .Where(pi =>
                    pi.PurchaseOrderItemId != null
                    && poItemIds.Contains(pi.PurchaseOrderItemId))
                .Join(
                    _db.FinancePayments.AsNoTracking(),
                    pi => pi.FinancePaymentId,
                    p => p.Id,
                    (pi, p) => new { pi.PurchaseOrderItemId, p.Status })
                .Where(x => x.Status != paymentAuditFailed && x.Status != paymentCancelled)
                .Select(x => x.PurchaseOrderItemId!)
                .Distinct()
                .ToListAsync(cancellationToken);

            return activeIds
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private async Task<IReadOnlyDictionary<string, string>> LoadVendorEnglishNameMapForPoLinesAsync(
            IEnumerable<PurchaseOrderItemListLineRaw> lines,
            CancellationToken cancellationToken)
        {
            var ids = lines
                .Select(r => r.VendorId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (ids.Count == 0)
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            return await _db.Vendors.AsNoTracking()
                .Where(v => ids.Contains(v.Id) && v.EnglishOfficialName != null && v.EnglishOfficialName != "")
                .Select(v => new { v.Id, v.EnglishOfficialName })
                .ToDictionaryAsync(
                    x => x.Id,
                    x => x.EnglishOfficialName!.Trim(),
                    StringComparer.OrdinalIgnoreCase,
                    cancellationToken);
        }

        private static List<object> MapPurchaseOrderItemListLines(
            IEnumerable<PurchaseOrderItemListLineRaw> lines,
            UserPermissionSummaryDto? summary,
            IReadOnlyDictionary<string, string?> createUserLoginByUserId,
            IReadOnlySet<string> poItemIdsWithActivePaymentRequest,
            IReadOnlyDictionary<string, string> vendorEnglishMap)
        {
            var mask511 = PurchaseSensitiveFieldMask511.ShouldMask(summary);
            var canViewVendorInfo = !mask511 && (summary?.IsSysAdmin == true
                || SummaryHasPermission(summary, "vendor.info.read")
                || SummaryHasPermission(summary, "vendor.read")
                || SummaryHasPermission(summary, "purchase-order.read")
                || SummaryHasPermission(summary, "purchase-order.write"));
            var canViewPurchaseAmount = !mask511 && (summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("purchase.amount.read") ?? false));
            var canInitiatePaymentFromPo = !mask511 && (summary?.IsSysAdmin == true
                || SummaryHasPermission(summary, "finance-payment.write")
                || SummaryHasPermission(summary, "purchase-order.write"));
            var canViewPurchaseUser = summary?.IsSysAdmin == true
                || SummaryHasPermission(summary, "purchase.user.read")
                || SummaryHasPermission(summary, "purchase-order.read");

            var list = new List<object>();
            foreach (var r in lines)
            {
                var costOut = canViewPurchaseAmount ? r.Cost : 0m;
                var qty = r.Qty;
                var lineTotal = canViewPurchaseAmount ? qty * r.Cost : 0m;
                var canApply = canInitiatePaymentFromPo
                    && r.FinancePaymentStatus < 2
                    && (r.ItemStatus == 30 || r.OrderStatus == 30);
                var createKey = (r.CreateByUserId ?? string.Empty).Trim();
                string? createUserName = null;
                if (!string.IsNullOrEmpty(createKey) && createUserLoginByUserId.TryGetValue(createKey, out var login))
                    createUserName = login;

                list.Add(new
                {
                    purchaseOrderItemId = r.PurchaseOrderItemId,
                    purchaseOrderId = r.PurchaseOrderId,
                    purchaseOrderItemCode = r.PurchaseOrderItemCode,
                    purchaseOrderCode = r.PurchaseOrderCode,
                    freightForwarderOrderNo = r.FreightForwarderOrderNo,
                    purchaseOrderType = r.PurchaseOrderType,
                    vendorId = canViewVendorInfo ? r.VendorId : null,
                    vendorName = canViewVendorInfo ? r.VendorName : null,
                    vendorEnglishName = canViewVendorInfo && !string.IsNullOrWhiteSpace(r.VendorId)
                        && vendorEnglishMap.TryGetValue(r.VendorId.Trim(), out var ven)
                        ? ven
                        : null,
                    itemStatus = r.ItemStatus,
                    purchaseProgressStatus = r.PurchaseProgressStatus,
                    stockInProgressStatus = r.StockInProgressStatus,
                    paymentRequestProgressStatus = poItemIdsWithActivePaymentRequest.Contains(r.PurchaseOrderItemId)
                        ? (short)1
                        : (short)0,
                    paymentProgressStatus = r.PaymentProgressStatus,
                    invoiceProgressStatus = r.InvoiceProgressStatus,
                    canApplyPayment = canApply,
                    orderCreateTime = r.OrderCreateTime,
                    purchaseUserName = canViewPurchaseUser ? r.PurchaseUserName : null,
                    createUserName,
                    createdBy = (string?)null,
                    pn = r.Pn,
                    brand = r.Brand,
                    qty = r.Qty,
                    cost = costOut,
                    lineTotal,
                    paymentRequestedAmount = canViewPurchaseAmount ? r.PaymentAmountRequested : 0m,
                    qtyStockInNotifyExpectSum = r.QtyStockInNotifyExpectSum,
                    qtyStockInNotifyNot = r.QtyStockInNotifyNot,
                    currency = r.Currency,
                    deliveryDate = r.DeliveryDate
                });
            }

            return list;
        }

        private async Task<(PurchaseOrderItemListQueryRequest Request, bool MaskAmounts)> BuildItemListAnalyticsQueryRequestAsync(
            string? startDate,
            string? endDate,
            string? purchaseOrderCode,
            string? freightForwarderOrderNo,
            string? vendorName,
            string? purchaseUserName,
            string? pn,
            short? orderType,
            string? transactionCurrency,
            short? paymentProgressStatus,
            short? purchaseProgressStatus,
            short? stockInProgressStatus,
            short? invoiceProgressStatus,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var summary = await GetPermissionSummaryAsync(userId);
            var mask511 = PurchaseSensitiveFieldMask511.ShouldMask(summary);
            var canViewVendorInfo = !mask511 && (summary?.IsSysAdmin == true
                || SummaryHasPermission(summary, "vendor.info.read")
                || SummaryHasPermission(summary, "vendor.read")
                || SummaryHasPermission(summary, "purchase-order.read")
                || SummaryHasPermission(summary, "purchase-order.write"));
            var canViewPurchaseAmount = !mask511 && (summary?.IsSysAdmin == true
                || SummaryHasPermission(summary, "purchase.amount.read"));
            var maskAmounts = !canViewPurchaseAmount;
            var canViewPurchaseUser = summary?.IsSysAdmin == true
                || SummaryHasPermission(summary, "purchase.user.read")
                || SummaryHasPermission(summary, "purchase-order.read");

            var request = new PurchaseOrderItemListQueryRequest
            {
                CurrentUserId = userId,
                StartDate = DateTime.TryParse(startDate, out var sd) ? sd : null,
                EndDate = DateTime.TryParse(endDate, out var ed) ? ed : null,
                PurchaseOrderCode = string.IsNullOrWhiteSpace(purchaseOrderCode) ? null : purchaseOrderCode.Trim(),
                FreightForwarderOrderNo = string.IsNullOrWhiteSpace(freightForwarderOrderNo) ? null : freightForwarderOrderNo.Trim(),
                VendorName = canViewVendorInfo && !string.IsNullOrWhiteSpace(vendorName) ? vendorName.Trim() : null,
                PurchaseUserName = canViewPurchaseUser && !string.IsNullOrWhiteSpace(purchaseUserName) ? purchaseUserName.Trim() : null,
                Pn = string.IsNullOrWhiteSpace(pn) ? null : pn.Trim(),
                OrderType = orderType,
                TransactionCurrency = transactionCurrency,
                PaymentProgressStatus = paymentProgressStatus,
                PurchaseProgressStatus = purchaseProgressStatus,
                StockInProgressStatus = stockInProgressStatus,
                InvoiceProgressStatus = invoiceProgressStatus
            };

            return (request, maskAmounts);
        }

        private async Task<(PurchaseOrderQueryRequest Request, bool MaskAmounts)> BuildListAnalyticsQueryRequestAsync(
            string? keyword,
            string? code,
            string? vendor,
            string? freightForwarderOrderNo,
            string? purchaseUserName,
            string? comment,
            short? orderType,
            short? status,
            string? startDate,
            string? endDate,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var summary = await GetPermissionSummaryAsync(userId);
            var mask511 = PurchaseSensitiveFieldMask511.ShouldMask(summary);
            var canViewPurchaseAmount = !mask511 && (summary?.IsSysAdmin == true
                || SummaryHasPermission(summary, "purchase.amount.read"));
            var maskAmounts = !canViewPurchaseAmount;
            var canViewPurchaseUser = summary?.IsSysAdmin == true
                || SummaryHasPermission(summary, "purchase.user.read")
                || SummaryHasPermission(summary, "purchase-order.read");

            var request = new PurchaseOrderQueryRequest
            {
                Keyword = keyword,
                PurchaseOrderCodeFilter = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
                VendorNameFilter = string.IsNullOrWhiteSpace(vendor) ? null : vendor.Trim(),
                FreightForwarderOrderNoFilter = string.IsNullOrWhiteSpace(freightForwarderOrderNo) ? null : freightForwarderOrderNo.Trim(),
                PurchaseUserNameFilter = canViewPurchaseUser && !string.IsNullOrWhiteSpace(purchaseUserName) ? purchaseUserName.Trim() : null,
                CommentFilter = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                OrderType = orderType,
                Status = status,
                StartDate = DateTime.TryParse(startDate, out var start) ? start : null,
                EndDate = DateTime.TryParse(endDate, out var end) ? end : null,
                CurrentUserId = userId
            };

            return (request, maskAmounts);
        }

        private static bool SummaryHasPermission(UserPermissionSummaryDto? summary, string code)
        {
            if (summary?.PermissionCodes == null) return false;
            return summary.PermissionCodes.Any(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 主单 <see cref="PurchaseOrder.Currency"/> 与明细 <see cref="PurchaseOrderItem.Currency"/> 不一致时，
        /// 对外展示的总金额币别应与明细一致（未取消明细仅一种币别时采用该币别；无明细或多币别仍用主单）。
        /// </summary>
        private static short ResolvePurchaseOrderHeaderCurrency(
            CRM.Core.Models.Purchase.PurchaseOrder order,
            List<CRM.Core.Models.Purchase.PurchaseOrderItem> itemList,
            short lineCancelledStatus)
        {
            var active = itemList.Where(i => i.Status != lineCancelledStatus).ToList();
            if (active.Count == 0) return order.Currency;
            var distinct = active.Select(i => i.Currency).Distinct().ToList();
            if (distinct.Count == 1) return distinct[0];
            return order.Currency;
        }

        private async Task<Dictionary<string, VendorInfo>> LoadVendorMapForPurchaseOrdersAsync(
            IEnumerable<PurchaseOrder> orders,
            CancellationToken cancellationToken)
        {
            var ids = orders
                .Select(o => o.VendorId)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (ids.Count == 0)
                return new Dictionary<string, VendorInfo>(StringComparer.OrdinalIgnoreCase);

            var rows = await _db.Vendors.AsNoTracking()
                .Where(v => ids.Contains(v.Id))
                .ToListAsync(cancellationToken);
            return rows.ToDictionary(v => v.Id, v => v, StringComparer.OrdinalIgnoreCase);
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
                u => string.IsNullOrWhiteSpace(u.RealName) ? u.UserName : u.RealName.Trim(),
                StringComparer.OrdinalIgnoreCase);
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

        private static string? ResolvePurchaseOrderVendorName(VendorInfo? vendor, CRM.Core.Models.Purchase.PurchaseOrder order)
        {
            if (vendor != null && !string.IsNullOrWhiteSpace(vendor.OfficialName))
                return vendor.OfficialName.Trim();
            return string.IsNullOrWhiteSpace(order.VendorName) ? null : order.VendorName.Trim();
        }

        private static string? ResolvePurchaseOrderVendorCode(VendorInfo? vendor, CRM.Core.Models.Purchase.PurchaseOrder order)
        {
            if (vendor != null && !string.IsNullOrWhiteSpace(vendor.Code))
                return vendor.Code.Trim();
            return string.IsNullOrWhiteSpace(order.VendorCode) ? null : order.VendorCode.Trim();
        }

        private static string? ResolvePurchaseOrderVendorEnglishName(VendorInfo? vendor)
        {
            if (vendor != null && !string.IsNullOrWhiteSpace(vendor.EnglishOfficialName))
                return vendor.EnglishOfficialName.Trim();
            return null;
        }

        private object MaskPurchaseOrder(
            CRM.Core.Models.Purchase.PurchaseOrder order,
            UserPermissionSummaryDto? summary,
            VendorContactInfo? vendorContact = null,
            VendorInfo? vendor = null,
            IReadOnlyDictionary<string, PurchaseOrderItemExtend>? itemExtends = null,
            IReadOnlyDictionary<string, string>? sellOrderItemCodes = null,
            string? assistorUserName = null)
        {
            var mask511 = PurchaseSensitiveFieldMask511.ShouldMask(summary);
            // vendor.info.read：完整联系人/地址等；vendor.read 或采购订单权限：至少返回供应商主键与名称（申请付款依赖 VendorId）
            // PRD §5.1.1：销售方向 + PurchaseDataScope==4 等条件下强制脱敏，不因 vendor.read / purchase-order.read 等放宽。
            var canViewVendorInfo = !mask511 && (summary?.IsSysAdmin == true
                || SummaryHasPermission(summary, "vendor.info.read")
                || SummaryHasPermission(summary, "vendor.read")
                || SummaryHasPermission(summary, "purchase-order.read")
                || SummaryHasPermission(summary, "purchase-order.write"));
            var canViewPurchaseAmount = !mask511 && (summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("purchase.amount.read") ?? false));
            // 采购员常仅有 purchase-order.write；从采购明细「申请付款」需与 FinancePaymentsController Create/Patch 一致
            var canInitiatePaymentFromPo = !mask511 && (summary?.IsSysAdmin == true
                || SummaryHasPermission(summary, "finance-payment.write")
                || SummaryHasPermission(summary, "purchase-order.write"));

            const short poOrderCancelled = -2;
            const short poLineCancelled = -2;
            var itemList = (order.Items ?? Enumerable.Empty<CRM.Core.Models.Purchase.PurchaseOrderItem>()).ToList();
            var displayCurrency = ResolvePurchaseOrderHeaderCurrency(order, itemList, poLineCancelled);
            var poOrderCanceled = order.Status == poOrderCancelled;
            var sellLinePurchaseSum = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            if (!poOrderCanceled && itemList.Count > 0)
            {
                foreach (var g in itemList.GroupBy(i => (i.SellOrderItemId ?? "").Trim(), StringComparer.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(g.Key)) continue;
                    sellLinePurchaseSum[g.Key] = g.Where(x => x.Status != poLineCancelled).Sum(x => x.Qty);
                }
            }

            return new
            {
                order.Id,
                order.PurchaseOrderCode,
                order.FreightForwarderOrderNo,
                VendorId = canViewVendorInfo ? order.VendorId : null,
                VendorName = canViewVendorInfo ? ResolvePurchaseOrderVendorName(vendor, order) : null,
                VendorCode = canViewVendorInfo ? ResolvePurchaseOrderVendorCode(vendor, order) : null,
                VendorEnglishName = canViewVendorInfo ? ResolvePurchaseOrderVendorEnglishName(vendor) : null,
                VendorContactId = canViewVendorInfo ? order.VendorContactId : null,
                VendorContactEmail = canViewVendorInfo ? vendorContact?.Email : null,
                VendorContactName = canViewVendorInfo ? (vendorContact?.CName ?? vendorContact?.EName) : null,
                VendorContactPhone = canViewVendorInfo ? (vendorContact?.Mobile ?? vendorContact?.Tel) : null,
                VendorOfficeAddress = canViewVendorInfo ? vendor?.OfficeAddress : null,
                order.PurchaseUserId,
                order.PurchaseUserName,
                order.Assistor,
                AssistorUserName = assistorUserName,
                order.Status,
                order.Type,
                Currency = displayCurrency,
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
                order.CreateByUserId,
                order.ModifyByUserId,
                Items = itemList.Select(i =>
                {
                    PurchaseOrderItemExtend? ext = null;
                    itemExtends?.TryGetValue(i.Id, out ext);
                    var soKey = (i.SellOrderItemId ?? "").Trim();
                    var sellLineSum = poOrderCanceled || string.IsNullOrEmpty(soKey)
                        ? 0m
                        : sellLinePurchaseSum.GetValueOrDefault(soKey);
                    return new
                    {
                        i.Id,
                        i.PurchaseOrderId,
                        i.PurchaseOrderItemCode,
                        i.SellOrderItemId,
                        SellOrderItemCode = !string.IsNullOrWhiteSpace(i.SellOrderItemId)
                            && sellOrderItemCodes != null
                            && sellOrderItemCodes.TryGetValue(i.SellOrderItemId.Trim(), out var soCode)
                            ? soCode
                            : null,
                        VendorId = canViewVendorInfo ? i.VendorId : null,
                        i.ProductId,
                        i.PN,
                        i.Brand,
                        i.Qty,
                        Cost = canViewPurchaseAmount ? i.Cost : 0m,
                        ConvertPrice = canViewPurchaseAmount ? i.ConvertPrice : 0m,
                        i.Currency,
                        i.Status,
                        i.StockInStatus,
                        i.FinancePaymentStatus,
                        i.StockOutStatus,
                        i.ErrStatus,
                        i.DeliveryDate,
                        i.DateCode,
                        i.Comment,
                        i.InnerComment,
                        i.CreateTime,
                        i.ModifyTime,
                        purchaseProgressStatus = ext?.PurchaseProgressStatus ?? (short)0,
                        stockInProgressStatus = ext?.StockInProgressStatus ?? (short)0,
                        paymentProgressStatus = ext?.PaymentProgressStatus ?? (short)0,
                        invoiceProgressStatus = ext?.InvoiceProgressStatus ?? (short)0,
                        purchaseProgressQty = ext?.PurchaseProgressQty ?? 0m,
                        sellLinePurchaseQtySum = sellLineSum,
                        stockInProgressQty = ext?.QtyReceiveTotal ?? 0m,
                        paymentProgressAmount = canViewPurchaseAmount ? (ext?.PaymentAmountFinish ?? 0m) : 0m,
                        paymentRequestedAmount = canViewPurchaseAmount ? (ext?.PaymentAmountRequested ?? 0m) : 0m,
                        qtyStockInNotifyExpectSum = ext?.QtyStockInNotifyExpectSum ?? 0m,
                        qtyStockInNotifyNot = ext?.QtyStockInNotifyNot ?? i.Qty,
                        invoiceProgressAmount = canViewPurchaseAmount ? (ext?.PurchaseInvoiceDone ?? 0m) : 0m,
                        // 业务口径：主单已确认即可申请付款；兼容历史数据中“主单已确认但明细状态未同步到30”的情况
                        CanApplyPayment = canInitiatePaymentFromPo
                            && i.FinancePaymentStatus < 2
                            && (i.Status == 30 || order.Status == 30)
                    };
                }).ToList()
            };
        }

        /// <summary>将 Npgsql/EF 底层原因合并进提示文案，避免界面仅显示「See the inner exception」。</summary>
        private static string PersistErrorMessage(Exception ex)
        {
            for (var c = ex; c != null; c = c.InnerException!)
            {
                if (c is PostgresException pg)
                    return pg.MessageText;
            }

            if (ex is DbUpdateException db)
            {
                var inner = db.InnerException?.Message;
                if (!string.IsNullOrWhiteSpace(inner))
                    return $"{db.Message.TrimEnd('.')}：{inner}";
            }

            return ex.Message;
        }
    }

    public class PurchaseOrderUpdateStatusRequest
    {
        public short Status { get; set; }
    }

    public class SendPurchaseOrderReportEmailRequest
    {
        public string To { get; set; } = string.Empty;
        public string PdfBase64 { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }
    }
}
