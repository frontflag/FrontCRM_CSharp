using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using System.Security.Claims;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [RequirePermission("rfq.read")]
    public class RFQsController : ControllerBase
    {
        private readonly IRFQService _rfqService;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly ILogger<RFQsController> _logger;

        public RFQsController(IRFQService rfqService, IDataPermissionService dataPermissionService, ILogger<RFQsController> logger)
        {
            _rfqService = rfqService;
            _dataPermissionService = dataPermissionService;
            _logger = logger;
        }

        // GET api/v1/rfqs?pageNumber=1&pageSize=20&keyword=&status=
        [HttpGet]
        public async Task<ActionResult<ApiResponse<object>>> GetRFQs(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] short? status = null,
            [FromQuery] string? customerId = null,
            [FromQuery] string? startDate = null,
            [FromQuery] string? endDate = null)
        {
            try
            {
                var request = new RFQQueryRequest
                {
                    PageIndex = pageNumber,
                    PageSize = pageSize,
                    Keyword = keyword,
                    Status = status,
                    CustomerId = customerId,
                    StartDate = string.IsNullOrEmpty(startDate) ? null : DateTime.Parse(startDate),
                    EndDate = string.IsNullOrEmpty(endDate) ? null : DateTime.Parse(endDate),
                    CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                };
                var result = await _rfqService.GetPagedAsync(request);
                return Ok(ApiResponse<object>.Ok(new
                {
                    items = result.Items,
                    totalCount = result.TotalCount,
                    pageNumber = result.PageIndex,
                    pageSize = result.PageSize,
                    totalPages = result.TotalPages
                }, "获取需求列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取需求列表失败");
                return StatusCode(500, ApiResponse<object>.Fail($"获取需求列表失败: {ex.Message}", 500));
            }
        }

        /// <summary>需求明细分页（须放在 {id} 之前，否则 "items" 会被当成 id）</summary>
        // GET api/v1/rfqs/items?...&salesUserId=&salesUserKeyword=&purchaserUserId=
        [HttpGet("items")]
        public async Task<ActionResult<ApiResponse<object>>> GetRFQItems(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? startDate = null,
            [FromQuery] string? endDate = null,
            [FromQuery] string? customerKeyword = null,
            [FromQuery] string? materialModel = null,
            [FromQuery] string? salesUserId = null,
            [FromQuery] string? salesUserKeyword = null,
            [FromQuery] string? purchaserUserId = null,
            [FromQuery] string? hasQuotesOnly = null)
        {
            try
            {
                var request = new RFQItemQueryRequest
                {
                    PageIndex = pageNumber,
                    PageSize = pageSize,
                    StartDate = string.IsNullOrEmpty(startDate) ? null : DateTime.Parse(startDate),
                    EndDate = string.IsNullOrEmpty(endDate) ? null : DateTime.Parse(endDate),
                    CustomerKeyword = customerKeyword,
                    MaterialModel = materialModel,
                    SalesUserId = salesUserId,
                    SalesUserKeyword = salesUserKeyword,
                    PurchaserUserId = purchaserUserId,
                    HasQuotesOnly = ParseQueryBool(hasQuotesOnly),
                    CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                };
                var result = await _rfqService.GetPagedItemsAsync(request);
                return Ok(ApiResponse<object>.Ok(new
                {
                    items = result.Items,
                    totalCount = result.TotalCount,
                    pageNumber = result.PageIndex,
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

        // GET api/v1/rfqs/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> GetRFQ(string id)
        {
            try
            {
                var rfq = await _rfqService.GetByIdAsync(id);
                if (rfq == null)
                    return NotFound(ApiResponse<object>.Fail("需求不存在", 404));
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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

        // POST api/v1/rfqs
        [HttpPost]
        [RequirePermission("rfq.write")]
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
                var rfq = await _rfqService.UpdateAsync(id, request);
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

                var rfq = await _rfqService.AssignPurchaserAsync(id, request);
                var resolvedPurchaserId = rfq.Items?.FirstOrDefault()?.AssignedPurchaserUserId1 ?? request.PurchaserId.Trim();
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
