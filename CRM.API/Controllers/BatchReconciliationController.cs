using System.Globalization;
using System.Text;
using CRM.API.Utilities;
using CRM.Core.Interfaces;
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
    private readonly ILogger<BatchReconciliationController> _logger;

    public BatchReconciliationController(
        IBatchReconciliationListQuery query,
        IRbacService rbacService,
        ILogger<BatchReconciliationController> logger)
    {
        _query = query;
        _rbacService = rbacService;
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
            var rows = await _query.GetConsumptionByGlobalBatchNoAsync(globalBatchNo, cancellationToken);
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

    [HttpGet("export/in-batches")]
    public async Task<IActionResult> ExportInBatches(
        [FromQuery] BatchReconciliationQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _query.ListForInBatchExportAsync(
                request,
                BatchReconciliationListQuery.MaxExportRows,
                cancellationToken);

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(',',
                "批次全局编号", "批次维度", "批次记录单位", "单位编号", "批次数量",
                "批次DC", "封装产地", "晶圆产地", "LOT", "SN", "固件版本号", "PARTCODE", "备注",
                "入库单号", "入库日期", "物料型号", "物料品牌", "仓库", "已出库合计", "剩余可出"));

            foreach (var r in rows)
            {
                sb.AppendLine(string.Join(',',
                    CsvCell(r.GlobalBatchNo),
                    CsvCell(r.BatchDimension),
                    CsvCell(r.BatchUnit),
                    CsvCell(r.UnitNo),
                    CsvCell(r.BatchQty.ToString(CultureInfo.InvariantCulture)),
                    CsvCell(r.Dc),
                    CsvCell(r.PackageOrigin),
                    CsvCell(r.WaferOrigin),
                    CsvCell(r.Lot),
                    CsvCell(r.SerialNumber),
                    CsvCell(r.FirmwareVersion),
                    CsvCell(r.PartCode),
                    CsvCell(r.BatchRemark),
                    CsvCell(r.StockInCode),
                    CsvCell(FormatDate(r.StockInDate)),
                    CsvCell(r.MaterialModel),
                    CsvCell(r.MaterialBrand),
                    CsvCell(r.WarehouseName),
                    CsvCell(r.TotalOutQty.ToString(CultureInfo.InvariantCulture)),
                    CsvCell(r.RemainingQty.ToString(CultureInfo.InvariantCulture))));
            }

            return CsvFile(sb.ToString(), "stock-in-batches.csv");
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
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _query.ListForOutBatchExportAsync(
                request,
                BatchReconciliationListQuery.MaxExportRows,
                cancellationToken);

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(',', "批次全局编号", "批次出库数量", "装箱单号", "出库日期", "物料型号", "LOT"));

            foreach (var r in rows)
            {
                sb.AppendLine(string.Join(',',
                    CsvCell(r.GlobalBatchNo),
                    CsvCell(r.OutQty.ToString(CultureInfo.InvariantCulture)),
                    CsvCell(r.PackingCode),
                    CsvCell(FormatDateNullable(r.StockOutDate)),
                    CsvCell(r.MaterialModel),
                    CsvCell(r.Lot)));
            }

            return CsvFile(sb.ToString(), "stock-out-batches.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出出库批次失败");
            return StatusCode(500, new { success = false, message = $"导出出库批次失败: {ex.Message}" });
        }
    }

    private static FileContentResult CsvFile(string content, string fileName)
    {
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(content)).ToArray();
        return new FileContentResult(bytes, "text/csv; charset=utf-8")
        {
            FileDownloadName = fileName
        };
    }

    private static string CsvCell(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    private static string FormatDate(DateTime dt) =>
        dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatDateNullable(DateTime? dt) =>
        dt.HasValue ? FormatDate(dt.Value) : string.Empty;
}
