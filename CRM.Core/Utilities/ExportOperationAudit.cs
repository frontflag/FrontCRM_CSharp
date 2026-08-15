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
    public const string FinancePaymentListRecordCode = "FINANCE_PAYMENT_LIST";
    public const string FinancePurchaseInvoiceListRecordCode = "FINANCE_PURCHASE_INVOICE_LIST";
    public const string FinanceReceivableListRecordCode = "FINANCE_RECEIVABLE_LIST";
    public const string FinanceCustomerAdvanceListRecordCode = "FINANCE_CUSTOMER_ADVANCE_LIST";
    public const string FinanceReceiptListRecordCode = "FINANCE_RECEIPT_LIST";
    public const string FinanceSellInvoiceListRecordCode = "FINANCE_SELL_INVOICE_LIST";
    public const string FinanceFfPayableListRecordCode = "FINANCE_FF_PAYABLE_LIST";
    public const string SalesOrderItemListRecordCode = "SALES_ORDER_ITEM_LIST";
    public const string PurchaseOrderItemListRecordCode = "PURCHASE_ORDER_ITEM_LIST";

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
        ExportFilterDisplayContext? display = null,
        string? pageTitle = null,
        string? pageUrl = null,
        string? sysRemark = null)
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
            ["filterSummary"] = summary,
            ["pageTitle"] = pageTitle,
            ["pageUrl"] = pageUrl,
            ["sysRemark"] = sysRemark
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

        var financeLabel = MapFinanceFilterDisplayValue(exportKind, key, text);
        if (financeLabel != null) return financeLabel;

        var orderItemLabel = MapOrderItemFilterDisplayValue(exportKind, key, text);
        if (orderItemLabel != null) return orderItemLabel;

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
            ExportAuditKinds.FinancePaymentList => FinancePaymentListLabels,
            ExportAuditKinds.FinancePurchaseInvoiceList => FinancePurchaseInvoiceListLabels,
            ExportAuditKinds.FinanceReceivableList => FinanceReceivableListLabels,
            ExportAuditKinds.FinanceCustomerAdvanceList => FinanceCustomerAdvanceListLabels,
            ExportAuditKinds.FinanceReceiptList => FinanceReceiptListLabels,
            ExportAuditKinds.FinanceSellInvoiceList => FinanceSellInvoiceListLabels,
            ExportAuditKinds.FinanceFfPayableList => FinanceFfPayableListLabels,
            ExportAuditKinds.SalesOrderItemList => SalesOrderItemListLabels,
            ExportAuditKinds.PurchaseOrderItemList => PurchaseOrderItemListLabels,
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

    private static readonly Dictionary<string, string> FinancePaymentListLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["keyword"] = "关键字",
        ["financePaymentCode"] = "付款单号",
        ["freightForwarderOrderNo"] = "货代单号",
        ["bankSlipNo"] = "银行水单号",
        ["paymentMode"] = "付款方式",
        ["vendorName"] = "供应商",
        ["remark"] = "备注",
        ["status"] = "状态",
        ["startDate"] = "付款日期起",
        ["endDate"] = "付款日期止"
    };

    private static readonly Dictionary<string, string> FinancePurchaseInvoiceListLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["keyword"] = "关键字",
        ["invoiceStatus"] = "开票状态",
        ["confirmStatus"] = "认证状态",
        ["verificationStatus"] = "核销状态",
        ["paymentStatus"] = "付款状态",
        ["startDate"] = "开票日期起",
        ["endDate"] = "开票日期止"
    };

    private static readonly Dictionary<string, string> FinanceReceivableListLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["keyword"] = "关键字",
        ["customerId"] = "客户",
        ["verificationStatus"] = "收款核销状态",
        ["onlyOpen"] = "收款核销状态",
        ["invoiceMatchStatus"] = "发票核销状态",
        ["invoiceMatchOnlyOpen"] = "发票核销状态",
        ["stockOutDateFrom"] = "出库日期起",
        ["stockOutDateTo"] = "出库日期止"
    };

    private static readonly Dictionary<string, string> FinanceCustomerAdvanceListLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["keyword"] = "关键字",
        ["customerId"] = "客户",
        ["currency"] = "币别",
        ["onlyPositiveBalance"] = "仅有余额"
    };

    private static readonly Dictionary<string, string> FinanceReceiptListLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["keyword"] = "关键字",
        ["status"] = "状态",
        ["receiptPurpose"] = "收款用途",
        ["verificationStatus"] = "核销状态",
        ["startDate"] = "收款日期起",
        ["endDate"] = "收款日期止"
    };

    private static readonly Dictionary<string, string> FinanceSellInvoiceListLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["keyword"] = "关键字",
        ["invoiceStatus"] = "开票状态",
        ["receiveStatus"] = "收款状态",
        ["matchStatus"] = "核销状态",
        ["startDate"] = "开票日期起",
        ["endDate"] = "开票日期止"
    };

    private static readonly Dictionary<string, string> FinanceFfPayableListLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["keyword"] = "关键字",
        ["customerId"] = "客户",
        ["freightForwarderCompanyId"] = "货代公司",
        ["payableStatus"] = "台账状态"
    };

    private static readonly Dictionary<string, string> SalesOrderItemListLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["orderCreateStart"] = "订单生成起",
        ["orderCreateEnd"] = "订单生成止",
        ["sellOrderCode"] = "销售订单/明细号",
        ["sellOrderItemCode"] = "销售订单明细号",
        ["customerName"] = "客户",
        ["customerId"] = "客户",
        ["salesUserName"] = "销售员",
        ["salesUserId"] = "销售员",
        ["purchaseUserAccount"] = "采购员",
        ["pn"] = "物料型号",
        ["purchaseOrderItemCode"] = "采购订单明细单号",
        ["customerSo"] = "客户订单号",
        ["customerPn"] = "客户型号",
        ["transactionCurrency"] = "交易币别",
        ["quickFilter"] = "快捷检索",
        ["purchaseProgressStatus"] = "采购状态",
        ["stockInProgressStatus"] = "入库状态",
        ["stockOutNotifyProgressStatus"] = "出库通知状态",
        ["stockOutProgressStatus"] = "出库状态",
        ["receiptProgressStatus"] = "收款状态",
        ["invoiceProgressStatus"] = "开票状态",
        ["stockOutPending"] = "仅待出库",
        ["receiptPending"] = "仅待收款",
        ["invoicePending"] = "仅待开票"
    };

    private static readonly Dictionary<string, string> PurchaseOrderItemListLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["startDate"] = "订单生成起",
        ["endDate"] = "订单生成止",
        ["purchaseOrderCode"] = "采购订单/明细号",
        ["freightForwarderOrderNo"] = "货代单号",
        ["vendorName"] = "供应商",
        ["purchaseUserName"] = "采购员",
        ["pn"] = "物料型号",
        ["sellOrderItemCode"] = "销售订单明细号",
        ["orderType"] = "订单类型",
        ["transactionCurrency"] = "交易币别",
        ["quickFilter"] = "快捷检索",
        ["paymentProgressStatus"] = "付款状态",
        ["purchaseProgressStatus"] = "采购状态",
        ["stockInProgressStatus"] = "入库状态",
        ["invoiceProgressStatus"] = "开票状态"
    };

    private static string? MapFinanceFilterDisplayValue(string exportKind, string key, string text)
    {
        if (string.Equals(exportKind, ExportAuditKinds.FinanceReceivableList, StringComparison.OrdinalIgnoreCase)
            && (key.Equals("onlyOpen", StringComparison.OrdinalIgnoreCase)
                || key.Equals("invoiceMatchOnlyOpen", StringComparison.OrdinalIgnoreCase)))
        {
            var yn = MapYesNo(text);
            return yn == "是" ? "待核销" : yn;
        }

        if (key.Equals("onlyOpen", StringComparison.OrdinalIgnoreCase)
            || key.Equals("onlyPositiveBalance", StringComparison.OrdinalIgnoreCase))
            return MapYesNo(text);

        if (key.Equals("currency", StringComparison.OrdinalIgnoreCase)
            || key.Equals("receiptCurrency", StringComparison.OrdinalIgnoreCase)
            || key.Equals("paymentCurrency", StringComparison.OrdinalIgnoreCase))
            return MapCurrencyIso(text);

        if (key.Equals("paymentMode", StringComparison.OrdinalIgnoreCase)
            || key.Equals("receiptMode", StringComparison.OrdinalIgnoreCase))
            return MapPaymentModeLabel(text);

        if (key.Equals("verificationStatus", StringComparison.OrdinalIgnoreCase)
            || key.Equals("matchStatus", StringComparison.OrdinalIgnoreCase)
            || key.Equals("invoiceMatchStatus", StringComparison.OrdinalIgnoreCase))
            return MapVerificationStatusLabel(text);

        if (key.Equals("paymentStatus", StringComparison.OrdinalIgnoreCase))
            return MapPaymentDoneStatusLabel(text);

        if (key.Equals("receiveStatus", StringComparison.OrdinalIgnoreCase))
            return MapReceiveStatusLabel(text);

        if (key.Equals("receiptPurpose", StringComparison.OrdinalIgnoreCase))
            return MapReceiptPurposeLabel(text);

        if (key.Equals("payableStatus", StringComparison.OrdinalIgnoreCase))
            return MapFfPayableStatusLabel(text);

        if (key.Equals("invoiceStatus", StringComparison.OrdinalIgnoreCase))
            return MapInvoiceStatusLabel(text);

        if (key.Equals("status", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(exportKind, ExportAuditKinds.FinancePaymentList, StringComparison.OrdinalIgnoreCase))
                return MapPaymentStatusLabel(text);
            if (string.Equals(exportKind, ExportAuditKinds.FinanceReceiptList, StringComparison.OrdinalIgnoreCase))
                return MapReceiptStatusLabel(text);
        }

        return null;
    }

    private static string MapYesNo(string raw)
    {
        if (bool.TryParse(raw, out var b)) return b ? "是" : "否";
        if (raw is "1" or "true" or "True") return "是";
        if (raw is "0" or "false" or "False") return "否";
        return raw;
    }

    private static string MapCurrencyIso(string raw)
    {
        if (!TryParseShort(raw, out var code)) return raw;
        return Enum.IsDefined(typeof(CurrencyCode), code)
            ? ((CurrencyCode)code).ToIsoText()
            : raw;
    }

    private static string MapPaymentModeLabel(string raw) =>
        TryParseShort(raw, out var code)
            ? code switch { 1 => "银行转账", 2 => "现金", 3 => "支票", 4 => "承兑汇票", _ => raw }
            : raw;

    private static string MapVerificationStatusLabel(string raw) =>
        TryParseShort(raw, out var code)
            ? code switch { 0 => "未核销", 1 => "部分核销", 2 => "核销完成", _ => raw }
            : raw;

    private static string MapPaymentDoneStatusLabel(string raw) =>
        TryParseShort(raw, out var code)
            ? code switch { 0 => "未付款", 1 => "部分付款", 2 => "付款完成", _ => raw }
            : raw;

    private static string MapReceiveStatusLabel(string raw) =>
        TryParseShort(raw, out var code)
            ? code switch { 0 => "未收款", 1 => "部分收款", 2 => "收款完成", _ => raw }
            : raw;

    private static string MapReceiptPurposeLabel(string raw) =>
        TryParseShort(raw, out var code)
            ? code switch
            {
                FinanceReceiptPurposeCode.Normal => "普通",
                FinanceReceiptPurposeCode.Advance => "预收",
                _ => raw
            }
            : raw;

    private static string MapFfPayableStatusLabel(string raw) =>
        TryParseShort(raw, out var code)
            ? code switch
            {
                FinanceFreightForwarderPayableStatusCodes.Pending => "待付款",
                FinanceFreightForwarderPayableStatusCodes.Partial => "部分付款",
                FinanceFreightForwarderPayableStatusCodes.Completed => "付款完成",
                _ => raw
            }
            : raw;

    private static string MapInvoiceStatusLabel(string raw) =>
        TryParseShort(raw, out var code)
            ? code switch
            {
                1 => "未申请",
                2 => "申请中",
                100 => "已开票",
                101 => "开票失败",
                -1 => "已作废",
                _ => raw
            }
            : raw;

    private static string MapPaymentStatusLabel(string raw) =>
        TryParseShort(raw, out var code)
            ? code switch
            {
                1 => "新建",
                2 => "待审核",
                10 => "审核通过",
                100 => "付款完成",
                -1 => "审核失败",
                -2 => "取消",
                _ => raw
            }
            : raw;

    private static string MapReceiptStatusLabel(string raw) =>
        TryParseShort(raw, out var code)
            ? code switch
            {
                0 => "草稿",
                1 => "待审核",
                2 => "已审核",
                3 => "已收款",
                4 => "已取消",
                _ => raw
            }
            : raw;

    private static string? MapOrderItemFilterDisplayValue(string exportKind, string key, string text)
    {
        var isSo = string.Equals(exportKind, ExportAuditKinds.SalesOrderItemList, StringComparison.OrdinalIgnoreCase);
        var isPo = string.Equals(exportKind, ExportAuditKinds.PurchaseOrderItemList, StringComparison.OrdinalIgnoreCase);
        if (!isSo && !isPo) return null;

        if (key.Equals("transactionCurrency", StringComparison.OrdinalIgnoreCase))
        {
            return text.Trim().ToLowerInvariant() switch
            {
                "rmb" => "人民币",
                "foreign" => "外币",
                _ => text
            };
        }

        if (key.Equals("orderType", StringComparison.OrdinalIgnoreCase))
        {
            return TryParseShort(text, out var ot)
                ? ot switch { 1 => "客单采购", 2 => "备货采购", 3 => "样品采购", _ => text }
                : text;
        }

        if (key.Equals("quickFilter", StringComparison.OrdinalIgnoreCase))
            return MapOrderItemQuickFilterLabel(text);

        if (key.Equals("stockOutPending", StringComparison.OrdinalIgnoreCase)
            || key.Equals("receiptPending", StringComparison.OrdinalIgnoreCase)
            || key.Equals("invoicePending", StringComparison.OrdinalIgnoreCase))
            return MapYesNo(text);

        if (key.EndsWith("ProgressStatus", StringComparison.OrdinalIgnoreCase))
            return MapTriProgressCsv(text);

        return null;
    }

    private static string MapOrderItemQuickFilterLabel(string raw)
    {
        return raw.Trim() switch
        {
            SellOrderItemListQuickFilterCodes.PendingSubmitAudit
                or PurchaseOrderItemListQuickFilterCodes.PendingSubmitAudit => "待提交审核",
            SellOrderItemListQuickFilterCodes.PendingSubmitPurchaseReq => "待提交采购申请",
            SellOrderItemListQuickFilterCodes.PendingSubmitStockOutNotify => "待提交出库通知",
            SellOrderItemListQuickFilterCodes.AppliedPendingPo => "已申请待下采购",
            SellOrderItemListQuickFilterCodes.PurchasedPendingStockIn => "已采购待入库",
            SellOrderItemListQuickFilterCodes.NotifyPendingPacking => "已通知待装箱",
            SellOrderItemListQuickFilterCodes.PackedPendingStockOut => "已装箱待出库",
            SellOrderItemListQuickFilterCodes.InStockPendingOut => "在库待出库",
            SellOrderItemListQuickFilterCodes.UsedStocking => "使用备货",
            SellOrderItemListQuickFilterCodes.StockOutPendingReceipt => "已出库待收款",
            SellOrderItemListQuickFilterCodes.ReceiptPartial => "部分收款",
            SellOrderItemListQuickFilterCodes.ReceiptComplete => "收款完成",
            PurchaseOrderItemListQuickFilterCodes.PendingVendorConfirm => "待供应商确认",
            PurchaseOrderItemListQuickFilterCodes.PendingSubmitPaymentRequest => "待提交请款",
            PurchaseOrderItemListQuickFilterCodes.PendingSubmitArrivalNotify => "待提交到货通知",
            PurchaseOrderItemListQuickFilterCodes.PayLater => "后付款",
            PurchaseOrderItemListQuickFilterCodes.ConfirmedUnpaid => "已确认未付款",
            PurchaseOrderItemListQuickFilterCodes.StockedInUnpaid => "已入库未付款",
            PurchaseOrderItemListQuickFilterCodes.PaymentPartial => "部分付款",
            PurchaseOrderItemListQuickFilterCodes.PaymentComplete => "付款完成",
            PurchaseOrderItemListQuickFilterCodes.ConfirmedPendingStockIn => "已确认待入库",
            PurchaseOrderItemListQuickFilterCodes.PaidPendingStockIn => "已付款待入库",
            PurchaseOrderItemListQuickFilterCodes.StockedIn => "已入库",
            _ => raw
        };
    }

    private static string MapTriProgressCsv(string raw)
    {
        var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return raw;
        return string.Join("、", parts.Select(MapTriProgressOne));
    }

    private static string MapTriProgressOne(string raw) =>
        TryParseShort(raw, out var code)
            ? code switch { 0 => "待处理", 1 => "部分", 2 => "完成", _ => raw }
            : raw;
}
