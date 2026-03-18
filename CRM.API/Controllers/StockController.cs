using Microsoft.AspNetCore.Mvc;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/stock")]
    public class StockController : ControllerBase
    {
        private readonly IStockService _service;
        private readonly ILogger<StockController> _logger;

        public StockController(IStockService service, ILogger<StockController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<StockInfo>>>> GetAll([FromQuery] string? warehouseId = null)
        {
            try
            {
                var list = string.IsNullOrWhiteSpace(warehouseId)
                    ? await _service.GetAllAsync()
                    : await _service.GetByWarehouseIdAsync(warehouseId);
                return Ok(ApiResponse<IEnumerable<StockInfo>>.Ok(list, "获取库存列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取库存列表失败");
                return StatusCode(500, ApiResponse<IEnumerable<StockInfo>>.Fail($"获取库存列表失败: {ex.Message}", 500));
            }
        }
    }
}
