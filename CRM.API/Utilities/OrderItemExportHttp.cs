using System.Globalization;
using System.Text;
using System.Collections.Generic;
using CRM.Core.Interfaces;
using CRM.Core.Utilities;

namespace CRM.API.Utilities;

/// <summary>销售/采购订单明细列表 CSV。</summary>
public static class OrderItemExportHttp
{
    public static string BuildSalesOrderItemCsv(
        IReadOnlyList<SellOrderItemLineDto> items,
        bool mask521,
        bool canViewCustomer,
        bool canViewAmount)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',',
            "销售订单明细", "销售订单号", "主状态",
            "客户中文", "客户英文", "业务员", "采购员",
            "物料型号", "品牌", "客户订单号", "客户料号", "数量",
            "单价", "单价币别", "行金额", "行金额币别",
            "折算美金单价", "折算美金行金额",
            "销售利润", "出库利润", "出库利润率",
            "采购进度", "入库进度", "出库通知进度", "出库进度", "收款进度", "开票进度",
            "创建日期", "创建人"));
        foreach (var r in items)
        {
            var hideParty = mask521 || !canViewCustomer;
            var hideAmount = !canViewAmount;
            sb.AppendLine(string.Join(',',
                InventoryExportHttp.CsvCell(r.SellOrderItemCode),
                InventoryExportHttp.CsvCell(r.SellOrderCode),
                InventoryExportHttp.CsvCell(SalesOrderStatusLabel(r.OrderStatus)),
                InventoryExportHttp.CsvCell(hideParty ? "***" : (r.CustomerName ?? string.Empty)),
                InventoryExportHttp.CsvCell(hideParty ? "***" : (r.CustomerEnglishName ?? string.Empty)),
                InventoryExportHttp.CsvCell(mask521 ? "***" : (r.SalesUserName ?? string.Empty)),
                InventoryExportHttp.CsvCell(r.PurchaseUserAccountDisplay),
                InventoryExportHttp.CsvCell(r.PN),
                InventoryExportHttp.CsvCell(r.Brand),
                InventoryExportHttp.CsvCell(hideParty ? string.Empty : (r.CustomerSo ?? string.Empty)),
                InventoryExportHttp.CsvCell(hideParty ? string.Empty : (r.CustomerPn ?? string.Empty)),
                InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDecimal(r.Qty)),
                AmountCell(hideAmount, r.Price),
                CurrencyCell(hideAmount, r.Currency),
                AmountCell(hideAmount, r.LineTotal),
                CurrencyCell(hideAmount, r.Currency),
                AmountCell(hideAmount, r.UsdUnitPrice),
                AmountCell(hideAmount, r.UsdLineTotal),
                AmountCell(hideAmount, r.SalesProfitExpected),
                AmountCell(hideAmount, r.ProfitOutBizUsd),
                InventoryExportHttp.CsvCell(hideAmount ? string.Empty : ProfitRateCell(r.ProfitOutBizUsd, r.ProfitOutRateBiz)),
                InventoryExportHttp.CsvCell(SoProgressLabel("purchase", r.PurchaseProgressStatus)),
                InventoryExportHttp.CsvCell(SoProgressLabel("stockIn", r.StockInProgressStatus)),
                InventoryExportHttp.CsvCell(SoProgressLabel("stockOutNotify", r.StockOutNotifyProgressStatus)),
                InventoryExportHttp.CsvCell(SoProgressLabel("stockOut", r.StockOutProgressStatus)),
                InventoryExportHttp.CsvCell(SoProgressLabel("receipt", r.ReceiptProgressStatus)),
                InventoryExportHttp.CsvCell(SoProgressLabel("invoice", r.InvoiceProgressStatus)),
                InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDateTime(r.OrderCreateTime)),
                InventoryExportHttp.CsvCell(mask521 ? "***" : (r.SalesUserName ?? string.Empty))));
        }

        return sb.ToString();
    }

    public static string BuildPurchaseOrderItemCsv(
        IReadOnlyList<PurchaseOrderItemListLineDto> items,
        bool hideParty,
        bool hideAmount,
        bool includeStockingAvailableQty = false)
    {
        var sb = new StringBuilder();
        var headers = new List<string>
        {
            "采购订单明细", "采购订单号", "货代单号", "明细状态",
            "供应商中文", "供应商英文", "采购员",
            "物料型号", "品牌",
            includeStockingAvailableQty ? "采购数量" : "数量"
        };
        if (includeStockingAvailableQty)
            headers.Add("可用库存数量");
        headers.AddRange(
        [
            includeStockingAvailableQty ? "采购单价" : "单价", "单价币别", "行金额", "行金额币别",
            "创建日期", "创建人",
            "请款进度", "付款进度", "采购进度", "入库进度", "发票进度"
        ]);
        sb.AppendLine(string.Join(',', headers));
        foreach (var r in items)
        {
            var cells = new List<string>
            {
                InventoryExportHttp.CsvCell(r.PurchaseOrderItemCode),
                InventoryExportHttp.CsvCell(r.PurchaseOrderCode),
                InventoryExportHttp.CsvCell(r.FreightForwarderOrderNo),
                InventoryExportHttp.CsvCell(PurchaseItemStatusLabel(r.ItemStatus)),
                InventoryExportHttp.CsvCell(hideParty ? "***" : (r.VendorName ?? string.Empty)),
                InventoryExportHttp.CsvCell(hideParty ? "***" : (r.VendorEnglishName ?? string.Empty)),
                InventoryExportHttp.CsvCell(r.PurchaseUserName),
                InventoryExportHttp.CsvCell(r.Pn),
                InventoryExportHttp.CsvCell(r.Brand),
                InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDecimal(r.Qty))
            };
            if (includeStockingAvailableQty)
                cells.Add(InventoryExportHttp.CsvCell(r.StockingAvailableQty.ToString(CultureInfo.InvariantCulture)));
            cells.AddRange(
            [
                AmountCell(hideAmount, r.Cost),
                CurrencyCell(hideAmount, r.Currency),
                AmountCell(hideAmount, r.LineTotal),
                CurrencyCell(hideAmount, r.Currency),
                InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDateTime(r.OrderCreateTime)),
                InventoryExportHttp.CsvCell(r.CreateUserName),
                InventoryExportHttp.CsvCell(r.PaymentRequestProgressStatus >= 1 ? "已申请" : "待申请"),
                InventoryExportHttp.CsvCell(PoProgressLabel("payment", r.PaymentProgressStatus)),
                InventoryExportHttp.CsvCell(PoProgressLabel("purchase", r.PurchaseProgressStatus)),
                InventoryExportHttp.CsvCell(PoProgressLabel("stockIn", r.StockInProgressStatus)),
                InventoryExportHttp.CsvCell(PoProgressLabel("invoice", r.InvoiceProgressStatus))
            ]);
            sb.AppendLine(string.Join(',', cells));
        }

        return sb.ToString();
    }

    public static PurchaseOrderItemListLineDto ToPurchaseExportRow(
        PurchaseOrderItemListLineRaw r,
        bool canViewVendor,
        bool canViewAmount,
        bool canViewPurchaseUser,
        IReadOnlyDictionary<string, string?> createUserLoginByUserId,
        IReadOnlySet<string> poItemIdsWithActivePaymentRequest,
        IReadOnlyDictionary<string, string> vendorEnglishMap)
    {
        var createKey = (r.CreateByUserId ?? string.Empty).Trim();
        string? createUserName = null;
        if (!string.IsNullOrEmpty(createKey) && createUserLoginByUserId.TryGetValue(createKey, out var login))
            createUserName = login;

        return new PurchaseOrderItemListLineDto
        {
            PurchaseOrderItemId = r.PurchaseOrderItemId,
            PurchaseOrderId = r.PurchaseOrderId,
            PurchaseOrderItemCode = r.PurchaseOrderItemCode,
            PurchaseOrderCode = r.PurchaseOrderCode,
            FreightForwarderOrderNo = r.FreightForwarderOrderNo,
            PurchaseOrderType = r.PurchaseOrderType,
            VendorId = canViewVendor ? r.VendorId : string.Empty,
            VendorName = canViewVendor ? r.VendorName : null,
            VendorEnglishName = canViewVendor && !string.IsNullOrWhiteSpace(r.VendorId)
                && vendorEnglishMap.TryGetValue(r.VendorId.Trim(), out var ven)
                ? ven
                : null,
            ItemStatus = r.ItemStatus,
            PurchaseProgressStatus = r.PurchaseProgressStatus,
            StockInProgressStatus = r.StockInProgressStatus,
            PaymentRequestProgressStatus = poItemIdsWithActivePaymentRequest.Contains(r.PurchaseOrderItemId)
                ? (short)1
                : (short)0,
            PaymentProgressStatus = r.PaymentProgressStatus,
            InvoiceProgressStatus = r.InvoiceProgressStatus,
            OrderCreateTime = r.OrderCreateTime,
            PurchaseUserName = canViewPurchaseUser ? r.PurchaseUserName : null,
            CreateUserName = createUserName,
            Pn = r.Pn,
            Brand = r.Brand,
            Qty = r.Qty,
            StockingAvailableQty = r.StockingAvailableQty,
            Cost = canViewAmount ? r.Cost : 0m,
            LineTotal = canViewAmount ? r.Qty * r.Cost : 0m,
            Currency = r.Currency
        };
    }

    private static string AmountCell(bool hide, decimal value) =>
        InventoryExportHttp.CsvCell(hide ? string.Empty : InventoryExportHttp.FormatDecimal(value));

    private static string AmountCell(bool hide, decimal? value) =>
        InventoryExportHttp.CsvCell(hide || !value.HasValue ? string.Empty : InventoryExportHttp.FormatDecimal(value));

    private static string CurrencyCell(bool hide, short code) =>
        InventoryExportHttp.CsvCell(hide ? string.Empty : InventoryExportHttp.CurrencyLabel(code));

    private static string ProfitRateCell(decimal profitUsd, decimal? rate)
    {
        if (rate == null) return string.Empty;
        if (rate.Value == 0m && profitUsd >= 0m) return string.Empty;
        return rate.Value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    public static string SalesOrderStatusLabel(short status) => status switch
    {
        1 => "新建",
        2 => "待审核",
        10 => "审核通过",
        20 => "进行中",
        100 => "完成",
        -1 => "审核失败",
        -2 => "取消",
        _ => status.ToString(CultureInfo.InvariantCulture)
    };

    public static string PurchaseItemStatusLabel(short status) => status switch
    {
        1 => "新建",
        2 => "待审核",
        10 => "审核通过",
        20 => "待确认",
        30 => "已确认",
        40 => "已付款",
        50 => "已发货",
        60 => "已入库",
        100 => "采购完成",
        -1 => "审核失败",
        -2 => "取消",
        _ => status.ToString(CultureInfo.InvariantCulture)
    };

    private static string SoProgressLabel(string kind, short status)
    {
        var slot = status == 2 ? "complete" : status == 1 ? "partial" : "pending";
        return (kind, slot) switch
        {
            ("purchase", "pending") => "待采购",
            ("purchase", "partial") => "采购中",
            ("purchase", "complete") => "采购完成",
            ("stockIn", "pending") => "待入库",
            ("stockIn", "partial") => "部分入库",
            ("stockIn", "complete") => "入库完成",
            ("stockOut", "pending") => "待出库",
            ("stockOut", "partial") => "部分出库",
            ("stockOut", "complete") => "出库完成",
            ("stockOutNotify", "pending") => "未通知",
            ("stockOutNotify", "partial") => "部分通知",
            ("stockOutNotify", "complete") => "通知完成",
            ("receipt", "pending") => "待收款",
            ("receipt", "partial") => "部分收款",
            ("receipt", "complete") => "收款完成",
            ("invoice", "pending") => "待开票",
            ("invoice", "partial") => "部分开票",
            ("invoice", "complete") => "开票完成",
            _ => status.ToString(CultureInfo.InvariantCulture)
        };
    }

    private static string PoProgressLabel(string kind, short status)
    {
        var slot = status == 2 ? "done" : status == 1 ? "partial" : "pending";
        return (kind, slot) switch
        {
            ("purchase", "pending") => "待采购",
            ("purchase", "partial") => "采购中",
            ("purchase", "done") => "采购完成",
            ("stockIn", "pending") => "待入库",
            ("stockIn", "partial") => "部分入库",
            ("stockIn", "done") => "入库完成",
            ("payment", "pending") => "待付款",
            ("payment", "partial") => "部分付款",
            ("payment", "done") => "付款完成",
            ("invoice", "pending") => "待开票",
            ("invoice", "partial") => "部分开票",
            ("invoice", "done") => "开票完成",
            _ => status.ToString(CultureInfo.InvariantCulture)
        };
    }
}
