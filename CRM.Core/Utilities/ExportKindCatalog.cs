using System.Text.Json;
using CRM.Core.Constants;

namespace CRM.Core.Utilities;

/// <summary>每个导出按钮的业务类型、所在页面标题与默认 URL。</summary>
public sealed record ExportKindCatalogEntry(
    string Kind,
    string BusinessTypeName,
    string PageTitle,
    string PageUrl);

/// <summary>从 ExtraInfo / 目录回填后的导出日志展示字段。</summary>
public sealed class ExportLogDisplay
{
    public string ExportKind { get; init; } = string.Empty;
    public string BusinessTypeName { get; init; } = string.Empty;
    public string PageTitle { get; init; } = string.Empty;
    public string PageUrl { get; init; } = string.Empty;
    public string? FilterSummary { get; init; }
    public int? ExportedCount { get; init; }
    public string? SysRemark { get; init; }
}

public static class ExportKindCatalog
{
    public static IReadOnlyList<ExportKindCatalogEntry> All { get; } =
    [
        new(ExportAuditKinds.StockInList, "入库单列表", "入库", "/inventory/stock-in"),
        new(ExportAuditKinds.StockOutList, "出库单列表", "出库单", "/inventory/stock-out"),
        new(ExportAuditKinds.InventoryStockList, "库存中心列表", "库存管理", "/inventory/list"),
        new(ExportAuditKinds.InventoryStockItemList, "库存明细列表", "库存明细", "/inventory/stock-items"),
        new(ExportAuditKinds.FinancePaymentList, "付款记录列表", "付款记录", "/finance/payments"),
        new(ExportAuditKinds.FinancePurchaseInvoiceList, "进项发票列表", "进项发票", "/finance/purchase-invoices"),
        new(ExportAuditKinds.FinanceReceivableList, "应收款列表", "应收款", "/finance/receivables"),
        new(ExportAuditKinds.FinanceCustomerAdvanceList, "预收款列表", "预收款", "/finance/customer-advances"),
        new(ExportAuditKinds.FinanceReceiptList, "收款记录列表", "收款记录", "/finance/receipts"),
        new(ExportAuditKinds.FinanceSellInvoiceList, "销项发票列表", "销项发票", "/finance/sell-invoices"),
        new(ExportAuditKinds.FinanceFfPayableList, "货代付款列表", "货代付款", "/finance/freight-forwarder-payables"),
        new(ExportAuditKinds.SalesOrderItemList, "销售订单明细列表", "销售订单明细", "/sales-order-items"),
        new(ExportAuditKinds.PurchaseOrderItemList, "采购订单明细列表", "采购订单明细", "/purchase-order-items"),
        new(ExportAuditKinds.StockingPurchaseItemList, "备货采购清单", "备货采购清单", "/stocking-purchase-items"),
        new(ExportAuditKinds.BatchReconciliationIn, "批次核销-入库批次", "批次核销", "/inventory/batch-reconciliation"),
        new(ExportAuditKinds.BatchReconciliationOut, "批次核销-出库批次", "批次核销", "/inventory/batch-reconciliation"),
        new(ExportAuditKinds.StockInBatch, "入库单批次", "入库", "/inventory/stock-in"),
        new(ExportAuditKinds.StockOutBatch, "装箱单出库批次", "装箱单", "/inventory/packing"),
        new(ExportAuditKinds.PurchaseOrderStockInBatch, "采购订单入库批次", "采购订单", "/purchase-orders"),
        new(ExportAuditKinds.SalesOrderStockOutBatch, "销售订单出库批次", "销售订单", "/sales-orders")
    ];

    private static readonly Dictionary<string, ExportKindCatalogEntry> ByKind =
        All.ToDictionary(x => x.Kind, StringComparer.OrdinalIgnoreCase);

    public static ExportKindCatalogEntry? Get(string? kind)
    {
        if (string.IsNullOrWhiteSpace(kind)) return null;
        return ByKind.TryGetValue(kind.Trim(), out var e) ? e : null;
    }

    public static string BusinessTypeName(string? kind) =>
        Get(kind)?.BusinessTypeName ?? (kind ?? string.Empty);

    /// <summary>仅接受站内相对路径，拒绝协议与空值。</summary>
    public static string? SanitizePageUrl(string? raw)
    {
        var s = (raw ?? string.Empty).Trim();
        if (s.Length == 0 || s.Length > 256) return null;
        if (!s.StartsWith('/')) return null;
        if (s.StartsWith("//", StringComparison.Ordinal)) return null;
        if (s.Contains("://", StringComparison.Ordinal)) return null;
        var q = s.IndexOf('?', StringComparison.Ordinal);
        if (q >= 0) s = s[..q];
        var hash = s.IndexOf('#', StringComparison.Ordinal);
        if (hash >= 0) s = s[..hash];
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    public static string BuildSysRemark(bool truncated, bool filtersMasked, int maxRows)
    {
        var parts = new List<string>(2);
        if (truncated) parts.Add($"已截断（上限 {maxRows}）");
        if (filtersMasked) parts.Add("条件已按权限脱敏");
        return string.Join("；", parts);
    }

    public static ExportLogDisplay Hydrate(string? extraInfoJson, string? fallbackKind = null)
    {
        string kind = fallbackKind ?? string.Empty;
        string? pageTitle = null;
        string? pageUrl = null;
        string? filterSummary = null;
        string? sysRemark = null;
        int? exportedCount = null;
        var truncated = false;
        var filtersMasked = false;
        var maxRows = ExportOperationAudit.DefaultMaxExportRows;

        if (!string.IsNullOrWhiteSpace(extraInfoJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(extraInfoJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("exportKind", out var k) && k.ValueKind == JsonValueKind.String)
                    kind = k.GetString() ?? kind;
                if (root.TryGetProperty("pageTitle", out var pt) && pt.ValueKind == JsonValueKind.String)
                    pageTitle = pt.GetString();
                if (root.TryGetProperty("pageUrl", out var pu) && pu.ValueKind == JsonValueKind.String)
                    pageUrl = pu.GetString();
                if (root.TryGetProperty("filterSummary", out var fs) && fs.ValueKind == JsonValueKind.String)
                    filterSummary = fs.GetString();
                if (root.TryGetProperty("sysRemark", out var sr) && sr.ValueKind == JsonValueKind.String)
                    sysRemark = sr.GetString();
                if (root.TryGetProperty("exportedCount", out var ec) && ec.TryGetInt32(out var n))
                    exportedCount = n;
                if (root.TryGetProperty("truncated", out var tr) && tr.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    truncated = tr.GetBoolean();
                if (root.TryGetProperty("filtersMasked", out var fm) && fm.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    filtersMasked = fm.GetBoolean();
                if (root.TryGetProperty("maxRows", out var mr) && mr.TryGetInt32(out var mx) && mx > 0)
                    maxRows = mx;
            }
            catch (JsonException)
            {
                // 旧脏数据：仅用目录回填
            }
        }

        var entry = Get(kind);
        if (string.IsNullOrWhiteSpace(sysRemark))
            sysRemark = BuildSysRemark(truncated, filtersMasked, maxRows);

        return new ExportLogDisplay
        {
            ExportKind = kind,
            BusinessTypeName = entry?.BusinessTypeName ?? kind,
            PageTitle = string.IsNullOrWhiteSpace(pageTitle) ? (entry?.PageTitle ?? string.Empty) : pageTitle.Trim(),
            PageUrl = SanitizePageUrl(pageUrl) ?? entry?.PageUrl ?? string.Empty,
            FilterSummary = filterSummary,
            ExportedCount = exportedCount,
            SysRemark = string.IsNullOrWhiteSpace(sysRemark) ? null : sysRemark
        };
    }
}
