using Microsoft.AspNetCore.Mvc;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/stock-in")]
    public class StockInController : ControllerBase
    {
        private readonly IStockInService _service;
        private readonly ILogger<StockInController> _logger;

        public StockInController(IStockInService service, ILogger<StockInController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<StockIn>>>> GetAll()
        {
            try
            {
                var list = await _service.GetAllAsync();
                return Ok(ApiResponse<IEnumerable<StockIn>>.Ok(list, "获取入库单列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取入库单列表失败");
                return StatusCode(500, ApiResponse<IEnumerable<StockIn>>.Fail($"获取入库单列表失败: {ex.Message}", 500));
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
                var entity = await _service.CreateAsync(request);
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
                return StatusCode(500, ApiResponse<StockIn>.Fail($"创建入库单失败: {ex.Message}", 500));
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
                _logger.LogError(ex, "更新入库单状态失败");
                return StatusCode(500, ApiResponse<object>.Fail($"更新状态失败: {ex.Message}", 500));
            }
        }
    }
}
