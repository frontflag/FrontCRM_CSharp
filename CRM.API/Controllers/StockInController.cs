using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using CRM.API.Models.DTOs;
using CRM.API.Utilities;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Utilities;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/stock-in")]
    public class StockInController : ControllerBase
    {
        private readonly IStockInService _service;
        private readonly IRbacService _rbacService;
        private readonly ILogger<StockInController> _logger;

        public StockInController(
            IStockInService service,
            IRbacService rbacService,
            ILogger<StockInController> logger)
        {
            _service = service;
            _rbacService = rbacService;
            _logger = logger;
        }

        public class ForceDeleteStockInRequest
        {
            public string ConfirmBillCode { get; set; } = string.Empty;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? model,
            [FromQuery] string? vendorName,
            [FromQuery] string? purchaseOrderCode,
            [FromQuery] string? salesOrderCode,
            [FromQuery] string? stockInCode,
            [FromQuery] string? sourceDisplayNo,
            [FromQuery] string? warehouseId,
            [FromQuery] DateTime? stockInDateStart,
            [FromQuery] DateTime? stockInDateEnd,
            [FromQuery] string? remark,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = new StockInQueryRequest
                {
                    Model = model,
                    VendorName = vendorName,
                    PurchaseOrderCode = purchaseOrderCode,
                    SalesOrderCode = salesOrderCode,
                    StockInCode = stockInCode,
                    SourceDisplayNo = sourceDisplayNo,
                    WarehouseId = warehouseId,
                    StockInDateStart = stockInDateStart,
                    StockInDateEnd = stockInDateEnd,
                    Remark = remark
                };
                var result = await _service.GetListPagedAsync(query, page, pageSize, cancellationToken);
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        items = result.Items,
                        total = result.TotalCount,
                        page = result.PageIndex,
                        pageSize = result.PageSize
                    },
                    message = "获取入库单列表成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取入库单列表失败");
                return StatusCode(500, new { success = false, message = $"获取入库单列表失败: {ex.Message}" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<StockIn>>> GetById(string id)
        {
            try
            {
                var entity = await _service.GetByIdAsync(id);
                if (entity == null)
                    return NotFound(ApiResponse<StockIn>.Fail("入库单不存在", 404));
                if (await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User))
                    PurchaseSensitiveFieldMask511.ApplyStockIn(entity, true);
                return Ok(ApiResponse<StockIn>.Ok(entity, "获取入库单成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取入库单失败");
                return StatusCode(500, ApiResponse<StockIn>.Fail($"获取入库单失败: {ex.Message}", 500));
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<StockIn>>> Create([FromBody] CreateStockInRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<StockIn>.Fail("请求体不能为空", 400));
                if (string.IsNullOrWhiteSpace(request.OperatorId))
                    request.OperatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var entity = await _service.CreateAsync(request, actorId);
                return Ok(ApiResponse<StockIn>.Ok(entity, "创建入库单成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<StockIn>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<StockIn>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建入库单失败");
                var detail = ex.InnerException?.Message;
                var message = string.IsNullOrWhiteSpace(detail)
                    ? $"创建入库单失败: {ex.Message}"
                    : $"创建入库单失败: {detail}";
                return StatusCode(500, ApiResponse<StockIn>.Fail(message, 500));
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<StockIn>>> Update(string id, [FromBody] UpdateStockInRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<StockIn>.Fail("请求体不能为空", 400));
                var entity = await _service.UpdateAsync(id, request);
                return Ok(ApiResponse<StockIn>.Ok(entity, "更新入库单成功"));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse<StockIn>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新入库单失败");
                return StatusCode(500, ApiResponse<StockIn>.Fail($"更新入库单失败: {ex.Message}", 500));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(string id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok(ApiResponse<object>.Ok(null, "删除入库单成功"));
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
                _logger.LogError(ex, "删除入库单失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除入库单失败: {ex.Message}", 500));
            }
        }

        [HttpPost("{id}/force-delete")]
        public async Task<ActionResult<ApiResponse<object>>> ForceDelete(string id, [FromBody] ForceDeleteStockInRequest? body)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return StatusCode(403, ApiResponse<object>.Fail("未登录或身份无效", 403));

                var summary = await _rbacService.GetUserPermissionSummaryAsync(userId.Trim());
                if (summary?.IsSysAdmin != true)
                    return StatusCode(403, ApiResponse<object>.Fail("仅系统管理员可执行强制删除", 403));

                if (body == null || string.IsNullOrWhiteSpace(body.ConfirmBillCode))
                    return BadRequest(ApiResponse<object>.Fail("请填写 confirmBillCode", 400));

                var userName = User.FindFirst(ClaimTypes.Name)?.Value;
                await _service.ForceDeleteAsync(
                    id,
                    body.ConfirmBillCode.Trim(),
                    userId.Trim(),
                    string.IsNullOrWhiteSpace(userName) ? null : userName.Trim());

                return Ok(ApiResponse<object>.Ok(null, "强制删除入库单成功"));
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
                _logger.LogError(ex, "强制删除入库单失败");
                return StatusCode(500, ApiResponse<object>.Fail($"强制删除入库单失败: {ex.Message}", 500));
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateStatus(string id, [FromQuery] short status)
        {
            try
            {
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.UpdateStatusAsync(id, status, actorId);
                return Ok(ApiResponse<object>.Ok(null, "更新状态成功"));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新入库单状态失败");
                return StatusCode(500, ApiResponse<object>.Fail($"更新状态失败: {ex.Message}", 500));
            }
        }
    }
}
