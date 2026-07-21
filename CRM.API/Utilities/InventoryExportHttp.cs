using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Services;
using CRM.Core.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Utilities;

/// <summary>库存相关 CSV 导出公共方法。</summary>
public static class InventoryExportHttp
{
    public static string? UserId(ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public static string? UserName(ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Name)?.Value ?? user.FindFirst(ClaimTypes.Email)?.Value;

    public static FileContentResult CsvFile(string content, string fileName)
    {
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(content)).ToArray();
        return new FileContentResult(bytes, "text/csv; charset=utf-8")
        {
            FileDownloadName = WithExportTimestamp(fileName)
        };
    }

    /// <summary>导出文件名追加时间戳：name.csv → name_yyMMddHHmm.csv</summary>
    public static string WithExportTimestamp(string fileName, DateTime? at = null)
    {
        var raw = (fileName ?? string.Empty).Trim();
        if (raw.Length == 0) raw = "export.csv";
        var stamp = (at ?? DateTime.Now).ToString("yyMMddHHmm", CultureInfo.InvariantCulture);
        var dot = raw.LastIndexOf('.');
        if (dot <= 0) return $"{raw}_{stamp}";
        return $"{raw[..dot]}_{stamp}{raw[dot..]}";
    }

    public static string CsvCell(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    public static string FormatDate(DateTime? dt) =>
        dt.HasValue ? dt.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;

    public static string FormatDateTime(DateTime? dt) =>
        dt.HasValue ? dt.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) : string.Empty;

    public static string FormatDecimal(decimal? value) =>
        value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;

    public static string FormatDecimal(int value) =>
        value.ToString(CultureInfo.InvariantCulture);

    public static Dictionary<string, object?> BatchFilters(BatchReconciliationQueryRequest request) =>
        ExportOperationAudit.NormalizeFilters(new Dictionary<string, object?>
        {
            ["globalBatchNo"] = request.GlobalBatchNo,
            ["purchaseOrderId"] = request.PurchaseOrderId,
            ["purchaseOrderCode"] = request.PurchaseOrderCode,
            ["stockInCode"] = request.StockInCode,
            ["packingCode"] = request.PackingCode,
            ["packingId"] = request.PackingId,
            ["sellOrderId"] = request.SellOrderId,
            ["materialModel"] = request.MaterialModel,
            ["lot"] = request.Lot,
            ["serialNumber"] = request.SerialNumber,
            ["vendorName"] = request.VendorName,
            ["customerName"] = request.CustomerName,
            ["remark"] = request.Remark
        });

    public static async Task AppendBatchExportLogAsync(
        IExportOperationLogService exportLog,
        IStockInService stockInService,
        IPackingService packingService,
        IPurchaseOrderService purchaseOrderService,
        ISalesOrderService salesOrderService,
        BatchReconciliationQueryRequest request,
        string? exportSource,
        bool isInBatches,
        int exportedCount,
        bool truncated,
        bool filtersMasked,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var source = (exportSource ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(source))
            source = InferBatchExportSource(request);

        var filters = BatchFilters(request);
        var userId = UserId(user);
        var userName = UserName(user);
        var maxRows = ExportOperationAudit.DefaultMaxExportRows;
        var truncNote = truncated ? "（已截断）" : string.Empty;

        if (source == BatchExportSources.PurchaseOrder && !string.IsNullOrWhiteSpace(request.PurchaseOrderId))
        {
            var order = await purchaseOrderService.GetByIdAsync(request.PurchaseOrderId.Trim());
            var code = order?.PurchaseOrderCode?.Trim() ?? request.PurchaseOrderCode?.Trim();
            var id = order?.Id ?? request.PurchaseOrderId.Trim();
            await exportLog.AppendAsync(new ExportOperationLogRequest
            {
                BizType = BusinessLogTypes.PurchaseOrder,
                RecordId = id,
                RecordCode = code,
                ActionType = PurchaseOrderBatchExportActionTypes.Export,
                ExportKind = ExportAuditKinds.PurchaseOrderStockInBatch,
                OperationDesc = string.IsNullOrEmpty(code)
                    ? $"导出采购订单入库批次 {exportedCount} 条{truncNote}"
                    : $"导出采购订单 {code} 入库批次 {exportedCount} 条{truncNote}",
                ExportedCount = exportedCount,
                MaxRows = maxRows,
                Truncated = truncated,
                Filters = filters,
                FiltersMasked = filtersMasked,
                OperatorUserId = userId,
                OperatorUserName = userName
            }, cancellationToken);
            return;
        }

        if (source == BatchExportSources.SalesOrder && !string.IsNullOrWhiteSpace(request.SellOrderId))
        {
            var order = await salesOrderService.GetByIdAsync(request.SellOrderId.Trim());
            var code = order?.SellOrderCode?.Trim();
            var id = order?.Id ?? request.SellOrderId.Trim();
            await exportLog.AppendAsync(new ExportOperationLogRequest
            {
                BizType = BusinessLogTypes.SalesOrder,
                RecordId = id,
                RecordCode = code,
                ActionType = SalesOrderBatchExportActionTypes.Export,
                ExportKind = ExportAuditKinds.SalesOrderStockOutBatch,
                OperationDesc = string.IsNullOrEmpty(code)
                    ? $"导出销售订单出库批次 {exportedCount} 条{truncNote}"
                    : $"导出销售订单 {code} 出库批次 {exportedCount} 条{truncNote}",
                ExportedCount = exportedCount,
                MaxRows = maxRows,
                Truncated = truncated,
                Filters = filters,
                FiltersMasked = filtersMasked,
                OperatorUserId = userId,
                OperatorUserName = userName
            }, cancellationToken);
            return;
        }

        if (source == BatchExportSources.Packing && !string.IsNullOrWhiteSpace(request.PackingId))
        {
            var packing = await packingService.GetPackingByIdAsync(request.PackingId.Trim(), cancellationToken);
            if (packing != null)
            {
                var code = packing.Code?.Trim();
                await exportLog.AppendAsync(new ExportOperationLogRequest
                {
                    BizType = BusinessLogTypes.Packing,
                    RecordId = packing.Id,
                    RecordCode = code,
                    ActionType = StockOutBatchOperationActionTypes.Export,
                    ExportKind = ExportAuditKinds.StockOutBatch,
                    OperationDesc = string.IsNullOrEmpty(code)
                        ? $"导出出库批次 {exportedCount} 条{truncNote}"
                        : $"导出装箱单 {code} 出库批次 {exportedCount} 条{truncNote}",
                    ExportedCount = exportedCount,
                    MaxRows = maxRows,
                    Truncated = truncated,
                    Filters = filters,
                    FiltersMasked = filtersMasked,
                    OperatorUserId = userId,
                    OperatorUserName = userName
                }, cancellationToken);
                return;
            }
        }

        if (source == BatchExportSources.StockIn && !string.IsNullOrWhiteSpace(request.StockInCode))
        {
            var code = request.StockInCode.Trim();
            var list = await stockInService.GetListPagedAsync(
                new StockInQueryRequest { StockInCode = code, CurrentUserId = userId },
                1,
                5,
                cancellationToken);
            var hit = list.Items.FirstOrDefault(x =>
                string.Equals(x.StockInCode, code, StringComparison.OrdinalIgnoreCase));
            if (hit != null)
            {
                await exportLog.AppendAsync(new ExportOperationLogRequest
                {
                    BizType = BusinessLogTypes.StockIn,
                    RecordId = hit.Id,
                    RecordCode = hit.StockInCode,
                    ActionType = StockInBatchOperationActionTypes.Export,
                    ExportKind = ExportAuditKinds.StockInBatch,
                    OperationDesc = $"导出入库批次 {exportedCount} 条{truncNote}",
                    ExportedCount = exportedCount,
                    MaxRows = maxRows,
                    Truncated = truncated,
                    Filters = filters,
                    FiltersMasked = filtersMasked,
                    OperatorUserId = userId,
                    OperatorUserName = userName
                }, cancellationToken);
                return;
            }
        }

        var kind = isInBatches ? ExportAuditKinds.BatchReconciliationIn : ExportAuditKinds.BatchReconciliationOut;
        var action = isInBatches
            ? InventoryExportActionTypes.BatchReconciliationInExport
            : InventoryExportActionTypes.BatchReconciliationOutExport;
        var recordCode = isInBatches
            ? ExportOperationAudit.BatchReconInRecordCode
            : ExportOperationAudit.BatchReconOutRecordCode;
        var desc = isInBatches
            ? $"批次核销导出入库批次 {exportedCount} 条{truncNote}"
            : $"批次核销导出出库批次 {exportedCount} 条{truncNote}";

        await exportLog.AppendAsync(new ExportOperationLogRequest
        {
            BizType = BusinessLogTypes.BatchReconciliation,
            RecordId = ExportOperationAudit.ListRecordId,
            RecordCode = recordCode,
            ActionType = action,
            ExportKind = kind,
            OperationDesc = desc,
            ExportedCount = exportedCount,
            MaxRows = maxRows,
            Truncated = truncated,
            Filters = filters,
            FiltersMasked = filtersMasked,
            OperatorUserId = userId,
            OperatorUserName = userName
        }, cancellationToken);
    }

    private static string InferBatchExportSource(BatchReconciliationQueryRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.PurchaseOrderId)) return BatchExportSources.PurchaseOrder;
        if (!string.IsNullOrWhiteSpace(request.SellOrderId)) return BatchExportSources.SalesOrder;
        if (!string.IsNullOrWhiteSpace(request.PackingId)) return BatchExportSources.Packing;
        if (!string.IsNullOrWhiteSpace(request.StockInCode)
            && string.IsNullOrWhiteSpace(request.GlobalBatchNo)
            && string.IsNullOrWhiteSpace(request.PurchaseOrderCode)
            && string.IsNullOrWhiteSpace(request.PackingCode)
            && string.IsNullOrWhiteSpace(request.MaterialModel)
            && string.IsNullOrWhiteSpace(request.Lot)
            && string.IsNullOrWhiteSpace(request.SerialNumber)
            && string.IsNullOrWhiteSpace(request.VendorName)
            && string.IsNullOrWhiteSpace(request.CustomerName)
            && string.IsNullOrWhiteSpace(request.Remark))
            return BatchExportSources.StockIn;
        return BatchExportSources.List;
    }

    public static async Task<(List<T> Items, bool Truncated, int TotalCount)> CollectForExportAsync<T>(
        Func<int, int, CancellationToken, Task<PagedResult<T>>> fetchPage,
        int maxRows = ExportOperationAudit.DefaultMaxExportRows,
        int pageSize = 2000,
        CancellationToken cancellationToken = default)
    {
        var items = new List<T>();
        var page = 1;
        var total = 0;
        while (items.Count < maxRows)
        {
            var take = Math.Min(pageSize, maxRows - items.Count);
            var pageResult = await fetchPage(page, take, cancellationToken);
            total = pageResult.TotalCount;
            var pageItems = pageResult.Items?.ToList() ?? new List<T>();
            if (pageItems.Count == 0) break;
            items.AddRange(pageItems);
            if (pageItems.Count < take) break;
            page++;
        }

        return (items, total > items.Count, total);
    }
}
