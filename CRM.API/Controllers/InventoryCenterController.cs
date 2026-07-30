using System.Linq;
using System.Security.Claims;
using CRM.API.Authorization;
using System.Text;
using System.Threading;
using CRM.API.Models.DTOs;
using CRM.API.Utilities;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Services;
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
        private readonly IRepository<StockOutItem> _stockOutItemRepo;
        private readonly IRepository<PickingTask> _pickingTaskRepo;
        private readonly IRepository<PickingTaskItem> _pickingTaskItemRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRbacService _rbacService;
        private readonly IExportOperationLogService _exportLog;
        private readonly ILogger<InventoryCenterController> _logger;

        public InventoryCenterController(
            IInventoryCenterService service,
            IRepository<StockInfo> stockRepo,
            IRepository<StockItem> stockItemRepo,
            IRepository<StockOutItem> stockOutItemRepo,
            IRepository<PickingTask> pickingTaskRepo,
            IRepository<PickingTaskItem> pickingTaskItemRepo,
            IUnitOfWork unitOfWork,
            IRbacService rbacService,
            IExportOperationLogService exportLog,
            ILogger<InventoryCenterController> logger)
        {
            _service = service;
            _stockRepo = stockRepo;
            _stockItemRepo = stockItemRepo;
            _stockOutItemRepo = stockOutItemRepo;
            _pickingTaskRepo = pickingTaskRepo;
            _pickingTaskItemRepo = pickingTaskItemRepo;
            _unitOfWork = unitOfWork;
            _rbacService = rbacService;
            _exportLog = exportLog;
            _logger = logger;
        }

        public class ForceDeleteInventoryRequest
        {
            public string ConfirmBillCode { get; set; } = string.Empty;
        }

        [HttpGet("overview")]
        public async Task<ActionResult<ApiResponse<IEnumerable<InventoryMaterialOverviewDto>>>> GetOverview(
            [FromQuery] string? warehouseId,
            [FromQuery] string? materialModel,
            [FromQuery] string? stockCode)
        {
            try
            {
                var list = await _service.GetMaterialOverviewAsync(warehouseId, materialModel, stockCode);
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

        /// <summary>库存总览列表（数据库分页；<c>stockType</c> 1/2/3 与前端库存类型筛选一致）。</summary>
        [HttpGet("overview/paged")]
        public async Task<IActionResult> GetOverviewPaged(
            [FromQuery] string? warehouseId,
            [FromQuery] string? materialModel,
            [FromQuery] string? stockCode,
            [FromQuery] short? stockType,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _service.GetMaterialOverviewPagedAsync(
                    warehouseId,
                    materialModel,
                    stockCode,
                    stockType,
                    page,
                    pageSize,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    cancellationToken);
                var items = result.Items.ToList();
                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                    SaleSensitiveFieldMask521.ApplyInventoryMaterialOverviews(items, true);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        items,
                        total = result.TotalCount,
                        page = result.PageIndex,
                        pageSize = result.PageSize
                    },
                    message = "获取库存总览成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取库存总览分页失败");
                return StatusCode(500, new { success = false, message = $"获取库存总览失败: {ex.Message}" });
            }
        }

        /// <summary>按当前筛选导出库存中心列表 CSV，并写入操作审计。</summary>
        [HttpGet("overview/export")]
        public async Task<IActionResult> ExportOverview(
            [FromQuery] string? warehouseId,
            [FromQuery] string? materialModel,
            [FromQuery] string? stockCode,
            [FromQuery] short? stockType,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = InventoryExportHttp.UserId(User);
                var mask521 = await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User);

                var (items, truncated, _) = await InventoryExportHttp.CollectForExportAsync(
                    (page, pageSize, ct) => _service.GetMaterialOverviewPagedAsync(
                        warehouseId, materialModel, stockCode, stockType, page, pageSize, userId, ct),
                    cancellationToken: cancellationToken);

                if (mask521)
                    SaleSensitiveFieldMask521.ApplyInventoryMaterialOverviews(items, true);

                var sb = new StringBuilder();
                sb.AppendLine(string.Join(',',
                    "库存类型", "物料型号", "品牌", "在库数量", "可用数量", "锁定数量", "库存金额", "币别",
                    "仓库", "区域", "最近变动", "库存编码", "创建时间", "创建人"));

                foreach (var r in items)
                {
                    sb.AppendLine(string.Join(',',
                        InventoryExportHttp.CsvCell(r.StockType.ToString()),
                        InventoryExportHttp.CsvCell(r.MaterialModel),
                        InventoryExportHttp.CsvCell(r.MaterialName),
                        InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDecimal(r.OnHandQty)),
                        InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDecimal(r.AvailableQty)),
                        InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDecimal(r.LockedQty)),
                        InventoryExportHttp.CsvCell(mask521 ? string.Empty : InventoryExportHttp.FormatDecimal(r.InventoryAmount)),
                        InventoryExportHttp.CsvCell(r.Currency.ToString()),
                        InventoryExportHttp.CsvCell(r.WarehouseCode),
                        InventoryExportHttp.CsvCell(r.RegionType.ToString()),
                        InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDateTime(r.LastMoveTime)),
                        InventoryExportHttp.CsvCell(r.StockCode),
                        InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDateTime(r.CreateTime)),
                        InventoryExportHttp.CsvCell(r.CreateUserName)));
                }

                var filters = ExportOperationAudit.NormalizeFilters(new Dictionary<string, object?>
                {
                    ["warehouseId"] = warehouseId,
                    ["materialModel"] = materialModel,
                    ["stockCode"] = stockCode,
                    ["stockType"] = stockType
                });

                var truncNote = truncated ? "（已截断）" : string.Empty;
                await _exportLog.AppendAsync(new ExportOperationLogRequest
                {
                    BizType = BusinessLogTypes.InventoryStock,
                    RecordId = ExportOperationAudit.ListRecordId,
                    RecordCode = ExportOperationAudit.InventoryStockListRecordCode,
                    ActionType = InventoryExportActionTypes.InventoryStockListExport,
                    ExportKind = ExportAuditKinds.InventoryStockList,
                    OperationDesc = $"导出库存中心列表 {items.Count} 条{truncNote}",
                    ExportedCount = items.Count,
                    Truncated = truncated,
                    Filters = filters,
                    FiltersMasked = mask521,
                    OperatorUserId = userId,
                    OperatorUserName = InventoryExportHttp.UserName(User)
                }, cancellationToken);

                return InventoryExportHttp.CsvFile(sb.ToString(), "库存中心列表.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出库存中心列表失败");
                return StatusCode(500, new { success = false, message = $"导出库存中心列表失败: {ex.Message}" });
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
                var list = await _service.GetStockItemsForAggregateAsync(
                    stockId,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
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
        public async Task<IActionResult> GetStockItemsList(
            [FromQuery] InventoryStockItemListQuery? query,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                query ??= new InventoryStockItemListQuery();
                query.CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var result = await _service.GetStockItemsListPagedAsync(query, page, pageSize, cancellationToken);
                var items = result.Items.ToList();
                if (await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User))
                    PurchaseSensitiveFieldMask511.ApplyInventoryStockItemListRows(items, true);

                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                    SaleSensitiveFieldMask521.ApplyInventoryStockItemListRows(items, true);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        items,
                        total = result.TotalCount,
                        page = result.PageIndex,
                        pageSize = result.PageSize
                    },
                    message = "获取库存明细列表成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取库存明细列表失败");
                return StatusCode(500, new { success = false, message = $"获取库存明细列表失败: {ex.Message}" });
            }
        }

        /// <summary>按当前筛选导出库存明细列表 CSV，并写入操作审计。</summary>
        [HttpGet("stock-items/export")]
        public async Task<IActionResult> ExportStockItemsList(
            [FromQuery] InventoryStockItemListQuery? query,
            CancellationToken cancellationToken = default)
        {
            try
            {
                query ??= new InventoryStockItemListQuery();
                query.CurrentUserId = InventoryExportHttp.UserId(User);
                var mask511 = await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User);
                var mask521 = await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User);

                var (items, truncated, _) = await InventoryExportHttp.CollectForExportAsync(
                    (page, pageSize, ct) => _service.GetStockItemsListPagedAsync(query, page, pageSize, ct),
                    cancellationToken: cancellationToken);

                if (mask511)
                    PurchaseSensitiveFieldMask511.ApplyInventoryStockItemListRows(items, true);
                if (mask521)
                    SaleSensitiveFieldMask521.ApplyInventoryStockItemListRows(items, true);

                static string OutboundStatusText(short status) => status switch
                {
                    1 => "未出库",
                    2 => "部分出库",
                    3 => "出库完成",
                    _ => string.Empty
                };

                static string RegionTypeText(short regionType) =>
                    regionType == RegionTypeCode.Overseas ? "境外" : "境内";

                var sb = new StringBuilder();
                sb.AppendLine(string.Join(',',
                    "出库状态", "库存明细编号", "入库单号", "入库日期", "仓库", "地域",
                    "物料型号", "品牌", "入库量", "已出库", "在库",
                    "供应商", "采购员", "采购明细编号", "货代单号",
                    "客户", "业务员", "销售明细编号", "批次", "库位", "入库毛利快照(USD)"));

                foreach (var r in items)
                {
                    sb.AppendLine(string.Join(',',
                        InventoryExportHttp.CsvCell(OutboundStatusText(r.OutboundStatus)),
                        InventoryExportHttp.CsvCell(r.StockItemCode),
                        InventoryExportHttp.CsvCell(r.StockInCode),
                        InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDate(r.StockInDate)),
                        InventoryExportHttp.CsvCell(r.WarehouseName ?? r.WarehouseCode),
                        InventoryExportHttp.CsvCell(RegionTypeText(r.RegionType)),
                        InventoryExportHttp.CsvCell(r.PurchasePn),
                        InventoryExportHttp.CsvCell(r.PurchaseBrand),
                        InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDecimal(r.QtyInbound)),
                        InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDecimal(r.QtyStockOut)),
                        InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDecimal(r.QtyRepertory)),
                        InventoryExportHttp.CsvCell(mask511 ? "***" : r.VendorName),
                        InventoryExportHttp.CsvCell(r.PurchaserName),
                        InventoryExportHttp.CsvCell(r.PurchaseOrderItemCode),
                        InventoryExportHttp.CsvCell(r.FreightForwarderOrderNo),
                        InventoryExportHttp.CsvCell(mask521 ? "***" : r.CustomerName),
                        InventoryExportHttp.CsvCell(mask521 ? "***" : r.SalespersonName),
                        InventoryExportHttp.CsvCell(r.SellOrderItemCode),
                        InventoryExportHttp.CsvCell(r.BatchNo),
                        InventoryExportHttp.CsvCell(r.LocationId),
                        InventoryExportHttp.CsvCell(
                            mask511 || mask521
                                ? string.Empty
                                : InventoryExportHttp.FormatDecimal(r.ProfitOutBizUsd))));
                }

                var filters = ExportOperationAudit.NormalizeFilters(new Dictionary<string, object?>
                {
                    ["stockInCode"] = query.StockInCode,
                    ["stockItemCode"] = query.StockItemCode,
                    ["freightForwarderOrderNo"] = query.FreightForwarderOrderNo,
                    ["stockInDateFrom"] = query.StockInDateFrom,
                    ["stockInDateTo"] = query.StockInDateTo,
                    ["warehouseId"] = query.WarehouseId,
                    ["purchasePn"] = query.PurchasePn,
                    ["purchaseBrand"] = query.PurchaseBrand,
                    ["outboundStatus"] = query.OutboundStatus,
                    ["repertoryHasStock"] = query.RepertoryHasStock,
                    ["customerName"] = mask521 ? null : query.CustomerName,
                    ["vendorName"] = mask511 ? null : query.VendorName,
                    ["salespersonUserId"] = mask521 ? null : query.SalespersonUserId,
                    ["purchaserUserId"] = query.PurchaserUserId,
                    ["salespersonName"] = mask521 ? null : query.SalespersonName,
                    ["purchaserName"] = query.PurchaserName
                });

                var truncNote = truncated ? "（已截断）" : string.Empty;
                await _exportLog.AppendAsync(new ExportOperationLogRequest
                {
                    BizType = BusinessLogTypes.InventoryStockItem,
                    RecordId = ExportOperationAudit.ListRecordId,
                    RecordCode = ExportOperationAudit.InventoryStockItemListRecordCode,
                    ActionType = InventoryExportActionTypes.InventoryStockItemListExport,
                    ExportKind = ExportAuditKinds.InventoryStockItemList,
                    OperationDesc = $"导出库存明细列表 {items.Count} 条{truncNote}",
                    ExportedCount = items.Count,
                    Truncated = truncated,
                    Filters = filters,
                    FiltersMasked = mask511 || mask521,
                    OperatorUserId = query.CurrentUserId,
                    OperatorUserName = InventoryExportHttp.UserName(User)
                }, cancellationToken);

                return InventoryExportHttp.CsvFile(sb.ToString(), "库存明细列表.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出库存明细列表失败");
                return StatusCode(500, new { success = false, message = $"导出库存明细列表失败: {ex.Message}" });
            }
        }

        [HttpGet("materials/{materialId}/traces")]
        public async Task<ActionResult<ApiResponse<IEnumerable<InventoryMaterialTraceDto>>>> GetMaterialTrace(string materialId)
        {
            try
            {
                var list = await _service.GetMaterialTraceAsync(
                    materialId,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
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
        public async Task<ActionResult<ApiResponse<InventoryFinanceSummaryDto>>> GetFinanceSummary(
            [FromQuery] int stagnantDays = 90,
            [FromQuery] string? warehouseId = null,
            [FromQuery] string? materialModel = null,
            [FromQuery] string? stockCode = null,
            [FromQuery] short? stockType = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var dto = await _service.GetFinanceSummaryAsync(
                    stagnantDays,
                    warehouseId,
                    materialModel,
                    stockCode,
                    stockType,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    cancellationToken);
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

        [HttpPut("warehouses/batch")]
        public async Task<ActionResult<ApiResponse<IEnumerable<WarehouseInfo>>>> SaveWarehousesBatch(
            [FromBody] List<WarehouseInfo> request)
        {
            try
            {
                var list = await _service.SaveWarehousesBatchAsync(request ?? new List<WarehouseInfo>());
                return Ok(ApiResponse<IEnumerable<WarehouseInfo>>.Ok(list, "保存仓库成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<IEnumerable<WarehouseInfo>>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量保存仓库失败");
                return StatusCode(500, ApiResponse<IEnumerable<WarehouseInfo>>.Fail($"保存仓库失败: {ex.Message}", 500));
            }
        }

        /// <summary>拣货单列表（出库通知 + 仓库 + 订单展示列）。</summary>
        [HttpGet("picking-list")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<PickingTaskListItemDto>>>> GetPickingTaskList(
            [FromQuery] short? status = null,
            [FromQuery] string? warehouseId = null,
            [FromQuery] string? taskCode = null,
            [FromQuery] string? packingCode = null,
            [FromQuery] string? freightForwarderOrderNo = null,
            [FromQuery] string? stockOutRequestCode = null,
            [FromQuery] string? materialModel = null,
            [FromQuery] string? customerName = null,
            [FromQuery] string? salesUserName = null,
            [FromQuery] DateTime? createTimeFrom = null,
            [FromQuery] DateTime? createTimeTo = null)
        {
            try
            {
                var query = new PickingTaskListQueryRequest
                {
                    Status = status,
                    WarehouseId = warehouseId,
                    TaskCode = taskCode,
                    PackingCode = packingCode,
                    FreightForwarderOrderNo = freightForwarderOrderNo,
                    StockOutRequestCode = stockOutRequestCode,
                    MaterialModel = materialModel,
                    CustomerName = customerName,
                    SalesUserName = salesUserName,
                    CreateTimeFrom = createTimeFrom,
                    CreateTimeTo = createTimeTo,
                    CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                };
                var list = await _service.GetPickingTaskListRowsAsync(query);
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

        [HttpGet("pick-page")]
        public async Task<ActionResult<ApiResponse<PickPageByPackingDto>>> GetPickPageByPacking(
            [FromQuery] string packingId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(packingId))
                    return BadRequest(ApiResponse<PickPageByPackingDto>.Fail("packingId 不能为空", 400));
                var dto = await _service.GetPickPageByPackingAsync(packingId.Trim(), cancellationToken);
                return Ok(ApiResponse<PickPageByPackingDto>.Ok(dto, "获取装箱拣货页成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<PickPageByPackingDto>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<PickPageByPackingDto>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取装箱拣货页失败 packingId={PackingId}", packingId);
                return StatusCode(500, ApiResponse<PickPageByPackingDto>.Fail($"获取装箱拣货页失败: {ex.Message}", 500));
            }
        }

        [HttpPost("picking-tasks/generate-by-packing")]
        public async Task<ActionResult<ApiResponse<PickingTask>>> GeneratePickingTaskByPacking(
            [FromBody] GeneratePickingTaskByPackingRequest request)
        {
            try
            {
                var task = await _service.GeneratePickingTaskByPackingAsync(request);
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
                _logger.LogError(ex, "按装箱单生成拣货任务失败");
                var detail = ApiExceptionMessages.FormatWithDatabaseInner(ex);
                return StatusCode(500, ApiResponse<PickingTask>.Fail($"生成拣货任务失败: {detail}", 500));
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
            [FromQuery] string? stockOutRequestId,
            [FromQuery] string? packingItemId,
            [FromQuery] string warehouseId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(warehouseId))
                    return BadRequest(ApiResponse<IReadOnlyList<PickingStockItemCandidateDto>>.Fail("warehouseId 不能为空", 400));

                IReadOnlyList<PickingStockItemCandidateDto> list;
                if (!string.IsNullOrWhiteSpace(packingItemId))
                {
                    list = await _service.GetPickingCandidateStockItemsByPackingItemAsync(
                        packingItemId.Trim(),
                        warehouseId.Trim());
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(stockOutRequestId))
                        return BadRequest(ApiResponse<IReadOnlyList<PickingStockItemCandidateDto>>.Fail("stockOutRequestId 或 packingItemId 不能为空", 400));
                    list = await _service.GetPickingCandidateStockItemsAsync(stockOutRequestId.Trim(), warehouseId.Trim());
                }

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

        [HttpDelete("stock-items/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteStockItem(string id)
        {
            try
            {
                var item = await _stockItemRepo.GetByIdAsync(id);
                if (item == null)
                    return NotFound(ApiResponse<object>.Fail("库存明细不存在", 404));

                if (item.QtyRepertory > 0 || item.QtyRepertoryAvailable > 0)
                    return BadRequest(ApiResponse<object>.Fail("当前库存明细数量大于 0，不能普通删除", 400));

                var aggregateId = item.StockAggregateId?.Trim();
                var hasDownstreamStockOut = (await _stockOutItemRepo.FindAsync(x => x.StockItemId == item.Id)).Any();
                if (hasDownstreamStockOut)
                    return BadRequest(ApiResponse<object>.Fail("存在下游出库明细引用，不能普通删除库存明细", 400));
                var hasDownstreamPicking = (await _pickingTaskItemRepo.FindAsync(x => x.StockItemId == item.Id)).Any();
                if (hasDownstreamPicking)
                    return BadRequest(ApiResponse<object>.Fail("存在下游拣货明细引用，不能普通删除库存明细", 400));
                await _stockItemRepo.DeleteAsync(item.Id);
                await _service.RecalculateStockAggregateTotalsAsync(aggregateId);
                await _unitOfWork.SaveChangesAsync();
                return Ok(ApiResponse<object>.Ok(null, "删除库存明细成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除库存明细失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除库存明细失败: {ex.Message}", 500));
            }
        }

        [HttpPost("stock-items/{id}/force-delete")]
        public async Task<ActionResult<ApiResponse<object>>> ForceDeleteStockItem(string id, [FromBody] ForceDeleteInventoryRequest? body)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return StatusCode(403, ApiResponse<object>.Fail("未登录或身份无效", 403));

                var summary = await _rbacService.GetUserPermissionSummaryAsync(userId.Trim());
                if (!ManagementAccountPolicy.CanForceDelete(summary))
                    return StatusCode(403, ApiResponse<object>.Fail("仅系统管理员或平台管理员可执行强制删除", 403));

                if (body == null || string.IsNullOrWhiteSpace(body.ConfirmBillCode))
                    return BadRequest(ApiResponse<object>.Fail("请填写 confirmBillCode", 400));

                var userName = User.FindFirst(ClaimTypes.Name)?.Value;
                await _service.ForceDeleteStockItemAsync(
                    id,
                    body.ConfirmBillCode.Trim(),
                    userId.Trim(),
                    string.IsNullOrWhiteSpace(userName) ? null : userName.Trim());

                return Ok(ApiResponse<object>.Ok(null, "强制删除库存明细成功"));
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
                _logger.LogError(ex, "强制删除库存明细失败");
                return StatusCode(500, ApiResponse<object>.Fail($"强制删除库存明细失败: {ex.Message}", 500));
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

                await _service.DeletePickingSlipAsync(task.Id);
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
                if (!ManagementAccountPolicy.CanForceDelete(summary))
                    return StatusCode(403, ApiResponse<object>.Fail("仅系统管理员或平台管理员可执行强制删除", 403));

                if (body == null || string.IsNullOrWhiteSpace(body.ConfirmBillCode))
                    return BadRequest(ApiResponse<object>.Fail("请填写 confirmBillCode", 400));

                var userName = User.FindFirst(ClaimTypes.Name)?.Value;
                await _service.ForceDeletePickingSlipAsync(
                    id,
                    body.ConfirmBillCode.Trim(),
                    userId.Trim(),
                    string.IsNullOrWhiteSpace(userName) ? null : userName.Trim());

                return Ok(ApiResponse<object>.Ok(null, "强制删除拣货单成功"));
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
                _logger.LogError(ex, "强制删除拣货单失败");
                return StatusCode(500, ApiResponse<object>.Fail($"强制删除拣货单失败: {ex.Message}", 500));
            }
        }

        [HttpGet("count-plans")]
        public async Task<IActionResult> GetCountPlans(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _service.GetCountPlansPagedAsync(
                    page,
                    pageSize,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    cancellationToken);
                var items = result.Items.ToList();
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        items,
                        total = result.TotalCount,
                        page = result.PageIndex,
                        pageSize = result.PageSize
                    },
                    message = "获取盘点计划成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取盘点计划失败");
                return StatusCode(500, new { success = false, message = $"获取盘点计划失败: {ex.Message}" });
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

