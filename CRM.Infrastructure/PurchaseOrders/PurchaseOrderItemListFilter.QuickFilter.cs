using CRM.Core.Constants;

namespace CRM.Infrastructure.PurchaseOrders;

internal static partial class PurchaseOrderItemListFilter
{
    private const short PoCancelled = -2;
    private const short ItemCancelled = -2;
    private const short PoConfirmed = 30;

    public static IQueryable<PurchaseOrderItemLineJoin> ApplyQuickFilter(
        IQueryable<PurchaseOrderItemLineJoin> q,
        string? quickFilter)
    {
        if (string.IsNullOrWhiteSpace(quickFilter)
            || !PurchaseOrderItemListQuickFilterCodes.IsKnown(quickFilter))
            return q;

        return quickFilter.Trim() switch
        {
            PurchaseOrderItemListQuickFilterCodes.PendingSubmitAudit => ApplyPendingSubmitAudit(q),
            PurchaseOrderItemListQuickFilterCodes.PendingVendorConfirm => ApplyPendingVendorConfirm(q),
            PurchaseOrderItemListQuickFilterCodes.PendingSubmitPaymentRequest => ApplyPendingSubmitPaymentRequest(q),
            PurchaseOrderItemListQuickFilterCodes.PendingSubmitArrivalNotify => ApplyPendingSubmitArrivalNotify(q),
            PurchaseOrderItemListQuickFilterCodes.PayLater => ApplyPayLater(q),
            PurchaseOrderItemListQuickFilterCodes.ConfirmedUnpaid => ApplyConfirmedUnpaid(q),
            PurchaseOrderItemListQuickFilterCodes.StockedInUnpaid => ApplyStockedInUnpaid(q),
            PurchaseOrderItemListQuickFilterCodes.PaymentPartial => ApplyPaymentPartial(q),
            PurchaseOrderItemListQuickFilterCodes.PaymentComplete => ApplyPaymentComplete(q),
            PurchaseOrderItemListQuickFilterCodes.ConfirmedPendingStockIn => ApplyConfirmedPendingStockIn(q),
            PurchaseOrderItemListQuickFilterCodes.PaidPendingStockIn => ApplyPaidPendingStockIn(q),
            PurchaseOrderItemListQuickFilterCodes.StockedIn => ApplyStockedIn(q),
            _ => q
        };
    }

    private static IQueryable<PurchaseOrderItemLineJoin> ApplyExcludeCancelled(IQueryable<PurchaseOrderItemLineJoin> q) =>
        q.Where(x => x.Po.Status != PoCancelled && x.Item.Status != ItemCancelled && !x.Item.IsDeleted);

    private static IQueryable<PurchaseOrderItemLineJoin> ApplyExcludeCancelOnly(IQueryable<PurchaseOrderItemLineJoin> q) =>
        q.Where(x => x.Po.Status != PoCancelled && x.Item.Status != ItemCancelled && !x.Item.IsDeleted);

    private static IQueryable<PurchaseOrderItemLineJoin> ApplyPendingSubmitAudit(IQueryable<PurchaseOrderItemLineJoin> q) =>
        ApplyExcludeCancelled(q).Where(x =>
            x.Po.Status == 1 || x.Po.Status == 2 || x.Po.Status == -1);

    private static IQueryable<PurchaseOrderItemLineJoin> ApplyPendingVendorConfirm(IQueryable<PurchaseOrderItemLineJoin> q) =>
        ApplyExcludeCancelled(q).Where(x => x.Po.Status == 20);

    /// <summary>与列表 <c>canApplyPayment</c> 同口径（忽略权限）：主单已确认、财务付款未完成、行应付余额 &gt; 0。</summary>
    private static IQueryable<PurchaseOrderItemLineJoin> ApplyPendingSubmitPaymentRequest(IQueryable<PurchaseOrderItemLineJoin> q) =>
        ApplyExcludeCancelled(q).Where(x =>
            x.Po.Status == PoConfirmed
            && x.Item.FinancePaymentStatus < 2
            && (x.Item.Status == PoConfirmed || x.Po.Status == PoConfirmed)
            && (x.Item.Qty * x.Item.Cost - (x.Ext != null ? x.Ext.PaymentAmountRequested : 0m)) > 0m);

    private static IQueryable<PurchaseOrderItemLineJoin> ApplyPendingSubmitArrivalNotify(IQueryable<PurchaseOrderItemLineJoin> q) =>
        ApplyExcludeCancelled(q).Where(x =>
            x.Po.Status == PoConfirmed
            && (x.Ext != null ? x.Ext.QtyStockInNotifyNot > 0m : x.Item.Qty > 0m));

    private static IQueryable<PurchaseOrderItemLineJoin> ApplyPayLater(IQueryable<PurchaseOrderItemLineJoin> q) =>
        ApplyExcludeCancelOnly(q).Where(x => x.Po.IsPayLater);

    private static IQueryable<PurchaseOrderItemLineJoin> ApplyConfirmedUnpaid(IQueryable<PurchaseOrderItemLineJoin> q) =>
        ApplyExcludeCancelled(q).Where(x =>
            x.Po.Status == PoConfirmed
            && (x.Ext == null || x.Ext.PaymentProgressStatus == 0));

    private static IQueryable<PurchaseOrderItemLineJoin> ApplyStockedInUnpaid(IQueryable<PurchaseOrderItemLineJoin> q) =>
        ApplyExcludeCancelOnly(q).Where(x =>
            x.Ext != null
            && x.Ext.StockInProgressStatus >= 1
            && x.Ext.PaymentProgressStatus == 0);

    private static IQueryable<PurchaseOrderItemLineJoin> ApplyPaymentPartial(IQueryable<PurchaseOrderItemLineJoin> q) =>
        ApplyExcludeCancelOnly(q).Where(x =>
            x.Ext != null && x.Ext.PaymentProgressStatus == 1);

    private static IQueryable<PurchaseOrderItemLineJoin> ApplyPaymentComplete(IQueryable<PurchaseOrderItemLineJoin> q) =>
        ApplyExcludeCancelOnly(q).Where(x =>
            x.Ext != null && x.Ext.PaymentProgressStatus == 2);

    private static IQueryable<PurchaseOrderItemLineJoin> ApplyConfirmedPendingStockIn(IQueryable<PurchaseOrderItemLineJoin> q) =>
        ApplyExcludeCancelled(q).Where(x =>
            x.Po.Status == PoConfirmed
            && (x.Ext == null || x.Ext.StockInProgressStatus == 0));

    private static IQueryable<PurchaseOrderItemLineJoin> ApplyPaidPendingStockIn(IQueryable<PurchaseOrderItemLineJoin> q) =>
        ApplyExcludeCancelOnly(q).Where(x =>
            x.Ext != null
            && x.Ext.PaymentProgressStatus == 2
            && x.Ext.StockInProgressStatus < 2);

    private static IQueryable<PurchaseOrderItemLineJoin> ApplyStockedIn(IQueryable<PurchaseOrderItemLineJoin> q) =>
        ApplyExcludeCancelOnly(q).Where(x =>
            x.Ext != null
            && (x.Ext.StockInProgressStatus == 1 || x.Ext.StockInProgressStatus == 2));
}
