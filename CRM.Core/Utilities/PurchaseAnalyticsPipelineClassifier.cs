using CRM.Core.Models.Purchase;

namespace CRM.Core.Utilities;

/// <summary>采购看板全链路环节：按明细行当前瓶颈阶段归类（互斥，用于饼图占比）。</summary>
public static class PurchaseAnalyticsPipelineClassifier
{
    public const string StockInPending = "stock_in_pending";
    public const string PaymentPending = "payment_pending";
    public const string InvoicePending = "invoice_pending";
    public const string Completed = "completed";

    public static string Classify(PurchaseOrderItemExtend ext) =>
        Classify(
            ext.StockInProgressStatus,
            ext.PaymentProgressStatus,
            ext.InvoiceProgressStatus,
            ext.PaymentAmountNot,
            ext.PurchaseInvoiceToBe);

    public static string Classify(
        short stockInProgressStatus,
        short paymentProgressStatus,
        short invoiceProgressStatus,
        decimal paymentAmountNot,
        decimal purchaseInvoiceToBe)
    {
        if (stockInProgressStatus < 2)
            return StockInPending;
        if (paymentProgressStatus < 2 || paymentAmountNot > 0)
            return PaymentPending;
        if (invoiceProgressStatus < 2 || purchaseInvoiceToBe > 0)
            return InvoicePending;
        return Completed;
    }

    public static string Label(string key) => key switch
    {
        StockInPending => "待入库",
        PaymentPending => "待付款",
        InvoicePending => "待进项开票",
        Completed => "已完结",
        _ => key
    };
}
