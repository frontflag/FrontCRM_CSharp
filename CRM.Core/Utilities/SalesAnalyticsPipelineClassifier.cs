using CRM.Core.Models.Sales;

namespace CRM.Core.Utilities;

/// <summary>销售看板全链路环节：按明细行当前瓶颈阶段归类（互斥，用于饼图占比）。</summary>
public static class SalesAnalyticsPipelineClassifier
{
    public const string PurchasePending = "purchase_pending";
    public const string StockInPending = "stock_in_pending";
    public const string StockOutPending = "stock_out_pending";
    public const string ReceiptPending = "receipt_pending";
    public const string InvoicePending = "invoice_pending";
    public const string Completed = "completed";

    public static string Classify(SellOrderItemExtend ext) =>
        Classify(
            ext.PurchaseProgressStatus,
            ext.StockInProgressStatus,
            ext.StockOutProgressStatus,
            ext.ReceiptProgressStatus,
            ext.InvoiceProgressStatus,
            ext.ReceiptAmountNot,
            ext.InvoiceAmountNot);

    public static string Classify(
        short purchaseProgressStatus,
        short stockInProgressStatus,
        short stockOutProgressStatus,
        short receiptProgressStatus,
        short invoiceProgressStatus,
        decimal receiptAmountNot,
        decimal invoiceAmountNot)
    {
        if (purchaseProgressStatus < 2)
            return PurchasePending;
        if (stockInProgressStatus < 2)
            return StockInPending;
        if (stockOutProgressStatus < 2)
            return StockOutPending;
        if (receiptProgressStatus < 2 || receiptAmountNot > 0)
            return ReceiptPending;
        if (invoiceProgressStatus < 2 || invoiceAmountNot > 0)
            return InvoicePending;
        return Completed;
    }

    public static string Label(string key) => key switch
    {
        PurchasePending => "待采购",
        StockInPending => "待入库",
        StockOutPending => "待出库",
        ReceiptPending => "待收款",
        InvoicePending => "待开票",
        Completed => "已完结",
        _ => key
    };
}
