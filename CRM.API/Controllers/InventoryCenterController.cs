using CRM.API.Models.DTOs;
using CRM.API.Utilities;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/inventory-center")]
    public class InventoryCenterController : ControllerBase
    {
        private readonly IInventoryCenterService _service;
        private readonly ILogger<InventoryCenterController> _logger;

        public InventoryCenterController(IInventoryCenterService service, ILogger<InventoryCenterController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("overview")]
        public async Task<ActionResult<ApiResponse<IEnumerable<InventoryMaterialOverviewDto>>>> GetOverview([FromQuery] string? warehouseId)
        {
            try
            {
                var list = await _service.GetMaterialOverviewAsync(warehouseId);
                return Ok(ApiResponse<IEnumerable<InventoryMaterialOverviewDto>>.Ok(list, "获取库存总览成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取库存总览失败");
                return StatusCode(500, ApiResponse<IEnumerable<InventoryMaterialOverviewDto>>.Fail($"获取库存总览失败: {ex.Message}", 500));
            }
        }

        [HttpGet("sell-order-items/{sellOrderItemId}/available-qty")]
        public async Task<ActionResult<ApiResponse<SellOrderLineAvailableQtyDto>>> GetAvailableQtyForSellOrderLine(string sellOrderItemId)
        {
            try
            {
                var dto = await _service.GetAvailableQtyForSellOrderItemAsync(sellOrderItemId);
                return Ok(ApiResponse<SellOrderLineAvailableQtyDto>.Ok(dto, "获取可出库数量成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取销售明细可用库存失败");
                return StatusCode(500, ApiResponse<SellOrderLineAvailableQtyDto>.Fail($"获取可用库存失败: {ex.Message}", 500));
            }
        }

        [HttpGet("stocks/{stockId}/stock-items")]
        public async Task<ActionResult<ApiResponse<IEnumerable<InventoryStockItemRowDto>>>> GetStockItemsForStock(string stockId)
        {
            try
            {
                var list = await _service.GetStockItemsForAggregateAsync(stockId);
                return Ok(ApiResponse<IEnumerable<InventoryStockItemRowDto>>.Ok(list, "获取库存明细成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取库存明细失败 StockId={StockId}", stockId);
                return StatusCode(500, ApiResponse<IEnumerable<InventoryStockItemRowDto>>.Fail($"获取库存明细失败: {ex.Message}", 500));
            }
        }

        [HttpGet("stock-items")]
        public async Task<ActionResult<ApiResponse<IEnumerable<InventoryStockItemListRowDto>>>> GetStockItemsList([FromQuery] InventoryStockItemListQuery? query)
        {
            try
            {
                var list = await _service.GetStockItemsListAsync(query);
                return Ok(ApiResponse<IEnumerable<InventoryStockItemListRowDto>>.Ok(list, "获取库存明细列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取库存明细列表失败");
                return StatusCode(500, ApiResponse<IEnumerable<InventoryStockItemListRowDto>>.Fail($"获取库存明细列表失败: {ex.Message}", 500));
            }
        }

        [HttpGet("materials/{materialId}/traces")]
        public async Task<ActionResult<ApiResponse<IEnumerable<InventoryMaterialTraceDto>>>> GetMaterialTrace(string materialId)
        {
            try
            {
                var list = await _service.GetMaterialTraceAsync(materialId);
                return Ok(ApiResponse<IEnumerable<InventoryMaterialTraceDto>>.Ok(list, "获取物料入库追溯成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取物料入库追溯失败");
                return StatusCode(500, ApiResponse<IEnumerable<InventoryMaterialTraceDto>>.Fail($"获取物料入库追溯失败: {ex.Message}", 500));
            }
        }

        [HttpGet("finance/summary")]
        public async Task<ActionResult<ApiResponse<InventoryFinanceSummaryDto>>> GetFinanceSummary([FromQuery] int stagnantDays = 90)
        {
            try
            {
                var dto = await _service.GetFinanceSummaryAsync(stagnantDays);
                return Ok(ApiResponse<InventoryFinanceSummaryDto>.Ok(dto, "获取库存财务分析成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取库存财务分析失败");
                return StatusCode(500, ApiResponse<InventoryFinanceSummaryDto>.Fail($"获取库存财务分析失败: {ex.Message}", 500));
            }
        }

        [HttpGet("warehouses")]
        public async Task<ActionResult<ApiResponse<IEnumerable<WarehouseInfo>>>> GetWarehouses()
        {
            try
            {
                var list = await _service.GetWarehousesAsync();
                return Ok(ApiResponse<IEnumerable<WarehouseInfo>>.Ok(list, "获取仓库列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取仓库列表失败");
                return StatusCode(500, ApiResponse<IEnumerable<WarehouseInfo>>.Fail($"获取仓库列表失败: {ex.Message}", 500));
            }
        }

        [HttpPost("warehouses")]
        public async Task<ActionResult<ApiResponse<WarehouseInfo>>> SaveWarehouse([FromBody] WarehouseInfo request)
        {
            try
            {
                var data = await _service.SaveWarehouseAsync(request);
                return Ok(ApiResponse<WarehouseInfo>.Ok(data, "保存仓库成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<WarehouseInfo>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存仓库失败");
                return StatusCode(500, ApiResponse<WarehouseInfo>.Fail($"保存仓库失败: {ex.Message}", 500));
            }
        }

        [HttpGet("picking-tasks")]
        public async Task<ActionResult<ApiResponse<IEnumerable<PickingTaskSummaryDto>>>> GetPickingTasks([FromQuery] short? status = null)
        {
            try
            {
                var list = await _service.GetPickingTasksAsync(status);
                return Ok(ApiResponse<IEnumerable<PickingTaskSummaryDto>>.Ok(list, "获取拣货任务成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取拣货任务失败");
                return StatusCode(500, ApiResponse<IEnumerable<PickingTaskSummaryDto>>.Fail($"获取拣货任务失败: {ex.Message}", 500));
            }
        }

        [HttpPost("picking-tasks/generate")]
        public async Task<ActionResult<ApiResponse<PickingTask>>> GeneratePickingTask([FromBody] GeneratePickingTaskRequest request)
        {
            try
            {
                var task = await _service.GeneratePickingTaskAsync(request);
                return Ok(ApiResponse<PickingTask>.Ok(task, "生成拣货任务成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<PickingTask>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<PickingTask>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成拣货任务失败");
                var detail = ApiExceptionMessages.FormatWithDatabaseInner(ex);
                return StatusCode(500, ApiResponse<PickingTask>.Fail($"生成拣货任务失败: {detail}", 500));
            }
        }

        [HttpPost("picking-tasks/{taskId}/complete")]
        public async Task<ActionResult<ApiResponse<object>>> CompletePickingTask(string taskId)
        {
            try
            {
                await _service.CompletePickingTaskAsync(taskId);
                return Ok(ApiResponse<object>.Ok(null, "拣货任务已完成"));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成拣货任务失败");
                return StatusCode(500, ApiResponse<object>.Fail($"完成拣货任务失败: {ex.Message}", 500));
            }
        }

        [HttpGet("count-plans")]
        public async Task<ActionResult<ApiResponse<IEnumerable<InventoryCountPlan>>>> GetCountPlans()
        {
            try
            {
                var list = await _service.GetCountPlansAsync();
                return Ok(ApiResponse<IEnumerable<InventoryCountPlan>>.Ok(list, "获取盘点计划成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取盘点计划失败");
                return StatusCode(500, ApiResponse<IEnumerable<InventoryCountPlan>>.Fail($"获取盘点计划失败: {ex.Message}", 500));
            }
        }

        [HttpPost("count-plans")]
        public async Task<ActionResult<ApiResponse<InventoryCountPlan>>> CreateCountPlan([FromBody] CreateCountPlanRequest request)
        {
            try
            {
                var data = await _service.CreateMonthlyCountPlanAsync(request);
                return Ok(ApiResponse<InventoryCountPlan>.Ok(data, "创建盘点计划成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<InventoryCountPlan>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<InventoryCountPlan>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建盘点计划失败");
                return StatusCode(500, ApiResponse<InventoryCountPlan>.Fail($"创建盘点计划失败: {ex.Message}", 500));
            }
        }

        [HttpPost("count-plans/submit")]
        public async Task<ActionResult<ApiResponse<object>>> SubmitCountPlan([FromBody] SubmitCountPlanRequest request)
        {
            try
            {
                await _service.SubmitCountPlanAsync(request);
                return Ok(ApiResponse<object>.Ok(null, "提交盘点成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提交盘点失败");
                return StatusCode(500, ApiResponse<object>.Fail($"提交盘点失败: {ex.Message}", 500));
            }
        }
    }
}

