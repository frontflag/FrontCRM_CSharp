using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.API.Utilities;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Utilities;
using System.Security.Claims;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class RFQsController : ControllerBase
    {
        private readonly IRFQService _rfqService;
        private readonly IRfqMainListQuery _rfqMainListQuery;
        private readonly IRfqItemListQuery _rfqItemListQuery;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IRbacService _rbacService;
        private readonly IPurchaseQuoterPoolService _purchaseQuoterPoolService;
        private readonly ILogger<RFQsController> _logger;

        public RFQsController(
            IRFQService rfqService,
            IRfqMainListQuery rfqMainListQuery,
            IRfqItemListQuery rfqItemListQuery,
            IDataPermissionService dataPermissionService,
            IRbacService rbacService,
            IPurchaseQuoterPoolService purchaseQuoterPoolService,
            ILogger<RFQsController> logger)
        {
            _rfqService = rfqService;
            _rfqMainListQuery = rfqMainListQuery;
            _rfqItemListQuery = rfqItemListQuery;
            _dataPermissionService = dataPermissionService;
            _rbacService = rbacService;
            _purchaseQuoterPoolService = purchaseQuoterPoolService;
            _logger = logger;
        }

        // GET api/v1/rfqs?pageNumber=1&pageSize=20&keyword=&status=
        [HttpGet]
        [RequirePermission("rfq.read")]
        public async Task<ActionResult<ApiResponse<object>>> GetRFQs(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int? page = null,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] short? status = null,
            [FromQuery] string? customerId = null,
            [FromQuery] string? startDate = null,
            [FromQuery] string? endDate = null,
            [FromQuery] string[]? tagIds = null)
        {
            try
            {
                var pageNorm = page is >= 1 ? page!.Value : (pageNumber < 1 ? 1 : pageNumber);
                var normalizedTagIds = tagIds?
                    .SelectMany(v => (v ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var request = new RFQQueryRequest
                {
                    PageIndex = pageNorm,
                    PageSize = pageSize,
                    Keyword = keyword,
                    Status = status,
                    CustomerId = customerId,
                    StartDate = PostgreSqlDateTime.ParseDateOnly(startDate),
                    EndDate = PostgreSqlDateTime.ParseDateOnly(endDate),
                    CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    TagIds = normalizedTagIds is { Count: > 0 } ? normalizedTagIds : null
                };
                var result = await _rfqService.GetPagedAsync(request);
                return Ok(ApiResponse<object>.Ok(new
                {
                    items = result.Items,
                    totalCount = result.TotalCount,
                    total = result.TotalCount,
                    pageNumber = result.PageIndex,
                    page = result.PageIndex,
                    pageSize = result.PageSize,
                    totalPages = result.TotalPages,
                    aggregates = result.Aggregates
                }, "获取需求列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取需求列表失败");
                return StatusCode(500, ApiResponse<object>.Fail($"获取需求列表失败: {ex.Message}", 500));
            }
        }

        [HttpGet("analytics/dashboard")]
        [RequirePermission("rfq.read")]
        public async Task<IActionResult> GetListAnalyticsDashboard(
            [FromQuery] string? keyword,
            [FromQuery] short? status,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] string[]? tagIds,
            CancellationToken cancellationToken = default)
        {
            var (request, maskCustomerNames) = await BuildListAnalyticsQueryRequestAsync(
                keyword, status, startDate, endDate, tagIds, cancellationToken);
            var data = await _rfqMainListQuery.GetListAnalyticsDashboardAsync(request, maskCustomerNames, cancellationToken);
            return Ok(ApiResponse<RfqListAnalyticsDashboardDto>.Ok(data));
        }

        [HttpGet("analytics/trends")]
        [RequirePermission("rfq.read")]
        public async Task<IActionResult> GetListAnalyticsTrends(
            [FromQuery] string? keyword,
            [FromQuery] short? status,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] string[]? tagIds,
            [FromQuery] string? groupBy,
            CancellationToken cancellationToken = default)
        {
            var (request, _) = await BuildListAnalyticsQueryRequestAsync(
                keyword, status, startDate, endDate, tagIds, cancellationToken);
            var data = await _rfqMainListQuery.GetListAnalyticsTrendsAsync(
                request,
                string.IsNullOrWhiteSpace(groupBy) ? "month" : groupBy.Trim(),
                cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<RfqListAnalyticsTrendPointDto>>.Ok(data));
        }

        [HttpGet("analytics/breakdowns")]
        [RequirePermission("rfq.read")]
        public async Task<IActionResult> GetListAnalyticsBreakdowns(
            [FromQuery] string? keyword,
            [FromQuery] short? status,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] string[]? tagIds,
            CancellationToken cancellationToken = default)
        {
            var (request, _) = await BuildListAnalyticsQueryRequestAsync(
                keyword, status, startDate, endDate, tagIds, cancellationToken);
            var data = await _rfqMainListQuery.GetListAnalyticsBreakdownsAsync(request, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>>.Ok(data));
        }

        [HttpGet("analytics/rankings")]
        [RequirePermission("rfq.read")]
        public async Task<IActionResult> GetListAnalyticsRankings(
            [FromQuery] string? keyword,
            [FromQuery] short? status,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] string[]? tagIds,
            CancellationToken cancellationToken = default)
        {
            var (request, maskCustomerNames) = await BuildListAnalyticsQueryRequestAsync(
                keyword, status, startDate, endDate, tagIds, cancellationToken);
            var data = await _rfqMainListQuery.GetListAnalyticsRankingsAsync(request, maskCustomerNames, cancellationToken);
            return Ok(ApiResponse<RfqListAnalyticsRankingsDto>.Ok(data));
        }

        /// <summary>需求明细分页（须放在 {id} 之前，否则 "items" 会被当成 id）</summary>
        // GET api/v1/rfqs/items?...&salesUserId=&salesUserKeyword=&purchaserUserId=
        [HttpGet("items")]
        [RequirePermission("rfq.read")]
        public async Task<ActionResult<ApiResponse<object>>> GetRFQItems(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int? page = null,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? startDate = null,
            [FromQuery] string? endDate = null,
            [FromQuery] string? customerKeyword = null,
            [FromQuery] string? materialModel = null,
            [FromQuery] string? salesUserId = null,
            [FromQuery] string? salesUserKeyword = null,
            [FromQuery] string? purchaserUserId = null,
            [FromQuery] string? hasQuotesOnly = null,
            [FromQuery] short? status = null,
            [FromQuery] string? rfqCode = null)
        {
            try
            {
                var pageNorm = page is >= 1 ? page!.Value : (pageNumber < 1 ? 1 : pageNumber);
                var request = new RFQItemQueryRequest
                {
                    PageIndex = pageNorm,
                    PageSize = pageSize,
                    StartDate = PostgreSqlDateTime.ParseDateOnly(startDate),
                    EndDate = PostgreSqlDateTime.ParseDateOnly(endDate),
                    CustomerKeyword = customerKeyword,
                    MaterialModel = materialModel,
                    SalesUserId = salesUserId,
                    SalesUserKeyword = salesUserKeyword,
                    PurchaserUserId = purchaserUserId,
                    HasQuotesOnly = ParseQueryBool(hasQuotesOnly),
                    Status = status,
                    RfqCode = rfqCode,
                    CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                };
                var result = await _rfqService.GetPagedItemsAsync(request);
                return Ok(ApiResponse<object>.Ok(new
                {
                    items = result.Items,
                    totalCount = result.TotalCount,
                    total = result.TotalCount,
                    pageNumber = result.PageIndex,
                    page = result.PageIndex,
                    pageSize = result.PageSize,
                    totalPages = result.TotalPages
                }, "获取需求明细列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取需求明细列表失败");
                return StatusCode(500, ApiResponse<object>.Fail($"获取需求明细列表失败: {ex.Message}", 500));
            }
        }

        [HttpGet("items/analytics/dashboard")]
        [RequirePermission("rfq.read")]
        public async Task<IActionResult> GetItemListAnalyticsDashboard(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] string? customerKeyword,
            [FromQuery] string? materialModel,
            [FromQuery] string? salesUserId,
            [FromQuery] string? salesUserKeyword,
            [FromQuery] string? purchaserUserId,
            [FromQuery] string? hasQuotesOnly,
            [FromQuery] short? status,
            [FromQuery] string? rfqCode,
            CancellationToken cancellationToken = default)
        {
            var (request, maskCustomerNames) = await BuildItemListAnalyticsQueryRequestAsync(
                startDate, endDate, customerKeyword, materialModel, salesUserId, salesUserKeyword,
                purchaserUserId, hasQuotesOnly, status, rfqCode, cancellationToken);
            var data = await _rfqItemListQuery.GetListAnalyticsDashboardAsync(request, maskCustomerNames, cancellationToken);
            return Ok(ApiResponse<RfqListAnalyticsDashboardDto>.Ok(data));
        }

        [HttpGet("items/analytics/trends")]
        [RequirePermission("rfq.read")]
        public async Task<IActionResult> GetItemListAnalyticsTrends(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] string? customerKeyword,
            [FromQuery] string? materialModel,
            [FromQuery] string? salesUserId,
            [FromQuery] string? salesUserKeyword,
            [FromQuery] string? purchaserUserId,
            [FromQuery] string? hasQuotesOnly,
            [FromQuery] short? status,
            [FromQuery] string? rfqCode,
            [FromQuery] string? groupBy,
            CancellationToken cancellationToken = default)
        {
            var (request, _) = await BuildItemListAnalyticsQueryRequestAsync(
                startDate, endDate, customerKeyword, materialModel, salesUserId, salesUserKeyword,
                purchaserUserId, hasQuotesOnly, status, rfqCode, cancellationToken);
            var data = await _rfqItemListQuery.GetListAnalyticsTrendsAsync(
                request,
                string.IsNullOrWhiteSpace(groupBy) ? "month" : groupBy.Trim(),
                cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<RfqListAnalyticsTrendPointDto>>.Ok(data));
        }

        [HttpGet("items/analytics/breakdowns")]
        [RequirePermission("rfq.read")]
        public async Task<IActionResult> GetItemListAnalyticsBreakdowns(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] string? customerKeyword,
            [FromQuery] string? materialModel,
            [FromQuery] string? salesUserId,
            [FromQuery] string? salesUserKeyword,
            [FromQuery] string? purchaserUserId,
            [FromQuery] string? hasQuotesOnly,
            [FromQuery] short? status,
            [FromQuery] string? rfqCode,
            CancellationToken cancellationToken = default)
        {
            var (request, _) = await BuildItemListAnalyticsQueryRequestAsync(
                startDate, endDate, customerKeyword, materialModel, salesUserId, salesUserKeyword,
                purchaserUserId, hasQuotesOnly, status, rfqCode, cancellationToken);
            var data = await _rfqItemListQuery.GetListAnalyticsBreakdownsAsync(request, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>>.Ok(data));
        }

        [HttpGet("items/analytics/rankings")]
        [RequirePermission("rfq.read")]
        public async Task<IActionResult> GetItemListAnalyticsRankings(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] string? customerKeyword,
            [FromQuery] string? materialModel,
            [FromQuery] string? salesUserId,
            [FromQuery] string? salesUserKeyword,
            [FromQuery] string? purchaserUserId,
            [FromQuery] string? hasQuotesOnly,
            [FromQuery] short? status,
            [FromQuery] string? rfqCode,
            CancellationToken cancellationToken = default)
        {
            var (request, maskCustomerNames) = await BuildItemListAnalyticsQueryRequestAsync(
                startDate, endDate, customerKeyword, materialModel, salesUserId, salesUserKeyword,
                purchaserUserId, hasQuotesOnly, status, rfqCode, cancellationToken);
            var data = await _rfqItemListQuery.GetListAnalyticsRankingsAsync(request, maskCustomerNames, cancellationToken);
            return Ok(ApiResponse<RfqItemListAnalyticsRankingsDto>.Ok(data));
        }

        /// <summary>标记需求明细为查无报价（status 0→5）</summary>
        // POST api/v1/rfqs/items/{itemId}/mark-no-quote
        [HttpPost("items/{itemId}/mark-no-quote")]
        [RequirePermission("rfq.read")]
        public async Task<ActionResult<ApiResponse<object>>> MarkNoQuote(string itemId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var item = await _rfqService.MarkNoQuoteAsync(itemId, userId);
                return Ok(ApiResponse<object>.Ok(new { id = item.Id, status = item.Status }, "已标记为查无报价"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<object>.Fail(ex.Message, 403));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ApiResponse<object>.Fail(ex.Message, 409));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "标记查无报价失败: {ItemId}", itemId);
                return StatusCode(500, ApiResponse<object>.Fail($"标记查无报价失败: {ex.Message}", 500));
            }
        }

        [HttpGet("default-assign-method")]
        [RequirePermission("rfq.create")]
        public async Task<ActionResult<ApiResponse<PurchaseParamsDefaultAssignMethodDto>>> GetDefaultAssignMethodForCreate(
            CancellationToken ct)
        {
            try
            {
                var assignMethod = await _purchaseQuoterPoolService.GetDefaultAssignMethodAsync(ct);
                return Ok(ApiResponse<PurchaseParamsDefaultAssignMethodDto>.Ok(
                    new PurchaseParamsDefaultAssignMethodDto { AssignMethod = assignMethod },
                    "ok"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取默认分配方式失败");
                return StatusCode(500, ApiResponse<PurchaseParamsDefaultAssignMethodDto>.Fail("读取失败", 500));
            }
        }

        // GET api/v1/rfqs/{id}
        [HttpGet("{id}")]
        [RequirePermission("rfq.read")]
        public async Task<ActionResult<ApiResponse<object>>> GetRFQ(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var rfq = await _rfqService.GetByIdAsync(id, userId);
                if (rfq == null)
                    return NotFound(ApiResponse<object>.Fail("需求不存在", 404));
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessRFQAsync(userId, rfq))
                    return StatusCode(403, ApiResponse<object>.Fail("无权限访问该需求", 403));
                return Ok(ApiResponse<object>.Ok(rfq, "获取需求成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取需求失败: {Id}", id);
                return StatusCode(500, ApiResponse<object>.Fail($"获取需求失败: {ex.Message}", 500));
            }
        }

        // POST api/v1/rfqs（与采购维护 rfq.write 拆分：仅销售侧/显式授权 rfq.create）
        [HttpPost]
        [RequirePermission("rfq.create")]
        public async Task<ActionResult<ApiResponse<object>>> CreateRFQ([FromBody] CreateRFQRequest request)
        {
            try
            {
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var rfq = await _rfqService.CreateAsync(request, actorId);
                return CreatedAtAction(nameof(GetRFQ), new { id = rfq.Id },
                    ApiResponse<object>.Ok(rfq, "需求创建成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ApiResponse<object>.Fail(ex.Message, 409));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "创建需求数据库失败");
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, ApiResponse<object>.Fail($"创建需求失败: {detail}", 500));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建需求失败");
                return StatusCode(500, ApiResponse<object>.Fail($"创建需求失败: {ex.Message}", 500));
            }
        }

        // PUT api/v1/rfqs/{id}
        [HttpPut("{id}")]
        [RequirePermission("rfq.write")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateRFQ(string id, [FromBody] UpdateRFQRequest request)
        {
            try
            {
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var rfq = await _rfqService.UpdateAsync(id, request, actorId);
                return Ok(ApiResponse<object>.Ok(rfq, "需求更新成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新需求失败: {Id}", id);
                return StatusCode(500, ApiResponse<object>.Fail($"更新需求失败: {ex.Message}", 500));
            }
        }

        // DELETE api/v1/rfqs/{id}
        [HttpDelete("{id}")]
        [RequirePermission("rfq.write")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteRFQ(string id)
        {
            try
            {
                await _rfqService.DeleteAsync(id);
                return Ok(ApiResponse<object>.Ok((object)null!, "需求删除成功"));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除需求失败: {Id}", id);
                return StatusCode(500, ApiResponse<object>.Fail($"删除需求失败: {ex.Message}", 500));
            }
        }

        // PATCH api/v1/rfqs/{id}/status
        [HttpPatch("{id}/status")]
        [RequirePermission("rfq.read")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateStatus(string id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                await _rfqService.UpdateStatusAsync(id, request.Status);
                return Ok(ApiResponse<object>.Ok((object)null!, "状态更新成功"));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新需求状态失败: {Id}", id);
                return StatusCode(500, ApiResponse<object>.Fail($"更新状态失败: {ex.Message}", 500));
            }
        }

        // POST api/v1/rfqs/{id}/assign
        [HttpPost("{id}/assign")]
        [RequirePermission("rfq.write")]
        public async Task<ActionResult<ApiResponse<object>>> AssignPurchaser(string id, [FromBody] AssignPurchaserRequest request)
        {
            try
            {
                var existing = await _rfqService.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(ApiResponse<object>.Fail("需求不存在", 404));
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessRFQAsync(userId, existing))
                    return StatusCode(403, ApiResponse<object>.Fail("无权限操作该需求", 403));

                var rfq = await _rfqService.AssignPurchaserAsync(id, request, userId);
                var assignedItem = !string.IsNullOrWhiteSpace(request.RfqItemId)
                    ? rfq.Items?.FirstOrDefault(i =>
                        string.Equals(i.Id, request.RfqItemId.Trim(), StringComparison.OrdinalIgnoreCase))
                    : rfq.Items?.FirstOrDefault(i => !i.IsDeleted);
                var resolvedPurchaserId = assignedItem?.AssignedPurchaserUserId1 ?? request.PurchaserId.Trim();
                return Ok(ApiResponse<object>.Ok(new
                {
                    id = Guid.NewGuid().ToString("N"),
                    rfqId = rfq.Id,
                    purchaserId = resolvedPurchaserId,
                    assignedAt = DateTime.UtcNow,
                    handleStatus = 0,
                    remark = request.Remark
                }, "分配成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分配采购员失败: {Id}", id);
                return StatusCode(500, ApiResponse<object>.Fail($"分配失败: {ex.Message}", 500));
            }
        }

        /// <summary>需求主表及明细字段变更日志</summary>
        // GET api/v1/rfqs/{id}/change-logs
        [HttpGet("{id}/change-logs")]
        [RequirePermission("rfq.read")]
        public async Task<ActionResult<ApiResponse<object>>> GetChangeLogs(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var rfq = await _rfqService.GetByIdAsync(id, userId);
                if (rfq == null)
                    return NotFound(ApiResponse<object>.Fail("需求不存在", 404));
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessRFQAsync(userId, rfq))
                    return StatusCode(403, ApiResponse<object>.Fail("无权限访问该需求", 403));

                var logs = await _rfqService.GetFieldChangeLogsAsync(id);
                return Ok(ApiResponse<object>.Ok(logs, "获取更改日志成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取需求更改日志失败: {Id}", id);
                return StatusCode(500, ApiResponse<object>.Fail($"获取更改日志失败: {ex.Message}", 500));
            }
        }

        /// <summary>需求关闭记录列表</summary>
        // GET api/v1/rfqs/{id}/close-records
        [HttpGet("{id}/close-records")]
        [RequirePermission("rfq.read")]
        public async Task<ActionResult<ApiResponse<object>>> GetCloseRecords(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var rfq = await _rfqService.GetByIdAsync(id, userId);
                if (rfq == null)
                    return NotFound(ApiResponse<object>.Fail("需求不存在", 404));
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessRFQAsync(userId, rfq))
                    return StatusCode(403, ApiResponse<object>.Fail("无权限访问该需求", 403));

                var records = await _rfqService.GetCloseRecordsAsync(id);
                return Ok(ApiResponse<object>.Ok(records, "获取关闭记录成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取关闭记录失败: {Id}", id);
                return StatusCode(500, ApiResponse<object>.Fail($"获取关闭记录失败: {ex.Message}", 500));
            }
        }

        /// <summary>关闭需求（写入关闭记录并更新主单终态）</summary>
        // POST api/v1/rfqs/{id}/close-records
        [HttpPost("{id}/close-records")]
        [RequirePermission("rfq.write")]
        public async Task<ActionResult<ApiResponse<object>>> AddCloseRecord(string id, [FromBody] CloseRfqRequest request)
        {
            try
            {
                var existing = await _rfqService.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(ApiResponse<object>.Fail("需求不存在", 404));
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessRFQAsync(userId, existing))
                    return StatusCode(403, ApiResponse<object>.Fail("无权限操作该需求", 403));

                var record = await _rfqService.CloseRfqAsync(id, request, userId);
                return Ok(ApiResponse<object>.Ok(record, "需求已关闭"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ApiResponse<object>.Fail(ex.Message, 409));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "关闭需求失败: {Id}", id);
                return StatusCode(500, ApiResponse<object>.Fail($"关闭需求失败: {ex.Message}", 500));
            }
        }

        private async Task<(RFQQueryRequest Request, bool MaskCustomerNames)> BuildListAnalyticsQueryRequestAsync(
            string? keyword,
            short? status,
            string? startDate,
            string? endDate,
            string[]? tagIds,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var mask521 = await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User);
            var summary = string.IsNullOrWhiteSpace(userId)
                ? null
                : await _rbacService.GetUserPermissionSummaryAsync(userId);
            var canViewCustomer = !mask521 && (summary?.IsSysAdmin == true
                || (summary?.PermissionCodes?.Contains("customer.info.read") ?? false));
            var maskCustomerNames = !canViewCustomer;

            var normalizedTagIds = tagIds?
                .SelectMany(v => (v ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var request = new RFQQueryRequest
            {
                Keyword = keyword,
                Status = status,
                StartDate = PostgreSqlDateTime.ParseDateOnly(startDate),
                EndDate = PostgreSqlDateTime.ParseDateOnly(endDate),
                CurrentUserId = userId,
                TagIds = normalizedTagIds is { Count: > 0 } ? normalizedTagIds : null
            };

            return (request, maskCustomerNames);
        }

        private async Task<(RFQItemQueryRequest Request, bool MaskCustomerNames)> BuildItemListAnalyticsQueryRequestAsync(
            string? startDate,
            string? endDate,
            string? customerKeyword,
            string? materialModel,
            string? salesUserId,
            string? salesUserKeyword,
            string? purchaserUserId,
            string? hasQuotesOnly,
            short? status,
            string? rfqCode,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var mask521 = await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User);
            var summary = string.IsNullOrWhiteSpace(userId)
                ? null
                : await _rbacService.GetUserPermissionSummaryAsync(userId);
            var canViewCustomer = !mask521 && (summary?.IsSysAdmin == true
                || (summary?.PermissionCodes?.Contains("customer.info.read") ?? false));
            var maskCustomerNames = !canViewCustomer;

            var request = new RFQItemQueryRequest
            {
                StartDate = PostgreSqlDateTime.ParseDateOnly(startDate),
                EndDate = PostgreSqlDateTime.ParseDateOnly(endDate),
                CustomerKeyword = canViewCustomer && !string.IsNullOrWhiteSpace(customerKeyword)
                    ? customerKeyword.Trim()
                    : null,
                MaterialModel = !string.IsNullOrWhiteSpace(materialModel) ? materialModel.Trim() : null,
                SalesUserId = !mask521 && !string.IsNullOrWhiteSpace(salesUserId) ? salesUserId.Trim() : null,
                SalesUserKeyword = !mask521 && !string.IsNullOrWhiteSpace(salesUserKeyword)
                    ? salesUserKeyword.Trim()
                    : null,
                PurchaserUserId = !string.IsNullOrWhiteSpace(purchaserUserId) ? purchaserUserId.Trim() : null,
                HasQuotesOnly = ParseQueryBool(hasQuotesOnly),
                Status = status,
                RfqCode = !string.IsNullOrWhiteSpace(rfqCode) ? rfqCode.Trim() : null,
                CurrentUserId = userId,
                CanViewCustomerInList = canViewCustomer
            };

            return (request, maskCustomerNames);
        }

        /// <summary>解析查询字符串布尔（兼容 true/True/1/yes），避免模型绑定对 query 的歧义。</summary>
        private static bool? ParseQueryBool(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var s = raw.Trim();
            if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase)) return false;
            if (s == "1") return true;
            if (s == "0") return false;
            if (string.Equals(s, "yes", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, "no", StringComparison.OrdinalIgnoreCase)) return false;
            return null;
        }
    }

    public class UpdateStatusRequest
    {
        public short Status { get; set; }
    }
}
