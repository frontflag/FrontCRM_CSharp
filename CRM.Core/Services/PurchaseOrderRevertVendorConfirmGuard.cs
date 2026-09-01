using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;

namespace CRM.Core.Services;

/// <inheritdoc />
public sealed class PurchaseOrderRevertVendorConfirmGuard : IPurchaseOrderRevertVendorConfirmGuard
{
    private const short FinancePaymentStatusAuditFailed = -1;
    private const short FinancePaymentStatusCancelled = -2;

    private readonly IRepository<PurchaseOrderItem> _poItemRepo;
    private readonly IRepository<StockInNotify> _notifyRepo;
    private readonly IRepository<StockIn> _stockInRepo;
    private readonly IRepository<StockInItemExtend> _stockInItemExtendRepo;
    private readonly IRepository<FinancePayment> _paymentRepo;
    private readonly IRepository<FinancePaymentItem> _paymentItemRepo;
    private readonly IRepository<FinancePurchaseInvoice> _purchaseInvoiceRepo;
    private readonly IRepository<FinancePurchaseInvoiceItem> _purchaseInvoiceItemRepo;

    public PurchaseOrderRevertVendorConfirmGuard(
        IRepository<PurchaseOrderItem> poItemRepo,
        IRepository<StockInNotify> notifyRepo,
        IRepository<StockIn> stockInRepo,
        IRepository<StockInItemExtend> stockInItemExtendRepo,
        IRepository<FinancePayment> paymentRepo,
        IRepository<FinancePaymentItem> paymentItemRepo,
        IRepository<FinancePurchaseInvoice> purchaseInvoiceRepo,
        IRepository<FinancePurchaseInvoiceItem> purchaseInvoiceItemRepo)
    {
        _poItemRepo = poItemRepo;
        _notifyRepo = notifyRepo;
        _stockInRepo = stockInRepo;
        _stockInItemExtendRepo = stockInItemExtendRepo;
        _paymentRepo = paymentRepo;
        _paymentItemRepo = paymentItemRepo;
        _purchaseInvoiceRepo = purchaseInvoiceRepo;
        _purchaseInvoiceItemRepo = purchaseInvoiceItemRepo;
    }

    /// <inheritdoc />
    public async Task EnsureCanRevertAsync(string purchaseOrderId, string? purchaseOrderCode)
    {
        if (string.IsNullOrWhiteSpace(purchaseOrderId))
            throw new ArgumentException("采购订单ID不能为空", nameof(purchaseOrderId));

        var orderId = purchaseOrderId.Trim();
        var items = (await _poItemRepo.FindAsync(i => i.PurchaseOrderId == orderId && !i.IsDeleted)).ToList();
        var poItemIds = items
            .Select(i => i.Id.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var labels = new List<string>();

        var paymentLabels = await CollectActivePaymentLabelsAsync(orderId, poItemIds);
        if (paymentLabels.Count > 0)
            labels.Add("付款单 " + string.Join("、", paymentLabels));

        var notices = poItemIds.Count == 0
            ? new List<StockInNotify>()
            : (await _notifyRepo.FindAsync(n =>
                !n.IsDeleted
                && (n.PurchaseOrderId == orderId
                    || (n.PurchaseOrderItemId != null && poItemIds.Contains(n.PurchaseOrderItemId.Trim())))))
            .ToList();
        if (notices.Count > 0)
        {
            var codes = notices
                .Select(n => string.IsNullOrWhiteSpace(n.NoticeCode) ? n.Id : n.NoticeCode.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToList();
            labels.Add("到货通知 " + string.Join("、", codes));
        }

        var postedStockInLabels = await CollectPostedPurchaseStockInLabelsAsync(notices, poItemIds);
        if (postedStockInLabels.Count > 0)
            labels.Add("入库单 " + string.Join("、", postedStockInLabels));

        var invoiceLabels = await CollectInvoiceLabelsAsync(purchaseOrderCode, notices);
        if (invoiceLabels.Count > 0)
            labels.Add("进项发票 " + string.Join("、", invoiceLabels));

        if (labels.Count == 0)
            return;

        throw new InvalidOperationException(
            "存在下游单据，不能取消确认并退回待确认：" + string.Join("；", labels) + "。请先处理这些单据。");
    }

    private async Task<List<string>> CollectActivePaymentLabelsAsync(string orderId, HashSet<string> poItemIds)
    {
        var paymentItems = (await _paymentItemRepo.FindAsync(pi =>
                !pi.IsDeleted
                && ((pi.PurchaseOrderId != null && pi.PurchaseOrderId == orderId)
                    || (pi.PurchaseOrderItemId != null && poItemIds.Contains(pi.PurchaseOrderItemId.Trim())))))
            .ToList();
        var paymentIds = paymentItems
            .Select(pi => pi.FinancePaymentId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (paymentIds.Count == 0)
            return new List<string>();

        var payments = (await _paymentRepo.FindAsync(p => !p.IsDeleted && paymentIds.Contains(p.Id))).ToList();
        return payments
            .Where(p => p.Status != FinancePaymentStatusCancelled && p.Status != FinancePaymentStatusAuditFailed)
            .Select(p => string.IsNullOrWhiteSpace(p.FinancePaymentCode) ? p.Id : p.FinancePaymentCode.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();
    }

    private async Task<List<string>> CollectPostedPurchaseStockInLabelsAsync(
        IReadOnlyList<StockInNotify> notices,
        HashSet<string> poItemIds)
    {
        var stockInIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var noticeIds = notices
            .Select(n => n.Id.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (noticeIds.Count > 0)
        {
            foreach (var s in await _stockInRepo.FindAsync(s =>
                         !s.IsDeleted && s.SourceId != null && noticeIds.Contains(s.SourceId.Trim())))
                stockInIds.Add(s.Id.Trim());
        }

        if (poItemIds.Count > 0)
        {
            var extends = (await _stockInItemExtendRepo.FindAsync(e =>
                    !e.IsDeleted
                    && e.PurchaseOrderItemId != null
                    && poItemIds.Contains(e.PurchaseOrderItemId.Trim())))
                .ToList();
            foreach (var e in extends)
            {
                if (!string.IsNullOrWhiteSpace(e.StockInId))
                    stockInIds.Add(e.StockInId.Trim());
            }
        }

        if (stockInIds.Count == 0)
            return new List<string>();

        var stockIns = (await _stockInRepo.FindAsync(s => !s.IsDeleted && stockInIds.Contains(s.Id))).ToList();
        return stockIns
            .Where(s => s.Status == StockInHeaderStatusCode.Posted && StockInTypeCode.IsPurchaseReceipt(s.StockInType))
            .Select(s => string.IsNullOrWhiteSpace(s.StockInCode) ? s.Id : s.StockInCode.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();
    }

    private async Task<List<string>> CollectInvoiceLabelsAsync(
        string? purchaseOrderCode,
        IReadOnlyList<StockInNotify> notices)
    {
        var invoiceIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(purchaseOrderCode))
        {
            var code = purchaseOrderCode.Trim();
            foreach (var row in await _purchaseInvoiceItemRepo.FindAsync(i =>
                         !i.IsDeleted && i.PurchaseOrderCode != null && i.PurchaseOrderCode == code))
                invoiceIdSet.Add(row.FinancePurchaseInvoiceId.Trim());
        }

        var stockInIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var noticeIds = notices
            .Select(n => n.Id.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (noticeIds.Count > 0)
        {
            foreach (var s in await _stockInRepo.FindAsync(s =>
                         !s.IsDeleted && s.SourceId != null && noticeIds.Contains(s.SourceId.Trim())))
                stockInIds.Add(s.Id.Trim());
        }

        if (stockInIds.Count > 0)
        {
            foreach (var row in await _purchaseInvoiceItemRepo.FindAsync(i =>
                         !i.IsDeleted && i.StockInId != null && stockInIds.Contains(i.StockInId.Trim())))
                invoiceIdSet.Add(row.FinancePurchaseInvoiceId.Trim());
        }

        if (invoiceIdSet.Count == 0)
            return new List<string>();

        var invoices = (await _purchaseInvoiceRepo.FindAsync(inv => !inv.IsDeleted && invoiceIdSet.Contains(inv.Id)))
            .ToList();
        return invoices
            .Select(inv =>
            {
                if (!string.IsNullOrWhiteSpace(inv.InvoiceNo))
                    return inv.InvoiceNo.Trim();
                if (!string.IsNullOrWhiteSpace(inv.InvoiceCode))
                    return inv.InvoiceCode.Trim();
                return inv.Id;
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();
    }
}
