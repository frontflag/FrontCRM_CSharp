using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CRM.Core.Constants;

namespace CRM.Core.Utilities;

/// <summary>导出条件摘要的展示上下文（仓库名等需运行时解析的字段）。</summary>
public sealed class ExportFilterDisplayContext
{
    public IReadOnlyDictionary<string, string>? WarehouseNamesById { get; init; }
}

/// <summary>导出审计 ExtraInfo 拼装与条件摘要（供 log_operation 使用）。</summary>
public static class ExportOperationAudit
{
    public const int DefaultMaxExportRows = 50000;
    public const int FilterSummaryMaxLength = 200;

    public const string ListRecordId = "list";
    public const string StockInListRecordCode = "STOCK_IN_LIST";
    public const string StockOutListRecordCode = "STOCK_OUT_LIST";
    public const string InventoryStockListRecordCode = "INVENTORY_STOCK_LIST";
    public const string InventoryStockItemListRecordCode = "INVENTORY_STOCK_ITEM_LIST";
    public const string BatchReconInRecordCode = "BATCH_RECON_IN";
    public const string BatchReconOutRecordCode = "BATCH_RECON_OUT";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string BuildExtraInfoJson(
        string exportKind,
        int exportedCount,
        IReadOnlyDictionary<string, object?> filters,
        bool filtersMasked,
        int maxRows = DefaultMaxExportRows,
        bool truncated = false,
        string? filterSummary = null,
        ExportFilterDisplayContext? display = null)
    {
        var summary = filterSummary ?? BuildFilterSummary(exportKind, filters, display);
        var payload = new Dictionary<string, object?>
        {
            ["schemaVersion"] = 1,
            ["exportKind"] = exportKind,
            ["exportedCount"] = exportedCount,
            ["affectedCount"] = exportedCount,
            ["maxRows"] = maxRows,
            ["truncated"] = truncated,
            ["filters"] = filters,
            ["filtersMasked"] = filtersMasked,
            ["filterSummary"] = summary
        };
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public static string BuildFilterSummary(
        string exportKind,
        IReadOnlyDictionary<string, object?> filters,
        ExportFilterDisplayContext? display = null)
    {
        if (filters.Count == 0 || filters.All(kv => IsEmptyFilterValue(kv.Value)))
            return "全部（无筛选）";

        var labels = GetLabels(exportKind);
        var parts = new List<string>();
        foreach (var (key, value) in filters)
        {
            if (IsEmptyFilterValue(value)) continue;
            var label = labels.TryGetValue(key, out var l) ? l : key;
            parts.Add($"{label}={FormatFilterDisplayValue(exportKind, key, value, display)}");
        }

        if (parts.Count == 0) return "全部（无筛选）";

        var text = string.Join("；", parts);
        if (text.Length <= FilterSummaryMaxLength) return text;
        return text[..(FilterSummaryMaxLength - 1)] + "…";
    }

    public static Dictionary<string, object?> NormalizeFilters(IEnumerable<KeyValuePair<string, object?>> raw)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in raw)
        {
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (key.Equals("page", StringComparison.OrdinalIgnoreCase)) continue;
            if (key.Equals("pageSize", StringComparison.OrdinalIgnoreCase)) continue;
            if (key.Equals("currentUserId", StringComparison.OrdinalIgnoreCase)) continue;
            if (key.Equals("exportSource", StringComparison.OrdinalIgnoreCase)) continue;
            dict[ToCamel(key)] = NormalizeValue(value);
        }
        return dict;
    }

    public static string? TryParseFilterSummary(string? extraInfo)
    {
        if (string.IsNullOrWhiteSpace(extraInfo)) return null;
        try
        {
            using var doc = JsonDocument.Parse(extraInfo);
            if (doc.RootElement.TryGetProperty("filterSummary", out var s) && s.ValueKind == JsonValueKind.String)
                return s.GetString();
        }
        catch
        {
            /* ignore */
        }
        return null;
    }

    public static int TryParseExportedCount(string? extraInfo, int fallback = 0)
    {
        if (string.IsNullOrWhiteSpace(extraInfo)) return fallback;
        try
        {
            using var doc = JsonDocument.Parse(extraInfo);
            if (doc.RootElement.TryGetProperty("exportedCount", out var c) && c.TryGetInt32(out var n))
                return n;
            if (doc.RootElement.TryGetProperty("affectedCount", out var a) && a.TryGetInt32(out var m))
                return m;
        }
        catch
        {
            /* ignore */
        }
        return fallback;
    }

    private static bool IsEmptyFilterValue(object? value)
    {
        if (value is null) return true;
        if (value is string s) return string.IsNullOrWhiteSpace(s);
        if (value is JsonElement je)
        {
            return je.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
                   || (je.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(je.GetString()));
        }
        return false;
    }

    private static object? NormalizeValue(object? value)
    {
        if (value is null) return null;
        if (value is string s)
        {
            var t = s.Trim();
            return t.Length == 0 ? null : t;
        }
        if (value is DateTime dt)
            return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (value is DateTimeOffset dto)
            return dto.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return value;
    }

    private static string FormatFilterValue(object? value)
    {
        if (value is null) return string.Empty;
        if (value is DateTime dt) return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (value is bool b) return b ? "true" : "false";
        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    /// <summary>摘要展示值：仓库 Id→名称，枚举/状态码→业务文案；原始 filters JSON 仍保留码值。</summary>
    private static string FormatFilterDisplayValue(
        string exportKind,
        string key,
        object? value,
        ExportFilterDisplayContext? display)
    {
        var text = FormatFilterValue(value);
        if (string.IsNullOrEmpty(text)) return text;

        if (key.Equals("warehouseId", StringComparison.OrdinalIgnoreCase))
        {
            if (display?.WarehouseNamesById != null
                && display.WarehouseNamesById.TryGetValue(text, out var name)
                && !string.IsNullOrWhiteSpace(name))
                return name.Trim();
            return text;
        }

        if (key.Equals("stockInType", StringComparison.OrdinalIgnoreCase))
            return MapStockInTypeLabel(text);
        if (key.Equals("stockOutType", StringComparison.OrdinalIgnoreCase))
            return MapStockOutTypeLabel(text);
        if (key.Equals("stockType", StringComparison.OrdinalIgnoreCase))
            return MapInventoryStockTypeLabel(text);
        if (key.Equals("outboundStatus", StringComparison.OrdinalIgnoreCase))
            return MapOutboundStatusLabel(text);
        if (key.Equals("repertoryHasStock", StringComparison.OrdinalIgnoreCase))
            return MapRepertoryHasStockLabel(text);
        if (key.Equals("status", StringComparison.OrdinalIgnoreCase)
            && string.Equals(exportKind, ExportAuditKinds.StockOutList, StringComparison.OrdinalIgnoreCase))
            return MapStockOutStatusLabel(text);
        if (key.Equals("shipmentMethod", StringComparison.OrdinalIgnoreCase))
            return MapShipmentMethodLabel(text);

        return text;
    }

    private static string MapStockInTypeLabel(string raw)
    {
        if (!TryParseShort(raw, out var code)) return raw;
        // 兼容迁移前库内 1/2/4
        code = code switch { 1 => StockInTypeCode.Purchase, 2 => StockInTypeCode.Return, 4 => StockInTypeCode.Scrap, _ => code };
        return code switch
        {
            StockInTypeCode.Purchase => "采购入库",
            StockInTypeCode.Customs => "报关入库",
            StockInTypeCode.Return => "退货入库",
            StockInTypeCode.Scrap => "报废入库",
            StockInTypeCode.Transfer => "调拨入库",
            _ => raw
        };
    }

    private static string MapStockOutTypeLabel(string raw)
    {
        if (!TryParseShort(raw, out var code)) return raw;
        return code switch
        {
            StockOutTypeCode.Sales or StockOutTypeCode.LegacySales => "销售出库",
            StockOutTypeCode.Customs => "报关出库",
            StockOutTypeCode.Return => "退货出库",
            StockOutTypeCode.Scrap => "报废出库",
            StockOutTypeCode.Transfer => "调拨出库",
            _ => raw
        };
    }

    private static string MapInventoryStockTypeLabel(string raw)
    {
        if (!TryParseShort(raw, out var code)) return raw;
        return code switch
        {
            1 => "客单库存",
            2 => "备货库存",
            3 => "样品库存",
            _ => raw
        };
    }

    private static string MapOutboundStatusLabel(string raw)
    {
        if (!TryParseShort(raw, out var code)) return raw;
        return code switch
        {
            1 => "未出库",
            2 => "部分出库",
            3 => "出库完成",
            _ => raw
        };
    }

    private static string MapRepertoryHasStockLabel(string raw)
    {
        if (bool.TryParse(raw, out var b))
            return b ? "有库存" : "无库存";
        var t = raw.Trim();
        if (t.Equals("1", StringComparison.Ordinal) || t.Equals("true", StringComparison.OrdinalIgnoreCase))
            return "有库存";
        if (t.Equals("0", StringComparison.Ordinal) || t.Equals("false", StringComparison.OrdinalIgnoreCase))
            return "无库存";
        return raw;
    }

    private static string MapStockOutStatusLabel(string raw)
    {
        if (!TryParseShort(raw, out var code)) return raw;
        return code switch
        {
            0 => "草稿",
            1 => "待出库",
            2 => "准备出库",
            3 => "已取消",
            4 => "出库完成",
            _ => raw
        };
    }

    private static string MapShipmentMethodLabel(string raw)
    {
        var code = raw.Trim();
        return code switch
        {
            LogisticsShipmentMethodCode.Delivery => "送货",
            LogisticsShipmentMethodCode.SelfPickup => "自提",
            LogisticsShipmentMethodCode.Express => "快递",
            _ => raw
        };
    }

    private static bool TryParseShort(string raw, out short code)
    {
        if (short.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out code))
            return true;
        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
            && i is >= short.MinValue and <= short.MaxValue)
        {
            code = (short)i;
            return true;
        }
        code = 0;
        return false;
    }

    private static string ToCamel(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        if (key.Length == 1) return key.ToLowerInvariant();
        if (char.IsLower(key[0])) return key;
        return char.ToLowerInvariant(key[0]) + key[1..];
    }

    private static IReadOnlyDictionary<string, string> GetLabels(string exportKind)
    {
        return exportKind switch
        {
            ExportAuditKinds.StockInList => StockInListLabels,
            ExportAuditKinds.StockOutList => StockOutListLabels,
            ExportAuditKinds.InventoryStockList => InventoryLabels,
            ExportAuditKinds.InventoryStockItemList => InventoryStockItemListLabels,
            ExportAuditKinds.BatchReconciliationIn or ExportAuditKinds.BatchReconciliationOut
                or ExportAuditKinds.StockInBatch or ExportAuditKinds.StockOutBatch
                or ExportAuditKinds.PurchaseOrderStockInBatch or ExportAuditKinds.SalesOrderStockOutBatch
                => BatchLabels,
            _ => BatchLabels
        };
    }

    private static readonly Dictionary<string, string> StockInListLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["model"] = "物料型号",
        ["vendorName"] = "供应商",
        ["purchaseOrderCode"] = "采购订单号",
        ["freightForwarderOrderNo"] = "货代单号",
        ["salesOrderCode"] = "销售订单号",
        ["stockInCode"] = "入库单号",
        ["sourceDisplayNo"] = "来源单号",
        ["warehouseId"] = "仓库",
        ["stockInDateStart"] = "入库日期起",
        ["stockInDateEnd"] = "入库日期止",
        ["remark"] = "备注",
        ["stockInType"] = "入库类型"
    };

    private static readonly Dictionary<string, string> StockOutListLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["keyword"] = "关键字",
        ["sourceCode"] = "来源单号",
        ["status"] = "状态",
        ["stockOutCode"] = "出库单号",
        ["packingCode"] = "装箱单号",
        ["shipmentMethod"] = "出货方式",
        ["customerName"] = "客户",
        ["salesUserName"] = "业务员",
        ["remark"] = "备注",
        ["stockOutType"] = "出库类型",
        ["stockOutDateFrom"] = "出库日期起",
        ["stockOutDateTo"] = "出库日期止",
        ["freightForwarderOrderNo"] = "货代单号"
    };

    private static readonly Dictionary<string, string> InventoryLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["warehouseId"] = "仓库",
        ["materialModel"] = "物料型号",
        ["stockCode"] = "库存编码",
        ["stockType"] = "库存类型"
    };

    private static readonly Dictionary<string, string> InventoryStockItemListLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["stockInCode"] = "入库单号",
        ["stockItemCode"] = "库存明细编号",
        ["freightForwarderOrderNo"] = "货代单号",
        ["stockInDateFrom"] = "入库日期起",
        ["stockInDateTo"] = "入库日期止",
        ["warehouseId"] = "仓库",
        ["purchasePn"] = "物料型号",
        ["purchaseBrand"] = "品牌",
        ["outboundStatus"] = "出库状态",
        ["repertoryHasStock"] = "是否有库存",
        ["customerName"] = "客户",
        ["vendorName"] = "供应商",
        ["salespersonName"] = "业务员",
        ["purchaserName"] = "采购员",
        ["salespersonUserId"] = "业务员",
        ["purchaserUserId"] = "采购员"
    };

    private static readonly Dictionary<string, string> BatchLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["globalBatchNo"] = "批次全局编号",
        ["purchaseOrderId"] = "采购订单Id",
        ["purchaseOrderCode"] = "采购订单号",
        ["stockInCode"] = "入库单号",
        ["packingCode"] = "装箱单号",
        ["packingId"] = "装箱单Id",
        ["sellOrderId"] = "销售订单Id",
        ["materialModel"] = "物料型号",
        ["lot"] = "LOT",
        ["serialNumber"] = "SN",
        ["vendorName"] = "供应商",
        ["customerName"] = "客户",
        ["remark"] = "备注"
    };
}
