using System.Globalization;
using System.Security.Claims;
using System.Text;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Services;
using CRM.Core.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Utilities;

/// <summary>财务列表 CSV 导出：列标签、枚举文案、审计写入。</summary>
public static class FinanceExportHttp
{
    public static string MaskedName(bool mask, string? value) =>
        mask ? "***" : (value ?? string.Empty);

    public static string AmountCell(bool maskAmount, decimal value) =>
        InventoryExportHttp.CsvCell(maskAmount ? string.Empty : InventoryExportHttp.FormatDecimal(value));

    public static string CurrencyCell(bool maskAmount, short? code) =>
        InventoryExportHttp.CsvCell(maskAmount ? string.Empty : InventoryExportHttp.CurrencyLabel(code));

    public static string CurrencyCell(bool maskAmount, byte code) =>
        CurrencyCell(maskAmount, (short)code);

    public static string PaymentStatusLabel(short status) => status switch
    {
        1 => "新建",
        2 => "待审核",
        10 => "审核通过",
        100 => "付款完成",
        -1 => "审核失败",
        -2 => "取消",
        _ => status.ToString(CultureInfo.InvariantCulture)
    };

    public static string ReceiptStatusLabel(short status) => status switch
    {
        0 => "草稿",
        1 => "待审核",
        2 => "已审核",
        3 => "已收款",
        4 => "已取消",
        _ => status.ToString(CultureInfo.InvariantCulture)
    };

    public static string PaymentModeLabel(short mode) => mode switch
    {
        1 => "银行转账",
        2 => "现金",
        3 => "支票",
        4 => "承兑汇票",
        _ => mode.ToString(CultureInfo.InvariantCulture)
    };

    public static string VerificationStatusLabel(short status) => status switch
    {
        0 => "未核销",
        1 => "部分核销",
        2 => "核销完成",
        _ => status.ToString(CultureInfo.InvariantCulture)
    };

    public static string ReceiptPurposeLabel(short purpose) => purpose switch
    {
        FinanceReceiptPurposeCode.Advance => "预收",
        _ => "普通"
    };

    public static string InvoiceStatusLabel(short status) => status switch
    {
        1 => "未申请",
        2 => "申请中",
        100 => "已开票",
        101 => "开票失败",
        -1 => "已作废",
        _ => status.ToString(CultureInfo.InvariantCulture)
    };

    /// <summary>进项发票列表开票状态与前端 normalize 一致：冲红作废，已认证视为已开票，否则未申请。</summary>
    public static string PurchaseInvoiceIssueStatusLabel(FinancePurchaseInvoice inv)
    {
        if (inv.RedInvoiceStatus == 1) return InvoiceStatusLabel(-1);
        if (inv.ConfirmStatus == 1) return InvoiceStatusLabel(100);
        return InvoiceStatusLabel(1);
    }

    public static string PaymentDoneStatusLabel(byte status) => status switch
    {
        0 => "未付款",
        1 => "部分付款",
        2 => "付款完成",
        _ => status.ToString(CultureInfo.InvariantCulture)
    };

    public static string ReceiveStatusLabel(byte status) => status switch
    {
        0 => "未收款",
        1 => "部分收款",
        2 => "收款完成",
        _ => status.ToString(CultureInfo.InvariantCulture)
    };

    public static string InvoiceTypeVatLabel(short type) => type switch
    {
        200 => "增值税普通发票",
        _ => "增值税专用发票"
    };

    public static string FfPayableStatusLabel(short status) => status switch
    {
        FinanceFreightForwarderPayableStatusCodes.Partial => "部分付款",
        FinanceFreightForwarderPayableStatusCodes.Completed => "付款完成",
        _ => "待付款"
    };

    public static async Task AppendListLogAsync(
        IExportOperationLogService exportLog,
        string bizType,
        string recordCode,
        string actionType,
        string exportKind,
        string listTitle,
        int exportedCount,
        bool truncated,
        IReadOnlyDictionary<string, object?> filters,
        bool filtersMasked,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var truncNote = truncated ? "（已截断）" : string.Empty;
        await exportLog.AppendAsync(new ExportOperationLogRequest
        {
            BizType = bizType,
            RecordId = ExportOperationAudit.ListRecordId,
            RecordCode = recordCode,
            ActionType = actionType,
            ExportKind = exportKind,
            OperationDesc = $"导出{listTitle} {exportedCount} 条{truncNote}",
            ExportedCount = exportedCount,
            Truncated = truncated,
            Filters = filters,
            FiltersMasked = filtersMasked,
            OperatorUserId = InventoryExportHttp.UserId(user),
            OperatorUserName = InventoryExportHttp.UserName(user)
        }, cancellationToken);
    }

    public static string BuildPaymentCsv(IReadOnlyList<FinancePayment> items, bool mask511)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',',
            "状态", "供应商中文", "供应商英文", "收款银行", "付款银行",
            "请款金额", "请款金额币别", "付款金额", "付款金额币别",
            "付款方式", "付款日期", "水单号", "备注", "货代单号", "付款单号",
            "创建日期", "创建人"));
        foreach (var r in items)
        {
            sb.AppendLine(string.Join(',',
                InventoryExportHttp.CsvCell(PaymentStatusLabel(r.Status)),
                InventoryExportHttp.CsvCell(MaskedName(mask511, r.VendorName)),
                InventoryExportHttp.CsvCell(MaskedName(mask511, r.VendorEnglishName)),
                InventoryExportHttp.CsvCell(MaskedName(mask511, r.VendorBankName)),
                InventoryExportHttp.CsvCell(MaskedName(mask511, r.PaymentBankName)),
                AmountCell(false, r.PaymentAmountToBe),
                CurrencyCell(false, r.PaymentCurrency),
                AmountCell(false, r.PaymentAmount),
                CurrencyCell(false, r.PaymentCurrency),
                InventoryExportHttp.CsvCell(PaymentModeLabel(r.PaymentMode)),
                InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDate(r.PaymentDate)),
                InventoryExportHttp.CsvCell(r.BankSlipNo),
                InventoryExportHttp.CsvCell(r.Remark),
                InventoryExportHttp.CsvCell(r.FreightForwarderOrderNo),
                InventoryExportHttp.CsvCell(r.FinancePaymentCode),
                InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDate(r.CreateTime)),
                InventoryExportHttp.CsvCell(r.CreateUserName)));
        }

        return sb.ToString();
    }

    public static string BuildPurchaseInvoiceCsv(IReadOnlyList<FinancePurchaseInvoice> items, bool mask511)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',',
            "开票状态", "发票单号", "开票日期", "供应商中文", "供应商英文", "发票号码",
            "发票金额", "发票金额币别", "核销状态", "核销金额", "核销金额币别",
            "付款状态", "付款金额", "付款金额币别", "发票备注", "发票类型", "创建日期", "创建人"));
        foreach (var r in items)
        {
            sb.AppendLine(string.Join(',',
                InventoryExportHttp.CsvCell(PurchaseInvoiceIssueStatusLabel(r)),
                InventoryExportHttp.CsvCell(r.InvoiceCode),
                InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDate(r.InvoiceDate)),
                InventoryExportHttp.CsvCell(MaskedName(mask511, r.VendorName)),
                InventoryExportHttp.CsvCell(MaskedName(mask511, r.VendorEnglishName)),
                InventoryExportHttp.CsvCell(r.InvoiceNo),
                AmountCell(mask511, r.InvoiceAmount),
                CurrencyCell(mask511, r.Currency),
                InventoryExportHttp.CsvCell(VerificationStatusLabel(r.VerificationStatus)),
                AmountCell(mask511, r.VerifiedDone),
                CurrencyCell(mask511, r.Currency),
                InventoryExportHttp.CsvCell(PaymentDoneStatusLabel(r.PaymentStatus)),
                AmountCell(mask511, r.PaymentDone),
                CurrencyCell(mask511, r.Currency),
                InventoryExportHttp.CsvCell(r.Remark),
                InventoryExportHttp.CsvCell(InvoiceTypeVatLabel(100)),
                InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDate(r.CreateTime)),
                InventoryExportHttp.CsvCell(string.Empty)));
        }

        return sb.ToString();
    }

    public static string BuildSellInvoiceCsv(IReadOnlyList<FinanceSellInvoice> items, bool mask521)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',',
            "开票状态", "发票单号", "开票日期", "客户中文", "客户英文", "发票号码",
            "发票金额", "发票金额币别", "核销状态", "核销金额", "核销金额币别",
            "收款状态", "收款金额", "收款金额币别", "发票备注", "发票类型", "创建日期", "创建人"));
        foreach (var r in items)
        {
            sb.AppendLine(string.Join(',',
                InventoryExportHttp.CsvCell(InvoiceStatusLabel(r.InvoiceStatus)),
                InventoryExportHttp.CsvCell(r.InvoiceCode),
                InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDate(r.MakeInvoiceDate)),
                InventoryExportHttp.CsvCell(MaskedName(mask521, r.CustomerName)),
                InventoryExportHttp.CsvCell(MaskedName(mask521, r.CustomerEnglishName)),
                InventoryExportHttp.CsvCell(r.InvoiceNo),
                AmountCell(mask521, r.InvoiceTotal),
                CurrencyCell(mask521, r.Currency),
                InventoryExportHttp.CsvCell(VerificationStatusLabel(r.MatchStatus)),
                AmountCell(mask521, r.MatchDone),
                CurrencyCell(mask521, r.Currency),
                InventoryExportHttp.CsvCell(ReceiveStatusLabel(r.ReceiveStatus)),
                AmountCell(mask521, r.ReceiveDone),
                CurrencyCell(mask521, r.Currency),
                InventoryExportHttp.CsvCell(r.Remark),
                InventoryExportHttp.CsvCell(InvoiceTypeVatLabel(r.SellInvoiceType)),
                InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDate(r.CreateTime)),
                InventoryExportHttp.CsvCell(string.Empty)));
        }

        return sb.ToString();
    }

    public static string BuildReceiptCsv(IReadOnlyList<FinanceReceipt> items, bool mask521)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',',
            "状态", "核销状态", "用途", "客户中文", "客户英文",
            "收款金额", "收款金额币别", "收款方式", "收款日期", "水单号", "备注",
            "收款单号", "创建日期", "创建人"));
        foreach (var r in items)
        {
            sb.AppendLine(string.Join(',',
                InventoryExportHttp.CsvCell(ReceiptStatusLabel(r.Status)),
                InventoryExportHttp.CsvCell(VerificationStatusLabel(r.VerificationStatus)),
                InventoryExportHttp.CsvCell(ReceiptPurposeLabel(r.ReceiptPurpose)),
                InventoryExportHttp.CsvCell(MaskedName(mask521, r.CustomerName)),
                InventoryExportHttp.CsvCell(MaskedName(mask521, r.CustomerEnglishName)),
                AmountCell(mask521, r.ReceiptAmount),
                CurrencyCell(mask521, r.ReceiptCurrency),
                InventoryExportHttp.CsvCell(PaymentModeLabel(r.ReceiptMode)),
                InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDate(r.ReceiptDate)),
                InventoryExportHttp.CsvCell(r.BankSlipNo),
                InventoryExportHttp.CsvCell(r.Remark),
                InventoryExportHttp.CsvCell(r.FinanceReceiptCode),
                InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDate(r.CreateTime)),
                InventoryExportHttp.CsvCell(r.CreateUserName)));
        }

        return sb.ToString();
    }

    public static string BuildReceivableCsv(IReadOnlyList<FinanceReceivableListItem> items, bool mask521)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',',
            "核销状态", "出库日期", "应收单号", "出库单号", "客户中文", "客户英文",
            "业务员", "型号", "品牌", "数量",
            "金额", "金额币别", "已核销", "已核销币别", "待核销", "待核销币别"));
        foreach (var r in items)
        {
            sb.AppendLine(string.Join(',',
                InventoryExportHttp.CsvCell(VerificationStatusLabel(r.VerificationStatus)),
                InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDate(r.StockOutDate)),
                InventoryExportHttp.CsvCell(r.ReceivableCode),
                InventoryExportHttp.CsvCell(r.StockOutCode),
                InventoryExportHttp.CsvCell(MaskedName(mask521, r.CustomerName)),
                InventoryExportHttp.CsvCell(MaskedName(mask521, r.CustomerEnglishName)),
                InventoryExportHttp.CsvCell(MaskedName(mask521, r.SalesUserName)),
                InventoryExportHttp.CsvCell(r.PN),
                InventoryExportHttp.CsvCell(r.Brand),
                InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDecimal(r.OutboundQty)),
                AmountCell(mask521, r.Amount),
                CurrencyCell(mask521, r.Currency),
                AmountCell(mask521, r.VerifiedDone),
                CurrencyCell(mask521, r.Currency),
                AmountCell(mask521, r.VerifiedToBe),
                CurrencyCell(mask521, r.Currency)));
        }

        return sb.ToString();
    }

    public static string BuildCustomerAdvanceCsv(IReadOnlyList<FinanceCustomerAdvance> items, bool mask521)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',',
            "客户中文", "客户英文",
            "余额", "余额币别", "累计入账", "累计入账币别", "累计核销", "累计核销币别"));
        foreach (var r in items)
        {
            sb.AppendLine(string.Join(',',
                InventoryExportHttp.CsvCell(MaskedName(mask521, r.CustomerName)),
                InventoryExportHttp.CsvCell(MaskedName(mask521, r.CustomerEnglishName)),
                AmountCell(mask521, r.Balance),
                CurrencyCell(mask521, r.Currency),
                AmountCell(mask521, r.TotalIn),
                CurrencyCell(mask521, r.Currency),
                AmountCell(mask521, r.TotalApplied),
                CurrencyCell(mask521, r.Currency)));
        }

        return sb.ToString();
    }

    public static string BuildFfPayableCsv(IReadOnlyList<FinanceFreightForwarderPayableListItem> items, bool mask521)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',',
            "状态", "收款单号", "客户中文", "客户英文", "货代公司",
            "收款金额", "收款金额币别", "已付", "已付币别", "待付", "待付币别", "收款日期"));
        foreach (var r in items)
        {
            sb.AppendLine(string.Join(',',
                InventoryExportHttp.CsvCell(FfPayableStatusLabel(r.PayableStatus)),
                InventoryExportHttp.CsvCell(r.FinanceReceiptCode),
                InventoryExportHttp.CsvCell(MaskedName(mask521, r.CustomerName)),
                InventoryExportHttp.CsvCell(MaskedName(mask521, r.CustomerEnglishName)),
                InventoryExportHttp.CsvCell(r.FreightForwarderCompanyName),
                AmountCell(false, r.ReceiptAmount),
                CurrencyCell(false, r.ReceiptCurrency),
                AmountCell(false, r.PaidAmount),
                CurrencyCell(false, r.ReceiptCurrency),
                AmountCell(false, r.PendingAmount),
                CurrencyCell(false, r.ReceiptCurrency),
                InventoryExportHttp.CsvCell(InventoryExportHttp.FormatDate(r.ReceiptDate))));
        }

        return sb.ToString();
    }

    public static FileContentResult CsvFile(string content, string fileName) =>
        InventoryExportHttp.CsvFile(content, fileName);
}
