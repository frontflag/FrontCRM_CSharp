using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;

namespace CRM.Core.Services;

/// <inheritdoc />
public class PurchaseOrderItemExtendSyncService : IPurchaseOrderItemExtendSyncService
{
    private const short PoItemCancelled = -2;
    private const short PoOrderCancelled = -2;
    private const short PoItemConfirmed = 30;
    private const short PoItemCompleted = 100;
    private const short FinancePaymentCancelled = -2;
    private const short FinancePaymentAuditFailed = -1;
    private const short StockInCancelled = 3;
    private const short StockInCompleted = 2;
    private const short StockInTypePurchase = 1;

    /// <summary>0=待 1=部分 2=完成</summary>
    private const short ProgressPending = 0;

    private const short ProgressPartial = 1;
    private const short ProgressComplete = 2;

    private readonly IRepository<PurchaseOrderItem> _poItemRepo;
    private readonly IRepository<PurchaseOrder> _poRepo;
    private readonly IRepository<PurchaseOrderItemExtend> _extendRepo;
    private readonly IRepository<StockInNotify> _notifyRepo;
    private readonly IRepository<FinancePaymentItem> _payItemRepo;
    private readonly IRepository<FinancePayment> _paymentRepo;
    private readonly IRepository<FinancePurchaseInvoiceItem> _purInvItemRepo;
    private readonly IRepository<FinancePurchaseInvoice> _purInvRepo;
    private readonly IRepository<StockIn> _stockInRepo;
    private readonly IRepository<StockInItem> _stockInItemRepo;
    private readonly IRepository<QCInfo> _qcRepo;
    private readonly IUnitOfWork? _unitOfWork;
    private readonly ISellOrderItemExtendSyncService _sellSoItemExtendSync;

    public PurchaseOrderItemExtendSyncService(
        IRepository<PurchaseOrderItem> poItemRepo,
        IRepository<PurchaseOrder> poRepo,
        IRepository<PurchaseOrderItemExtend> extendRepo,
        IRepository<StockInNotify> notifyRepo,
        IRepository<FinancePaymentItem> payItemRepo,
        IRepository<FinancePayment> paymentRepo,
        IRepository<FinancePurchaseInvoiceItem> purInvItemRepo,
        IRepository<FinancePurchaseInvoice> purInvRepo,
        IRepository<StockIn> stockInRepo,
        IRepository<StockInItem> stockInItemRepo,
        IRepository<QCInfo> qcRepo,
        ISellOrderItemExtendSyncService sellSoItemExtendSync,
        IUnitOfWork? unitOfWork = null)
    {
        _poItemRepo = poItemRepo;
        _poRepo = poRepo;
        _extendRepo = extendRepo;
        _notifyRepo = notifyRepo;
        _payItemRepo = payItemRepo;
        _paymentRepo = paymentRepo;
        _purInvItemRepo = purInvItemRepo;
        _purInvRepo = purInvRepo;
        _stockInRepo = stockInRepo;
        _stockInItemRepo = stockInItemRepo;
        _qcRepo = qcRepo;
        _sellSoItemExtendSync = sellSoItemExtendSync;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task RecalculateAsync(string purchaseOrderItemId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(purchaseOrderItemId)) return;

        var id = purchaseOrderItemId.Trim();
        var poItem = await _poItemRepo.GetByIdAsync(id);
        if (poItem == null) return;

        var poHeader = await _poRepo.GetByIdAsync(poItem.PurchaseOrderId);
        var poCancelled = poHeader?.Status == PoOrderCancelled;

        var ext = await _extendRepo.GetByIdAsync(id);
        if (ext == null)
        {
            var lineTotal = Math.Round(poItem.Qty * poItem.Cost, 2, MidpointRounding.AwayFromZero);
            ext = new PurchaseOrderItemExtend
            {
                Id = poItem.Id,
                QtyStockInNotifyNot = poItem.Qty,
                PurchaseInvoiceAmount = lineTotal,
                PurchaseInvoiceToBe = lineTotal,
                PaymentAmount = lineTotal,
                PaymentAmountNot = lineTotal,
                CreateTime = DateTime.UtcNow
            };
            await _extendRepo.AddAsync(ext);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        var lineCancelled = poItem.Status == PoItemCancelled;

        // --- 到货通知余量（与 Logistics 公式一致；与「有效入库数量」解耦）---
        var lines = (await _notifyRepo.FindAsync(x => x.PurchaseOrderItemId == id)).ToList();
        var sumReceiveNotify = lines.Sum(x => x.ReceiveQty);
        var inTransit = lines.Sum(x => Math.Max(0m, x.ExpectQty - x.ReceiveQty));
        ext.QtyStockInNotifyExpectSum = lines.Sum(x => x.ExpectQty);
        ext.QtyStockInNotifyNot = Math.Max(0m, poItem.Qty - sumReceiveNotify - inTransit);

        // --- 入库数量：已入库的采购入库单头表 PurchaseOrderItemId 与本行一致，按单头 TotalQuantity 累计 ---
        var stockInCompletedQty = await SumCompletedPurchaseStockInQtyForPoLineAsync(poItem.Id, cancellationToken);
        ext.QtyReceiveTotal = stockInCompletedQty;

        var invalidPurchaseLine = lineCancelled || poCancelled;

        // --- 采购进度（本行有效采购量 + 状态；明细或主单取消则数量 0、状态待采购）---
        if (invalidPurchaseLine)
        {
            ext.PurchaseProgressQty = 0m;
            ext.PurchaseProgressStatus = ProgressPending;
        }
        else
        {
            ext.PurchaseProgressQty = poItem.Qty;
            ext.PurchaseProgressStatus = poItem.Status >= PoItemCompleted
                ? ProgressComplete
                : poItem.Status >= PoItemConfirmed
                    ? ProgressPartial
                    : ProgressPending;
        }

        // --- 入库进度（相对本行有效采购数量）---
        var expectQty = invalidPurchaseLine ? 0m : poItem.Qty;
        if (expectQty <= 0m)
            ext.StockInProgressStatus = ProgressComplete;
        else if (stockInCompletedQty <= 0m)
            ext.StockInProgressStatus = ProgressPending;
        else if (stockInCompletedQty + 1e-9m >= expectQty)
            ext.StockInProgressStatus = ProgressComplete;
        else
            ext.StockInProgressStatus = ProgressPartial;

        // --- 付款：排除已取消、审核失败的付款单 ---
        var payItems = (await _payItemRepo.FindAsync(p =>
                p.PurchaseOrderItemId != null && p.PurchaseOrderItemId == id))
            .ToList();
        var paymentIds = payItems.Select(p => p.FinancePaymentId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var payments = paymentIds.Count == 0
            ? new List<FinancePayment>()
            : (await _paymentRepo.FindAsync(x => paymentIds.Contains(x.Id))).ToList();
        var validPaymentIds = payments
            .Where(p => p.Status != FinancePaymentCancelled && p.Status != FinancePaymentAuditFailed)
            .Select(p => p.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var payDone = payItems.Where(pi => validPaymentIds.Contains(pi.FinancePaymentId)).Sum(pi => pi.VerificationDone);
        ext.PaymentAmountFinish = Math.Round(payDone, 2, MidpointRounding.AwayFromZero);
        ext.PaymentAmountNot = Math.Max(0m, Math.Round(ext.PaymentAmount - ext.PaymentAmountFinish, 2, MidpointRounding.AwayFromZero));
        if (ext.PaymentAmountFinish <= 0m)
            ext.PaymentProgressStatus = ProgressPending;
        else if (ext.PaymentAmount > 0m && ext.PaymentAmountFinish + 0.0001m >= ext.PaymentAmount)
            ext.PaymentProgressStatus = ProgressComplete;
        else
            ext.PaymentProgressStatus = ProgressPartial;

        // --- 进项发票：按入库单 + 质检/采购来源解析到本行 ---
        var invDone = await SumPurchaseInvoiceBillAmountForPoLineAsync(poItem);
        ext.PurchaseInvoiceDone = Math.Round(invDone, 2, MidpointRounding.AwayFromZero);
        ext.PurchaseInvoiceToBe = Math.Max(0m, Math.Round(ext.PurchaseInvoiceAmount - ext.PurchaseInvoiceDone, 2, MidpointRounding.AwayFromZero));
        if (ext.PurchaseInvoiceDone <= 0m)
            ext.InvoiceProgressStatus = ProgressPending;
        else if (ext.PurchaseInvoiceAmount > 0m && ext.PurchaseInvoiceDone + 0.0001m >= ext.PurchaseInvoiceAmount)
            ext.InvoiceProgressStatus = ProgressComplete;
        else
            ext.InvoiceProgressStatus = ProgressPartial;

        ext.ModifyTime = DateTime.UtcNow;
        await _extendRepo.UpdateAsync(ext);

        if (!string.IsNullOrWhiteSpace(poItem.SellOrderItemId))
            await _sellSoItemExtendSync.RecalculateAsync(poItem.SellOrderItemId.Trim(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task RecalculateForFinancePurchaseInvoiceAsync(string financePurchaseInvoiceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(financePurchaseInvoiceId)) return;
        var invId = financePurchaseInvoiceId.Trim();
        var items = (await _purInvItemRepo.FindAsync(x => x.FinancePurchaseInvoiceId == invId)).ToList();
        var poItemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var it in items)
        {
            foreach (var pid in await ResolvePoItemIdsFromInvoiceItemAsync(it))
                poItemIds.Add(pid);
        }

        foreach (var pid in poItemIds)
            await RecalculateAsync(pid, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ResolvePurchaseOrderItemIdsForFinancePurchaseInvoiceAsync(
        string financePurchaseInvoiceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(financePurchaseInvoiceId))
            return Array.Empty<string>();
        var invId = financePurchaseInvoiceId.Trim();
        var items = (await _purInvItemRepo.FindAsync(x => x.FinancePurchaseInvoiceId == invId)).ToList();
        var poItemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var it in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var pid in await ResolvePoItemIdsFromInvoiceItemAsync(it))
                poItemIds.Add(pid);
        }

        return poItemIds.ToList();
    }

    /// <summary>
    /// 有效入库数量：状态为已入库的采购入库单头 <see cref="StockIn.PurchaseOrderItemId"/> 等于本采购明细主键时，累计单头 <see cref="StockIn.TotalQuantity"/>。
    /// </summary>
    private async Task<decimal> SumCompletedPurchaseStockInQtyForPoLineAsync(
        string purchaseOrderItemId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(purchaseOrderItemId)) return 0m;
        var poItemId = purchaseOrderItemId.Trim();
        var list = (await _stockInRepo.FindAsync(s =>
                s.Status == StockInCompleted
                && s.StockInType == StockInTypePurchase
                && s.PurchaseOrderItemId != null
                && s.PurchaseOrderItemId == poItemId))
            .ToList();
        return list.Sum(s => s.TotalQuantity);
    }

    private async Task<decimal> SumPurchaseInvoiceBillAmountForPoLineAsync(PurchaseOrderItem poItem)
    {
        var invItems = (await _purInvItemRepo.GetAllAsync())
            .Where(i => !string.IsNullOrWhiteSpace(i.StockInId))
            .ToList();
        if (invItems.Count == 0) return 0m;

        var stockInIds = invItems.Select(i => i.StockInId!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var stockIns = (await _stockInRepo.GetAllAsync())
            .Where(s => stockInIds.Contains(s.Id))
            .ToDictionary(s => s.Id, s => s, StringComparer.OrdinalIgnoreCase);

        var invoices = (await _purInvRepo.GetAllAsync()).ToDictionary(i => i.Id, i => i, StringComparer.OrdinalIgnoreCase);
        var qcs = (await _qcRepo.GetAllAsync()).ToDictionary(q => q.Id, q => q, StringComparer.OrdinalIgnoreCase);
        var notifies = (await _notifyRepo.GetAllAsync()).ToDictionary(n => n.Id, n => n, StringComparer.OrdinalIgnoreCase);
        var allSiItems = (await _stockInItemRepo.GetAllAsync()).ToList();
        var siItemsByStockIn = allSiItems.GroupBy(x => x.StockInId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        decimal sum = 0m;
        foreach (var ii in invItems)
        {
            if (!invoices.TryGetValue(ii.FinancePurchaseInvoiceId, out var inv) || inv.RedInvoiceStatus == 1)
                continue;
            if (!stockIns.TryGetValue(ii.StockInId!.Trim(), out var si) || si.Status == StockInCancelled)
                continue;
            if (!InvoiceItemTouchesPoLine(si, poItem, qcs, notifies, siItemsByStockIn))
                continue;
            sum += ii.BillAmount;
        }

        return sum;
    }

    private static bool StockInItemsMatchPoLine(StockIn si, PurchaseOrderItem poItem, IReadOnlyDictionary<string, List<StockInItem>> siItemsByStockIn)
    {
        if (!siItemsByStockIn.TryGetValue(si.Id, out var sil) || sil.Count == 0)
            return false;
        foreach (var line in sil)
        {
            var mid = line.MaterialId?.Trim();
            if (string.IsNullOrEmpty(mid)) continue;
            if (string.Equals(mid, poItem.Id, StringComparison.OrdinalIgnoreCase))
                return true;
            if (!string.IsNullOrWhiteSpace(poItem.ProductId) &&
                string.Equals(mid, poItem.ProductId.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;
            if (!string.IsNullOrWhiteSpace(poItem.PN) &&
                string.Equals(mid, poItem.PN.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool InvoiceItemTouchesPoLine(
        StockIn si,
        PurchaseOrderItem poItem,
        IReadOnlyDictionary<string, QCInfo> qcsById,
        IReadOnlyDictionary<string, StockInNotify> notifyById,
        IReadOnlyDictionary<string, List<StockInItem>> siItemsByStockIn)
    {
        if (!string.IsNullOrWhiteSpace(si.PurchaseOrderItemId) &&
            string.Equals(si.PurchaseOrderItemId.Trim(), poItem.Id.Trim(), StringComparison.OrdinalIgnoreCase))
            return StockInItemsMatchPoLine(si, poItem, siItemsByStockIn);

        if (!string.IsNullOrWhiteSpace(si.SellOrderItemId) && !string.IsNullOrWhiteSpace(poItem.SellOrderItemId) &&
            string.Equals(si.SellOrderItemId.Trim(), poItem.SellOrderItemId.Trim(), StringComparison.OrdinalIgnoreCase))
            return StockInItemsMatchPoLine(si, poItem, siItemsByStockIn);

        foreach (var qc in qcsById.Values)
        {
            var qSid = qc.StockInId?.Trim();
            if (string.IsNullOrEmpty(qSid) ||
                !string.Equals(qSid, si.Id.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.IsNullOrWhiteSpace(qc.StockInNotifyId) &&
                notifyById.TryGetValue(qc.StockInNotifyId.Trim(), out var n) &&
                string.Equals(n.PurchaseOrderItemId.Trim(), poItem.Id.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private async Task<List<string>> ResolvePoItemIdsFromInvoiceItemAsync(FinancePurchaseInvoiceItem item)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(item.StockInId)) return result.ToList();

        var si = await _stockInRepo.GetByIdAsync(item.StockInId.Trim());
        if (si == null) return result.ToList();

        if (!string.IsNullOrWhiteSpace(si.PurchaseOrderItemId))
        {
            result.Add(si.PurchaseOrderItemId.Trim());
            return result.ToList();
        }

        var qcLinked = (await _qcRepo.FindAsync(q => q.StockInId == si.Id)).FirstOrDefault();
        if (qcLinked != null)
        {
            var notice = await _notifyRepo.GetByIdAsync(qcLinked.StockInNotifyId);
            if (notice != null && !string.IsNullOrWhiteSpace(notice.PurchaseOrderItemId))
            {
                result.Add(notice.PurchaseOrderItemId.Trim());
                return result.ToList();
            }
        }

        if (!string.IsNullOrWhiteSpace(si.SellOrderItemId))
        {
            var lines = (await _poItemRepo.FindAsync(x => x.SellOrderItemId == si.SellOrderItemId.Trim())).ToList();
            var sil = (await _stockInItemRepo.FindAsync(x => x.StockInId == si.Id)).ToList();
            foreach (var poLine in lines)
            {
                var oneSiMap = new Dictionary<string, List<StockInItem>>(StringComparer.OrdinalIgnoreCase)
                {
                    [si.Id] = sil
                };
                if (StockInItemsMatchPoLine(si, poLine, oneSiMap))
                    result.Add(poLine.Id);
            }
        }

        return result.ToList();
    }
}
