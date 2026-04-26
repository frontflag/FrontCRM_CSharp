using System.Linq;
using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.API.Utilities;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/inventory-center")]
    public class InventoryCenterController : ControllerBase
    {
        private readonly IInventoryCenterService _service;
        private readonly IRepository<StockInfo> _stockRepo;
        private readonly IRepository<StockItem> _stockItemRepo;
        private readonly IRepository<PickingTask> _pickingTaskRepo;
        private readonly IRepository<PickingTaskItem> _pickingTaskItemRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRbacService _rbacService;
        private readonly ILogger<InventoryCenterController> _logger;

        public InventoryCenterController(
            IInventoryCenterService service,
            IRepository<StockInfo> stockRepo,
            IRepository<StockItem> stockItemRepo,
            IRepository<PickingTask> pickingTaskRepo,
            IRepository<PickingTaskItem> pickingTaskItemRepo,
            IUnitOfWork unitOfWork,
            IRbacService rbacService,
            ILogger<InventoryCenterController> logger)
        {
            _service = service;
            _stockRepo = stockRepo;
            _stockItemRepo = stockItemRepo;
            _pickingTaskRepo = pickingTaskRepo;
            _pickingTaskItemRepo = pickingTaskItemRepo;
            _unitOfWork = unitOfWork;
            _rbacService = rbacService;
            _logger = logger;
        }

        public class ForceDeleteInventoryRequest
        {
            public string ConfirmBillCode { get; set; } = string.Empty;
        }

        [HttpGet("overview")]
        public async Task<ActionResult<ApiResponse<IEnumerable<InventoryMaterialOverviewDto>>>> GetOverview([FromQuery] string? warehouseId)
        {
            try
            {
                var list = await _service.GetMaterialOverviewAsync(warehouseId);
                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                {
                    var masked = list.ToList();
                    SaleSensitiveFieldMask521.ApplyInventoryMaterialOverviews(masked, true);
                    list = masked;
                }

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
                if (await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User))
                {
                    var masked = list.ToList();
                    PurchaseSensitiveFieldMask511.ApplyInventoryStockItemRows(masked, true);
                    list = masked;
                }

                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                {
                    var masked2 = list.ToList();
                    SaleSensitiveFieldMask521.ApplyInventoryStockItemRows(masked2, true);
                    list = masked2;
                }

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
                if (await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User))
                {
                    var masked = list.ToList();
                    PurchaseSensitiveFieldMask511.ApplyInventoryStockItemListRows(masked, true);
                    list = masked;
                }

                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                {
                    var masked2 = list.ToList();
                    SaleSensitiveFieldMask521.ApplyInventoryStockItemListRows(masked2, true);
                    list = masked2;
                }

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
                if (await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User))
                {
                    var masked = list.ToList();
                    PurchaseSensitiveFieldMask511.ApplyInventoryMaterialTraces(masked, true);
                    list = masked;
                }

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
                if (await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User))
                    PurchaseSensitiveFieldMask511.ApplyInventoryFinanceSummary(dto, true);
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

        /// <summary>拣货单列表（出库通知 + 仓库 + 订单展示列）。</summary>
        [HttpGet("picking-list")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<PickingTaskListItemDto>>>> GetPickingTaskList()
        {
            try
            {
                var list = await _service.GetPickingTaskListRowsAsync();
                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                {
                    var masked = list.ToList();
                    SaleSensitiveFieldMask521.ApplyPickingTaskListItems(masked, true);
                    list = masked;
                }

                return Ok(ApiResponse<IReadOnlyList<PickingTaskListItemDto>>.Ok(list, "获取拣货单列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取拣货单列表失败");
                return StatusCode(500, ApiResponse<IReadOnlyList<PickingTaskListItemDto>>.Fail($"获取拣货单列表失败: {ex.Message}", 500));
            }
        }

        /// <summary>拣货单详情（头信息 + 明细行）。</summary>
        [HttpGet("picking-list/{id}")]
        public async Task<ActionResult<ApiResponse<PickingTaskDetailViewDto>>> GetPickingTaskListDetail(string id)
        {
            try
            {
                var dto = await _service.GetPickingTaskDetailForUiAsync(id);
                if (dto == null)
                    return NotFound(ApiResponse<PickingTaskDetailViewDto>.Fail("拣货单不存在", 404));
                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                    SaleSensitiveFieldMask521.ApplyPickingTaskDetailView(dto, true);
                return Ok(ApiResponse<PickingTaskDetailViewDto>.Ok(dto, "获取拣货单详情成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<PickingTaskDetailViewDto>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取拣货单详情失败 Id={Id}", id);
                return StatusCode(500, ApiResponse<PickingTaskDetailViewDto>.Fail($"获取拣货单详情失败: {ex.Message}", 500));
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

        /// <summary>出库拣货：可拣 <c>stockitem</c> 候选列表（FIFO 仅排序）。</summary>
        [HttpGet("picking-candidates")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<PickingStockItemCandidateDto>>>> GetPickingCandidates(
            [FromQuery] string stockOutRequestId,
            [FromQuery] string warehouseId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(stockOutRequestId))
                    return BadRequest(ApiResponse<IReadOnlyList<PickingStockItemCandidateDto>>.Fail("stockOutRequestId 不能为空", 400));
                if (string.IsNullOrWhiteSpace(warehouseId))
                    return BadRequest(ApiResponse<IReadOnlyList<PickingStockItemCandidateDto>>.Fail("warehouseId 不能为空", 400));

                var list = await _service.GetPickingCandidateStockItemsAsync(stockOutRequestId.Trim(), warehouseId.Trim());
                return Ok(ApiResponse<IReadOnlyList<PickingStockItemCandidateDto>>.Ok(list, "获取拣货候选成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<IReadOnlyList<PickingStockItemCandidateDto>>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<IReadOnlyList<PickingStockItemCandidateDto>>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取拣货候选失败");
                return StatusCode(500, ApiResponse<IReadOnlyList<PickingStockItemCandidateDto>>.Fail($"获取拣货候选失败: {ex.Message}", 500));
            }
        }

        /// <summary>保存/覆盖拣货任务明细（须与出库通知数量一致）。</summary>
        [HttpPost("picking-tasks/{taskId}/items")]
        public async Task<ActionResult<ApiResponse<object>>> SavePickingTaskItems(
            string taskId,
            [FromBody] IReadOnlyList<SavePickingTaskItemLineRequest>? lines)
        {
            try
            {
                if (lines == null || lines.Count == 0)
                    return BadRequest(ApiResponse<object>.Fail("拣货明细不能为空", 400));

                await _service.SavePickingTaskItemsAsync(taskId.Trim(), lines);
                return Ok(ApiResponse<object>.Ok(null, "拣货明细已保存"));
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
                _logger.LogError(ex, "保存拣货明细失败 TaskId={TaskId}", taskId);
                return StatusCode(500, ApiResponse<object>.Fail($"保存拣货明细失败: {ex.Message}", 500));
            }
        }

        [HttpDelete("stocks/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteStock(string id)
        {
            try
            {
                var stock = await _stockRepo.GetByIdAsync(id);
                if (stock == null)
                    return NotFound(ApiResponse<object>.Fail("库存不存在", 404));

                if (stock.QtyRepertory > 0 || stock.QtyRepertoryAvailable > 0)
                    return BadRequest(ApiResponse<object>.Fail("当前库存数量大于 0，不能普通删除", 400));

                var stockItems = (await _stockItemRepo.FindAsync(x => x.StockAggregateId == stock.Id)).ToList();
                foreach (var item in stockItems)
                {
                    if (item.QtyRepertory > 0 || item.QtyRepertoryAvailable > 0)
                        return BadRequest(ApiResponse<object>.Fail("存在在库明细数量大于 0，不能普通删除", 400));
                }
                foreach (var item in stockItems)
                    await _stockItemRepo.DeleteAsync(item.Id);

                await _stockRepo.DeleteAsync(stock.Id);
                await _unitOfWork.ExecuteNonQueryAsync(
                    $@"UPDATE public.stock_extend SET is_deleted = true, ""ModifyTime"" = NOW() WHERE ""StockId"" = '{SqlEscape(stock.Id)}'");
                await _unitOfWork.SaveChangesAsync();
                return Ok(ApiResponse<object>.Ok(null, "删除库存成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除库存失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除库存失败: {ex.Message}", 500));
            }
        }

        [HttpPost("stocks/{id}/force-delete")]
        public async Task<ActionResult<ApiResponse<object>>> ForceDeleteStock(string id, [FromBody] ForceDeleteInventoryRequest? body)
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

                var stock = await _stockRepo.GetByIdAsync(id);
                if (stock == null)
                    return NotFound(ApiResponse<object>.Fail("库存不存在", 404));

                var targetCode = !string.IsNullOrWhiteSpace(stock.StockCode) ? stock.StockCode.Trim() : stock.Id.Trim();
                if (!string.Equals(body.ConfirmBillCode.Trim(), targetCode, StringComparison.Ordinal))
                    return BadRequest(ApiResponse<object>.Fail("确认编号不匹配，已拒绝删除", 400));

                var stockItems = (await _stockItemRepo.FindAsync(x => x.StockAggregateId == stock.Id)).ToList();
                foreach (var item in stockItems)
                    await _stockItemRepo.DeleteAsync(item.Id);
                await _stockRepo.DeleteAsync(stock.Id);
                await _unitOfWork.ExecuteNonQueryAsync(
                    $@"UPDATE public.stock_extend SET is_deleted = true, ""ModifyTime"" = NOW() WHERE ""StockId"" = '{SqlEscape(stock.Id)}'");
                await _unitOfWork.SaveChangesAsync();
                return Ok(ApiResponse<object>.Ok(null, "强制删除库存成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "强制删除库存失败");
                return StatusCode(500, ApiResponse<object>.Fail($"强制删除库存失败: {ex.Message}", 500));
            }
        }

        [HttpDelete("picking-list/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeletePickingSlip(string id)
        {
            try
            {
                var task = await _pickingTaskRepo.GetByIdAsync(id);
                if (task == null)
                    return NotFound(ApiResponse<object>.Fail("拣货单不存在", 404));

                if (task.Status != 1)
                    return BadRequest(ApiResponse<object>.Fail("仅待拣货状态可普通删除", 400));

                var items = (await _pickingTaskItemRepo.FindAsync(x => x.PickingTaskId == task.Id)).ToList();
                foreach (var item in items)
                    await _pickingTaskItemRepo.DeleteAsync(item.Id);
                await _pickingTaskRepo.DeleteAsync(task.Id);
                await _unitOfWork.SaveChangesAsync();
                return Ok(ApiResponse<object>.Ok(null, "删除拣货单成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除拣货单失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除拣货单失败: {ex.Message}", 500));
            }
        }

        [HttpPost("picking-list/{id}/force-delete")]
        public async Task<ActionResult<ApiResponse<object>>> ForceDeletePickingSlip(string id, [FromBody] ForceDeleteInventoryRequest? body)
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

                var task = await _pickingTaskRepo.GetByIdAsync(id);
                if (task == null)
                    return NotFound(ApiResponse<object>.Fail("拣货单不存在", 404));

                if (!string.Equals(body.ConfirmBillCode.Trim(), task.TaskCode.Trim(), StringComparison.Ordinal))
                    return BadRequest(ApiResponse<object>.Fail("确认单号不匹配，已拒绝删除", 400));

                var items = (await _pickingTaskItemRepo.FindAsync(x => x.PickingTaskId == task.Id)).ToList();
                foreach (var item in items)
                    await _pickingTaskItemRepo.DeleteAsync(item.Id);
                await _pickingTaskRepo.DeleteAsync(task.Id);
                await _unitOfWork.SaveChangesAsync();
                return Ok(ApiResponse<object>.Ok(null, "强制删除拣货单成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "强制删除拣货单失败");
                return StatusCode(500, ApiResponse<object>.Fail($"强制删除拣货单失败: {ex.Message}", 500));
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

        private static string SqlEscape(string s) => s.Replace("'", "''", StringComparison.Ordinal);
    }
}

