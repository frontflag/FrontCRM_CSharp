using System.Globalization;
using System.Text;
using CRM.API.Utilities;
using CRM.Core.Interfaces;
using CRM.Core.Services;
using CRM.Core.Utilities;
using CRM.Infrastructure.BatchReconciliation;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/batch-reconciliation")]
public class BatchReconciliationController : ControllerBase
{
    private readonly IBatchReconciliationListQuery _query;
    private readonly IRbacService _rbacService;
    private readonly IExportOperationLogService _exportLog;
    private readonly IStockInService _stockInService;
    private readonly IPackingService _packingService;
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly ISalesOrderService _salesOrderService;
    private readonly ILogger<BatchReconciliationController> _logger;

    public BatchReconciliationController(
        IBatchReconciliationListQuery query,
        IRbacService rbacService,
        IExportOperationLogService exportLog,
        IStockInService stockInService,
        IPackingService packingService,
        IPurchaseOrderService purchaseOrderService,
        ISalesOrderService salesOrderService,
        ILogger<BatchReconciliationController> logger)
    {
        _query = query;
        _rbacService = rbacService;
        _exportLog = exportLog;
        _stockInService = stockInService;
        _packingService = packingService;
        _purchaseOrderService = purchaseOrderService;
        _salesOrderService = salesOrderService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] BatchReconciliationQueryRequest request,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            request ??= new BatchReconciliationQueryRequest();
            request.CurrentUserId = InventoryExportHttp.UserId(User);
            var result = await _query.GetPagedAsync(request, page, pageSize, cancellationToken);
            var mask511 = await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User);
            var mask521 = await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User);
            if (mask511)
                PurchaseSensitiveFieldMask511.ApplyBatchReconciliationRows(result.Items, true);
            if (mask521)
                SaleSensitiveFieldMask521.ApplyBatchReconciliationRows(result.Items, true);

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
                message = "获取批次核销列表成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取批次核销列表失败");
            return StatusCode(500, new { success = false, message = $"获取批次核销列表失败: {ex.Message}" });
        }
    }

    [HttpGet("consumption/{globalBatchNo}")]
    public async Task<IActionResult> GetConsumption(
        string globalBatchNo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = InventoryExportHttp.UserId(User);
            var rows = await _query.GetConsumptionByGlobalBatchNoAsync(globalBatchNo, userId, cancellationToken);
            if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                SaleSensitiveFieldMask521.ApplyBatchReconciliationConsumptionRows(rows, true);

            return Ok(new
            {
                success = true,
                data = rows,
                message = "获取出库消耗明细成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取出库消耗明细失败");
            return StatusCode(500, new { success = false, message = $"获取出库消耗明细失败: {ex.Message}" });
        }
    }

    /// <param name="exportSource">list|stockIn|packing|purchaseOrder|salesOrder；缺省时按筛选推断。</param>
    [HttpGet("export/in-batches")]
    public async Task<IActionResult> ExportInBatches(
        [FromQuery] BatchReconciliationQueryRequest request,
        [FromQuery] string? exportSource = null,
        [FromQuery] string? exportPageUrl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            request ??= new BatchReconciliationQueryRequest();
            request.CurrentUserId = InventoryExportHttp.UserId(User);
            var mask511 = await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User);
            var mask521 = await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User);
            if (mask511) request.VendorName = null;
            if (mask521) request.CustomerName = null;

            var maxRows = BatchReconciliationListQuery.MaxExportRows;
            var rows = await _query.ListForInBatchExportAsync(request, maxRows, cancellationToken);
            var truncated = rows.Count >= maxRows;

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(',',
                "批次全局编号", "批次维度", "批次记录单位", "单位编号", "批次数量",
                "批次DC", "封装产地", "晶圆产地", "LOT", "SN", "固件版本号", "PARTCODE", "备注",
                "入库单号", "入库日期", "物料型号", "物料品牌", "仓库", "已出库合计", "剩余可出"));

            foreach (var r in rows)
            {
                sb.AppendLine(string.Join(',',
                    InventoryExportHttp.CsvCell(r.GlobalBatchNo),
                    InventoryExportHttp.CsvCell(r.BatchDimension),
                    InventoryExportHttp.CsvCell(r.BatchUnit),
                    InventoryExportHttp.CsvCell(r.UnitNo),
                    InventoryExportHttp.CsvCell(r.BatchQty.ToString(CultureInfo.InvariantCulture)),
                    InventoryExportHttp.CsvCell(r.Dc),
                    InventoryExportHttp.CsvCell(r.PackageOrigin),
                    InventoryExportHttp.CsvCell(r.WaferOrigin),
                    InventoryExportHttp.CsvCell(r.Lot),
                    InventoryExportHttp.CsvCell(r.SerialNumber),
                    InventoryExportHttp.CsvCell(r.FirmwareVersion),
                    InventoryExportHttp.CsvCell(r.PartCode),
                    InventoryExportHttp.CsvCell(r.BatchRemark),
                    InventoryExportHttp.CsvCell(r.StockInCode),
                    InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDate(r.StockInDate)),
                    InventoryExportHttp.CsvCell(r.MaterialModel),
                    InventoryExportHttp.CsvCell(r.MaterialBrand),
                    InventoryExportHttp.CsvCell(r.WarehouseName),
                    InventoryExportHttp.CsvCell(r.TotalOutQty.ToString(CultureInfo.InvariantCulture)),
                    InventoryExportHttp.CsvCell(r.RemainingQty.ToString(CultureInfo.InvariantCulture))));
            }

            await InventoryExportHttp.AppendBatchExportLogAsync(
                _exportLog,
                _stockInService,
                _packingService,
                _purchaseOrderService,
                _salesOrderService,
                request,
                exportSource,
                isInBatches: true,
                exportedCount: rows.Count,
                truncated,
                filtersMasked: mask511 || mask521,
                User,
                cancellationToken,
                exportPageUrl);

            return InventoryExportHttp.CsvFile(sb.ToString(), "stock-in-batches.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出入库批次失败");
            return StatusCode(500, new { success = false, message = $"导出入库批次失败: {ex.Message}" });
        }
    }

    [HttpGet("export/out-batches")]
    public async Task<IActionResult> ExportOutBatches(
        [FromQuery] BatchReconciliationQueryRequest request,
        [FromQuery] string? exportSource = null,
        [FromQuery] string? exportPageUrl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            request ??= new BatchReconciliationQueryRequest();
            request.CurrentUserId = InventoryExportHttp.UserId(User);
            var mask511 = await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User);
            var mask521 = await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User);
            if (mask511) request.VendorName = null;
            if (mask521) request.CustomerName = null;

            var maxRows = BatchReconciliationListQuery.MaxExportRows;
            var rows = await _query.ListForOutBatchExportAsync(request, maxRows, cancellationToken);
            var truncated = rows.Count >= maxRows;

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(',', "批次全局编号", "批次出库数量", "装箱单号", "出库日期", "物料型号", "LOT"));

            foreach (var r in rows)
            {
                sb.AppendLine(string.Join(',',
                    InventoryExportHttp.CsvCell(r.GlobalBatchNo),
                    InventoryExportHttp.CsvCell(r.OutQty.ToString(CultureInfo.InvariantCulture)),
                    InventoryExportHttp.CsvCell(r.PackingCode),
                    InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDate(r.StockOutDate)),
                    InventoryExportHttp.CsvCell(r.MaterialModel),
                    InventoryExportHttp.CsvCell(r.Lot)));
            }

            await InventoryExportHttp.AppendBatchExportLogAsync(
                _exportLog,
                _stockInService,
                _packingService,
                _purchaseOrderService,
                _salesOrderService,
                request,
                exportSource,
                isInBatches: false,
                exportedCount: rows.Count,
                truncated,
                filtersMasked: mask511 || mask521,
                User,
                cancellationToken,
                exportPageUrl);

            return InventoryExportHttp.CsvFile(sb.ToString(), "stock-out-batches.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出出库批次失败");
            return StatusCode(500, new { success = false, message = $"导出出库批次失败: {ex.Message}" });
        }
    }
}
