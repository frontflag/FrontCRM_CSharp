using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services;

/// <inheritdoc />
public class SellOrderItemExtendSyncService : ISellOrderItemExtendSyncService
{
    private const short PoItemStatusConfirmed = 30;
    private const short StockInCompleted = 2;
    /// <summary>已出库</summary>
    private const short StockOutCompleted = 2;
    /// <summary>已完成（列表「标记完成」）</summary>
    private const short StockOutFinished = 4;

    /// <summary>0=待 1=部分 2=完成</summary>
    private const short ProgressPending = 0;

    private const short ProgressPartial = 1;
    private const short ProgressComplete = 2;

    private readonly IRepository<SellOrderItem> _soItemRepo;
    private readonly IRepository<SellOrderItemExtend> _extendRepo;
    private readonly IRepository<PurchaseOrderItem> _poItemRepo;
    private readonly IRepository<StockIn> _stockInRepo;
    private readonly IRepository<StockInItemExtend> _stockInItemExtendRepo;
    private readonly IRepository<StockInItem> _stockInItemRepo;
    private readonly IRepository<StockOutRequest> _stockOutRequestRepo;
    private readonly IRepository<StockOut> _stockOutRepo;
    private readonly IRepository<StockOutItem> _stockOutItemRepo;
    private readonly IRepository<StockOutItemExtend> _stockOutItemExtendRepo;
    private readonly IRepository<FinanceReceivable> _receivableRepo;
    private readonly ISellOrderMainStatusSyncService _mainStatusSync;
    private readonly ILogger<SellOrderItemExtendSyncService> _logger;

    public SellOrderItemExtendSyncService(
        IRepository<SellOrderItem> soItemRepo,
        IRepository<SellOrderItemExtend> extendRepo,
        IRepository<PurchaseOrderItem> poItemRepo,
        IRepository<StockIn> stockInRepo,
        IRepository<StockInItemExtend> stockInItemExtendRepo,
        IRepository<StockInItem> stockInItemRepo,
        IRepository<StockOutRequest> stockOutRequestRepo,
        IRepository<StockOut> stockOutRepo,
        IRepository<StockOutItem> stockOutItemRepo,
        IRepository<StockOutItemExtend> stockOutItemExtendRepo,
        IRepository<FinanceReceivable> receivableRepo,
        ISellOrderMainStatusSyncService mainStatusSync,
        ILogger<SellOrderItemExtendSyncService> logger)
    {
        _soItemRepo = soItemRepo;
        _extendRepo = extendRepo;
        _poItemRepo = poItemRepo;
        _stockInRepo = stockInRepo;
        _stockInItemExtendRepo = stockInItemExtendRepo;
        _stockInItemRepo = stockInItemRepo;
        _stockOutRequestRepo = stockOutRequestRepo;
        _stockOutRepo = stockOutRepo;
        _stockOutItemRepo = stockOutItemRepo;
        _stockOutItemExtendRepo = stockOutItemExtendRepo;
        _receivableRepo = receivableRepo;
        _mainStatusSync = mainStatusSync;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RecalculateAsync(string sellOrderItemId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sellOrderItemId))
        {
            _logger.LogWarning("[SellLineStockOutSync] Recalculate skipped: SellOrderItemId empty");
            return;
        }

        var id = sellOrderItemId.Trim();
        _logger.LogInformation("[SellLineStockOutSync] Recalculate begin SellOrderItemId={SellOrderItemId}", id);

        var soItem = await _soItemRepo.GetByIdAsync(id);
        if (soItem == null)
        {
            _logger.LogWarning("[SellLineStockOutSync] Recalculate skipped: sellorderitem not found SellOrderItemId={SellOrderItemId}", id);
            return;
        }

        var ext = await _extendRepo.GetByIdAsync(id);
        if (ext == null)
        {
            _logger.LogWarning(
                "[SellLineStockOutSync] Recalculate skipped: sellorderitemextend row missing (1:1 with line) SellOrderItemId={SellOrderItemId}",
                id);
            return;
        }

        var poItems = (await _poItemRepo.FindAsync(p => p.SellOrderItemId == id))
            .ToList();
        var purchasedQty = poItems.Sum(p => p.Qty);
        soItem.PurchasedQty = purchasedQty;
        await _soItemRepo.UpdateAsync(soItem);

        ext.QtyAlreadyPurchased = purchasedQty;
        ext.QtyNotPurchase = Math.Max(0m, soItem.Qty - purchasedQty);

        var lineAmountTotal = Math.Round(soItem.Qty * soItem.Price, 2, MidpointRounding.AwayFromZero);
        ext.ReceiptAmount = lineAmountTotal;
        ext.InvoiceAmount = lineAmountTotal;
        ext.PaymentAmountToBe = lineAmountTotal;

        // 实出数量 / 出库进度：按出库明细扩展表 sell_order_item_id 归属本行累计（多行出库单不可用头表 TotalQuantity）
        var (sumStockOut, completedStockOuts) = await SumCompletedSalesStockOutQtyForLineAsync(id);
        ext.QtyStockOutActual = sumStockOut;

        // --- 销售数量变更后：校验；单条有效出库通知仅收缩超量，不将部分通知扩成整单（多条仅校验）---
        await AlignStockOutRequestsWithSoLineQtyAsync(soItem, sumStockOut, cancellationToken);

        var requests = (await _stockOutRequestRepo.FindAsync(r => r.SalesOrderItemId == id))
            .ToList();
        var notifySum = requests
            .Where(r => StockOutRequestStatusCode.IsCountedForSalesLineNotifyQuantity(r.Status, r.StockOutType))
            .Sum(r => r.Quantity);
        ext.QtyStockOutNotify = notifySum;
        ext.QtyStockOutNotifyNot = Math.Max(0m, soItem.Qty - notifySum);

        if (completedStockOuts.Count > 0)
        {
            var detail = string.Join(", ", completedStockOuts.Select(o =>
                $"{o.StockOutCode}(id={o.Id},st={o.Status},qty={o.TotalQuantity})"));
            _logger.LogInformation(
                "[SellLineStockOutSync] Matched stockout headers for line SellOrderItemId={SellOrderItemId} Count={Count} SumTotalQty={SumTotalQty} Detail=[{Detail}]",
                id, completedStockOuts.Count, sumStockOut, detail);
        }
        else
        {
            _logger.LogInformation(
                "[SellLineStockOutSync] No qualifying stockout rows (type=1, status 2|4, SellOrderItemId match) SellOrderItemId={SellOrderItemId}",
                id);
        }

        var receivables = (await _receivableRepo.FindAsync(r =>
                r.SellOrderItemId == id && !r.IsDeleted))
            .ToList();
        var verifiedSum = receivables.Sum(r => r.VerifiedDone);
        ext.ReceiptAmountFinish = verifiedSum;
        ext.ReceiptAmountNot = Math.Max(0m, Math.Round(lineAmountTotal - verifiedSum, 2, MidpointRounding.AwayFromZero));
        ext.InvoiceAmountNot = Math.Max(0m, Math.Round(lineAmountTotal - ext.InvoiceAmountFinish, 2, MidpointRounding.AwayFromZero));

        var qtyLine = soItem.Qty;
        if (ext.QtyAlreadyPurchased <= 0m)
            ext.PurchaseProgressStatus = ProgressPending;
        else if (ext.QtyAlreadyPurchased + 1e-9m >= qtyLine)
            ext.PurchaseProgressStatus = ProgressComplete;
        else
            ext.PurchaseProgressStatus = ProgressPartial;

        // 入库进度：stockinitemextend.sell_order_item_id = 本销售明细，父单已入库采购入库时累计对应明细行数量
        var extMatches = (await _stockInItemExtendRepo.FindAsync(x => x.SellOrderItemId != null && x.SellOrderItemId == id))
            .ToList();
        decimal sumReceive = 0m;
        if (extMatches.Count > 0)
        {
            var itemIds = extMatches.Select(x => x.Id).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var siItems = (await _stockInItemRepo.FindAsync(x => itemIds.Contains(x.Id))).ToList();
            var siIds = siItems.Select(x => x.StockInId).Distinct().ToList();
            var completedSiIds = (await _stockInRepo.FindAsync(s =>
                    siIds.Contains(s.Id)
                    && s.Status == StockInCompleted
                    && s.StockInType == StockInTypeCode.Purchase))
                .Select(s => s.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            sumReceive = siItems.Where(i => completedSiIds.Contains(i.StockInId)).Sum(i => (decimal)i.Quantity);
        }

        if (sumReceive <= 0m)
            ext.StockInProgressStatus = ProgressPending;
        else if (sumReceive + 1e-9m >= qtyLine)
            ext.StockInProgressStatus = ProgressComplete;
        else
            ext.StockInProgressStatus = ProgressPartial;

        if (sumStockOut <= 0m)
            ext.StockOutProgressStatus = ProgressPending;
        else if (sumStockOut + 1e-9m >= qtyLine)
            ext.StockOutProgressStatus = ProgressComplete;
        else
            ext.StockOutProgressStatus = ProgressPartial;

        if (ext.ReceiptAmountFinish <= 0m)
            ext.ReceiptProgressStatus = ProgressPending;
        else if (ext.ReceiptAmount > 0m && ext.ReceiptAmountFinish + 0.0001m >= ext.ReceiptAmount)
            ext.ReceiptProgressStatus = ProgressComplete;
        else
            ext.ReceiptProgressStatus = ProgressPartial;

        if (ext.InvoiceAmountFinish <= 0m)
            ext.InvoiceProgressStatus = ProgressPending;
        else if (ext.InvoiceAmount > 0m && ext.InvoiceAmountFinish + 0.0001m >= ext.InvoiceAmount)
            ext.InvoiceProgressStatus = ProgressComplete;
        else
            ext.InvoiceProgressStatus = ProgressPartial;

        var outboundCostLines = await LoadOutboundCostLinesAsync(id, completedStockOuts);
        ApplyProfitFields(soItem, ext, poItems, outboundCostLines);

        ext.ModifyTime = DateTime.UtcNow;
        await _extendRepo.UpdateAsync(ext);

        _logger.LogInformation(
            "[SellLineStockOutSync] Recalculate done SellOrderItemId={SellOrderItemId} LineQty={LineQty} QtyStockOutActual={QtyStockOutActual} StockOutProgressStatus={StockOutProgressStatus} (0=待 1=部分 2=完成)",
            id, qtyLine, ext.QtyStockOutActual, ext.StockOutProgressStatus);

        if (!string.IsNullOrWhiteSpace(soItem.SellOrderId))
            await _mainStatusSync.TrySyncOrderMainStatusAsync(soItem.SellOrderId, cancellationToken);
    }

    private async Task AlignStockOutRequestsWithSoLineQtyAsync(
        SellOrderItem soItem,
        decimal sumStockOutActual,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var requests = (await _stockOutRequestRepo.FindAsync(r => r.SalesOrderItemId == soItem.Id)).ToList();
        if (requests.Count == 0) return;

        var active = requests
            .Where(r => StockOutRequestStatusCode.IsCountedForSalesLineNotifyQuantity(r.Status, r.StockOutType))
            .ToList();

        if (active.Count == 1)
        {
            var request = active[0];
            if (request.Status != StockOutRequestStatusCode.StockedOut)
            {
                var targetQty = InventoryQuantity.RoundFromDecimal(soItem.Qty);
                // 分批出库：部分通知数量有意小于销售行数量，刷新/重算时不得自动扩成整单。
                if (request.Quantity > targetQty)
                {
                    request.Quantity = targetQty;
                    request.ModifyTime = DateTime.UtcNow;
                    await _stockOutRequestRepo.UpdateAsync(request);
                }
            }
        }

        var activeNotifySum = active.Sum(r => (decimal)r.Quantity);
        if (soItem.Qty + 1e-9m < activeNotifySum)
            throw new InvalidOperationException(
                $"销售数量不能小于有效出库通知数量合计（已通知 {activeNotifySum}）");
        if (soItem.Qty + 1e-9m < sumStockOutActual)
            throw new InvalidOperationException(
                $"销售数量不能小于已实际出库数量（{sumStockOutActual}）");
    }

    private static void ApplyProfitFields(
        SellOrderItem soItem,
        SellOrderItemExtend ext,
        List<PurchaseOrderItem> poItems,
        IReadOnlyList<SellOrderOutboundCostLine> outboundCostLines)
    {
        var revUsdNow = Math.Round(soItem.Qty * soItem.ConvertPrice, 2, MidpointRounding.AwayFromZero);
        var quoteCostUsdLine = Math.Round(soItem.Qty * ext.QuoteConvertCost, 2, MidpointRounding.AwayFromZero);
        if (ext.QuoteConvertCost > 0m)
        {
            ext.ReQuoteProfitExpected = Math.Round(revUsdNow - quoteCostUsdLine, 2, MidpointRounding.AwayFromZero);
            ext.ReQuoteProfitRateExpected = quoteCostUsdLine > 0m
                ? Math.Round(revUsdNow / quoteCostUsdLine, 6, MidpointRounding.AwayFromZero)
                : 0m;
        }
        else
        {
            ext.ReQuoteProfitExpected = 0m;
            ext.ReQuoteProfitRateExpected = 0m;
        }

        var poCostTotal = Math.Round(poItems.Sum(p => p.Qty * p.ConvertPrice), 2, MidpointRounding.AwayFromZero);
        ext.PoCostUsdTotal = poCostTotal;
        ext.PurchaseProfitExpected = Math.Round(revUsdNow - poCostTotal, 2, MidpointRounding.AwayFromZero);
        ext.PurchaseProfitRateExpected = poCostTotal > 0m
            ? Math.Round(revUsdNow / poCostTotal, 6, MidpointRounding.AwayFromZero)
            : 0m;

        var confirmedItems = poItems.Where(p => p.Status >= PoItemStatusConfirmed).ToList();
        var poCostConfirmed = Math.Round(confirmedItems.Sum(p => p.Qty * p.ConvertPrice), 2, MidpointRounding.AwayFromZero);
        ext.PoCostUsdConfirmed = poCostConfirmed;
        ext.SalesProfitExpected = Math.Round(revUsdNow - poCostConfirmed, 2, MidpointRounding.AwayFromZero);

        var sumPoQty = poItems.Sum(p => p.Qty);
        var avgCostUsd = sumPoQty > 0m
            ? poItems.Sum(p => p.Qty * p.ConvertPrice) / sumPoQty
            : 0m;
        var outQty = ext.QtyStockOutActual;
        var revOut = Math.Round(outQty * soItem.ConvertPrice, 2, MidpointRounding.AwayFromZero);
        var outboundSnapshot = SellOrderOutboundProfitCalc.Compute(revOut, outQty, outboundCostLines, avgCostUsd);
        ext.ProfitOutBizUsd = outboundSnapshot.ProfitOutBizUsd;
        ext.ProfitOutRateBiz = outboundSnapshot.ProfitOutRateBiz;
        // 财务 USD：出库时点汇率与加权成本方案未接入前，与业务 USD 同口径写入
        ext.ProfitOutFinUsd = ext.ProfitOutBizUsd;
        ext.ProfitOutRateFin = ext.ProfitOutRateBiz;
    }

    /// <summary>
    /// 已完成销售出库数量：按明细扩展 <c>sell_order_item_id</c> 归属累计。
    /// 多行出库单头 <c>TotalQuantity</c> / 头表 <c>SellOrderItemId</c> 不能代表单行实出。
    /// </summary>
    private async Task<(decimal SumQty, List<StockOut> Headers)> SumCompletedSalesStockOutQtyForLineAsync(
        string sellOrderItemId)
    {
        var lineExtends = (await _stockOutItemExtendRepo.FindAsync(e =>
                !e.IsDeleted
                && e.SellOrderItemId != null
                && e.SellOrderItemId == sellOrderItemId))
            .ToList();
        if (lineExtends.Count == 0)
            return (0m, new List<StockOut>());

        var extendByItemId = lineExtends
            .Where(e => !string.IsNullOrWhiteSpace(e.Id))
            .GroupBy(e => e.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var itemIds = extendByItemId.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var outItems = (await _stockOutItemRepo.FindAsync(i =>
                !i.IsDeleted && itemIds.Contains(i.Id)))
            .ToList();
        if (outItems.Count == 0)
            return (0m, new List<StockOut>());

        var stockOutIds = outItems
            .Select(i => i.StockOutId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var completedHeaders = (await _stockOutRepo.FindAsync(o =>
                stockOutIds.Contains(o.Id)
                && (o.Status == StockOutCompleted || o.Status == StockOutFinished)
                && StockOutTypeCode.IsSalesStockOut(o.StockOutType)))
            .ToList();
        var completedIdSet = completedHeaders
            .Select(o => o.Id.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        decimal sum = 0m;
        foreach (var item in outItems)
        {
            if (!completedIdSet.Contains(item.StockOutId.Trim()))
                continue;
            if (!extendByItemId.TryGetValue(item.Id.Trim(), out var extRow))
                continue;
            var qty = extRow.QtyStockOut > 0
                ? extRow.QtyStockOut
                : (item.ActualQty > 0 ? item.ActualQty : item.Quantity);
            if (qty > 0)
                sum += qty;
        }

        return (sum, completedHeaders);
    }

    private async Task<List<SellOrderOutboundCostLine>> LoadOutboundCostLinesAsync(
        string sellOrderItemId,
        IReadOnlyList<StockOut> completedStockOuts)
    {
        if (completedStockOuts.Count == 0)
            return new List<SellOrderOutboundCostLine>();

        var outIds = completedStockOuts
            .Select(o => o.Id.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (outIds.Count == 0)
            return new List<SellOrderOutboundCostLine>();

        var outItems = (await _stockOutItemRepo.FindAsync(i =>
                !i.IsDeleted && outIds.Contains(i.StockOutId)))
            .ToList();
        if (outItems.Count == 0)
            return new List<SellOrderOutboundCostLine>();

        var qtyByItemId = outItems.ToDictionary(
            i => i.Id.Trim(),
            i => i.ActualQty > 0 ? i.ActualQty : i.Quantity,
            StringComparer.OrdinalIgnoreCase);
        var itemIds = qtyByItemId.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var lineId = sellOrderItemId.Trim();
        var extends = (await _stockOutItemExtendRepo.FindAsync(e =>
                !e.IsDeleted
                && itemIds.Contains(e.Id)
                && e.SellOrderItemId != null
                && e.SellOrderItemId == lineId))
            .ToList();

        var lines = new List<SellOrderOutboundCostLine>(extends.Count);
        foreach (var e in extends)
        {
            var itemId = e.Id.Trim();
            var qty = e.QtyStockOut > 0 ? e.QtyStockOut : qtyByItemId.GetValueOrDefault(itemId, 0);
            if (qty <= 0)
                continue;

            lines.Add(new SellOrderOutboundCostLine
            {
                PurchaseOrderItemId = e.PurchaseOrderItemId,
                PurchaseOrderItemCode = e.PurchaseOrderItemCode,
                PurchasePriceUsd = e.PurchasePriceUsd,
                Qty = qty,
                ProfitOutBizUsd = e.ProfitOutBizUsd
            });
        }

        return lines;
    }
}
