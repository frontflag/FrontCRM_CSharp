using Microsoft.AspNetCore.Mvc;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/stock-out")]
    public class StockOutController : ControllerBase
    {
        private readonly IStockOutService _service;
        private readonly ILogger<StockOutController> _logger;

        public StockOutController(IStockOutService service, ILogger<StockOutController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<StockOut>>>> GetAll()
        {
            try
            {
                var list = await _service.GetAllAsync();
                return Ok(ApiResponse<IEnumerable<StockOut>>.Ok(list, "获取出库单列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取出库单列表失败");
                return StatusCode(500, ApiResponse<IEnumerable<StockOut>>.Fail($"获取出库单列表失败: {ex.Message}", 500));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<StockOut>>> GetById(string id)
        {
            try
            {
                var entity = await _service.GetByIdAsync(id);
                if (entity == null)
                    return NotFound(ApiResponse<StockOut>.Fail("出库单不存在", 404));
                return Ok(ApiResponse<StockOut>.Ok(entity, "获取出库单成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取出库单失败");
                return StatusCode(500, ApiResponse<StockOut>.Fail($"获取出库单失败: {ex.Message}", 500));
            }
        }

        [HttpPost("request")]
        public async Task<ActionResult<ApiResponse<StockOutRequest>>> CreateRequest([FromBody] StockOutRequestCreateApiRequest? body)
        {
            try
            {
                if (body == null)
                    return BadRequest(ApiResponse<StockOutRequest>.Fail("请求体不能为空", 400));
                var request = new CreateStockOutRequestRequest
                {
                    RequestCode = body.RequestCode ?? string.Empty,
                    SalesOrderId = body.SalesOrderId ?? string.Empty,
                    SalesOrderItemId = body.SalesOrderItemId ?? string.Empty,
                    MaterialCode = body.MaterialCode ?? string.Empty,
                    MaterialName = body.MaterialName ?? string.Empty,
                    Quantity = body.Quantity,
                    CustomerId = body.CustomerId ?? string.Empty,
                    RequestUserId = body.RequestUserId ?? string.Empty,
                    RequestDate = body.RequestDate,
                    Remark = body.Remark,
                };
                var entity = await _service.CreateStockOutRequestAsync(request);
                return Ok(ApiResponse<StockOutRequest>.Ok(entity, "创建出库申请成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<StockOutRequest>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<StockOutRequest>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建出库申请失败");
                return StatusCode(500, ApiResponse<StockOutRequest>.Fail($"创建出库申请失败: {ex.Message}", 500));
            }
        }

        [HttpGet("request")]
        public async Task<ActionResult<ApiResponse<IEnumerable<StockOutRequestListItemDto>>>> GetRequests()
        {
            try
            {
                var list = await _service.GetStockOutRequestListAsync();
                return Ok(ApiResponse<IEnumerable<StockOutRequestListItemDto>>.Ok(list, "获取出库通知列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取出库通知列表失败");
                return StatusCode(500, ApiResponse<IEnumerable<StockOutRequestListItemDto>>.Fail($"获取出库通知列表失败: {ex.Message}", 500));
            }
        }

        [HttpPost("execute")]
        public async Task<ActionResult<ApiResponse<StockOut>>> Execute([FromBody] ExecuteStockOutRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<StockOut>.Fail("请求体不能为空", 400));
                var entity = await _service.ExecuteStockOutAsync(request);
                return Ok(ApiResponse<StockOut>.Ok(entity, "执行出库成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<StockOut>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<StockOut>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行出库失败");
                return StatusCode(500, ApiResponse<StockOut>.Fail($"执行出库失败: {ex.Message}", 500));
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateStatus(string id, [FromQuery] short status)
        {
            try
            {
                await _service.UpdateStatusAsync(id, status);
                return Ok(ApiResponse<object>.Ok(null, "更新状态成功"));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新出库单状态失败");
                return StatusCode(500, ApiResponse<object>.Fail($"更新状态失败: {ex.Message}", 500));
            }
        }
    }
}
