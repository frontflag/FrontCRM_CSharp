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
    private readonly IRepository<PackingItem>? _packingItemRepo;
    private readonly IRepository<PickingTaskItem>? _pickingTaskItemRepo;
    private readonly IRepository<PickingTask>? _pickingTaskRepo;
    private readonly IRepository<StockItem>? _stockItemRepo;
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
        ILogger<SellOrderItemExtendSyncService> logger,
        IRepository<PackingItem>? packingItemRepo = null,
        IRepository<PickingTaskItem>? pickingTaskItemRepo = null,
        IRepository<PickingTask>? pickingTaskRepo = null,
        IRepository<StockItem>? stockItemRepo = null)
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
        _packingItemRepo = packingItemRepo;
        _pickingTaskItemRepo = pickingTaskItemRepo;
        _pickingTaskRepo = pickingTaskRepo;
        _stockItemRepo = stockItemRepo;
    }

    /// <inheritdoc />
    public async Task RecalculateAsync(
        string sellOrderItemId,
        CancellationToken cancellationToken = default,
        bool enforceLineQtyOutboundGuards = true)
    {
        if (string.IsNullOrWhiteSpace(sellOrderItemId))
        {
            _logger.LogWarning("[SellLineStockOutSync] Recalculate skipped: SellOrderItemId empty");
            return;
        }

        var id = sellOrderItemId.Trim();
        _logger.LogInformation(
            "[SellLineStockOutSync] Recalculate begin SellOrderItemId={SellOrderItemId} EnforceGuards={EnforceGuards}",
            id,
            enforceLineQtyOutboundGuards);

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
        // 出库强制删除等场景传 enforceLineQtyOutboundGuards=false，仍可收缩单条超量通知，但不抛「不能小于已实出」
        await AlignStockOutRequestsWithSoLineQtyAsync(
            soItem,
            sumStockOut,
            enforceLineQtyOutboundGuards,
            cancellationToken);

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
                    && s.StockInType != StockInTypeCode.Transfer))
                .Select(s => s.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            sumReceive = siItems.Where(i => completedSiIds.Contains(i.StockInId)).Sum(i =>
            {
                if (i.Quantity > 0) return (decimal)i.Quantity;
                return i.QtyReceived > 0 ? i.QtyReceived : 0m;
            });
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
        var (stockingUsedQty, stockingPickCostUsd) = await LoadStockingPickCostAsync(id, cancellationToken);
        ApplyProfitFields(soItem, ext, poItems, outboundCostLines, stockingUsedQty, stockingPickCostUsd);

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
        bool enforceLineQtyOutboundGuards,
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

        if (!enforceLineQtyOutboundGuards)
        {
            if (soItem.Qty + 1e-9m < sumStockOutActual
                || soItem.Qty + 1e-9m < active.Sum(r => (decimal)r.Quantity))
            {
                _logger.LogWarning(
                    "[SellLineStockOutSync] Skip line-qty outbound guards SellOrderItemId={SellOrderItemId} LineQty={LineQty} ActualOut={ActualOut} NotifySum={NotifySum}",
                    soItem.Id,
                    soItem.Qty,
                    sumStockOutActual,
                    active.Sum(r => (decimal)r.Quantity));
            }
            return;
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
        IReadOnlyList<SellOrderOutboundCostLine> outboundCostLines,
        decimal stockingUsedQty = 0m,
        decimal stockingPickCostUsd = 0m)
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

        var outboundCostUsd = Math.Round(
            outboundCostLines.Sum(l => l.Qty * l.PurchasePriceUsd),
            2,
            MidpointRounding.AwayFromZero);
        var outboundQty = outboundCostLines.Sum(l => (decimal)l.Qty);
        var (stockingCovered, stockingUnit) = SellOrderSalesExpectedProfitCalc.ResolveStockingUnitCost(
            soItem.Qty,
            outboundQty,
            outboundCostUsd,
            stockingUsedQty,
            stockingPickCostUsd);
        var salesExpected = SellOrderSalesExpectedProfitCalc.Compute(
            revUsdNow,
            soItem.Qty,
            hasPoItems: poItems.Count > 0,
            poCostUsdTotal: poCostTotal,
            stockingCovered: stockingCovered,
            stockingUnitCostUsd: stockingUnit,
            quoteConvertCost: ext.QuoteConvertCost);
        ext.SalesProfitExpected = salesExpected.ProfitUsdForStorage;

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
    /// 多行出库单头 <c>TotalQuantity</c> 不能代表单行实出。
    /// 兼容历史脏数据：扩展行误写了备货/样品库存原绑定行时，若出库单头 <c>SellOrderItemId</c> 为本行，
    /// 且扩展为空绑定或库存类型为备货(2)/样品(3)，仍计入本行。
    /// </summary>
    private async Task<(decimal SumQty, List<StockOut> Headers)> SumCompletedSalesStockOutQtyForLineAsync(
        string sellOrderItemId)
    {
        const short stockTypeStocking = 2;
        const short stockTypeSample = 3;

        var lineId = sellOrderItemId.Trim();

        // A) 扩展行明确归属本销售明细
        var lineExtends = (await _stockOutItemExtendRepo.FindAsync(e =>
                !e.IsDeleted
                && e.SellOrderItemId != null
                && e.SellOrderItemId == lineId))
            .ToList();

        // B) 头表归属本行，但扩展行 SellOrderItemId 空/误挂备货原行（刷新可修复历史出库状态）
        var headerMatched = (await _stockOutRepo.FindAsync(o =>
                !o.IsDeleted
                && o.SellOrderItemId != null
                && o.SellOrderItemId == lineId
                && (o.Status == StockOutCompleted || o.Status == StockOutFinished)
                && (o.StockOutType == StockOutTypeCode.Sales
                    || o.StockOutType == StockOutTypeCode.LegacySales)))
            .ToList();
        var headerIdSet = headerMatched
            .Select(o => o.Id.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<StockOutItem> headerItems = new();
        if (headerIdSet.Count > 0)
        {
            headerItems = (await _stockOutItemRepo.FindAsync(i =>
                    !i.IsDeleted && headerIdSet.Contains(i.StockOutId)))
                .ToList();
        }

        var headerItemIds = headerItems
            .Select(i => i.Id.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var headerItemExtends = headerItemIds.Count == 0
            ? new List<StockOutItemExtend>()
            : (await _stockOutItemExtendRepo.FindAsync(e =>
                    !e.IsDeleted && headerItemIds.Contains(e.Id)))
                .ToList();

        var attributedExtends = new Dictionary<string, StockOutItemExtend>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in lineExtends)
        {
            if (!string.IsNullOrWhiteSpace(e.Id))
                attributedExtends[e.Id.Trim()] = e;
        }

        foreach (var e in headerItemExtends)
        {
            if (string.IsNullOrWhiteSpace(e.Id))
                continue;
            var itemId = e.Id.Trim();
            if (attributedExtends.ContainsKey(itemId))
                continue;

            var extLine = e.SellOrderItemId?.Trim();
            var isOrphanOrPool =
                string.IsNullOrWhiteSpace(extLine)
                || e.StockType == stockTypeStocking
                || e.StockType == stockTypeSample;
            // 客单层且扩展已指向其它销售行：留给那一行的 A 路径，避免多行出库单重复累计
            if (!isOrphanOrPool && !string.Equals(extLine, lineId, StringComparison.OrdinalIgnoreCase))
                continue;

            attributedExtends[itemId] = e;
        }

        if (attributedExtends.Count == 0)
            return (0m, new List<StockOut>());

        var itemIds = attributedExtends.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
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

        // 勿在 IQueryable/FindAsync 中调用 IsSalesStockOut（EF 无法翻译自定义方法）
        var completedHeaders = (await _stockOutRepo.FindAsync(o =>
                !o.IsDeleted
                && stockOutIds.Contains(o.Id)
                && (o.Status == StockOutCompleted || o.Status == StockOutFinished)
                && (o.StockOutType == StockOutTypeCode.Sales
                    || o.StockOutType == StockOutTypeCode.LegacySales)))
            .ToList();
        var completedIdSet = completedHeaders
            .Select(o => o.Id.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        decimal sum = 0m;
        foreach (var item in outItems)
        {
            if (!completedIdSet.Contains(item.StockOutId.Trim()))
                continue;
            if (!attributedExtends.TryGetValue(item.Id.Trim(), out var extRow))
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
        var headerLineIds = completedStockOuts
            .Where(o => !string.IsNullOrWhiteSpace(o.SellOrderItemId)
                        && string.Equals(o.SellOrderItemId.Trim(), lineId, StringComparison.OrdinalIgnoreCase))
            .Select(o => o.Id.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 与 SumCompleted 归属一致：扩展行挂本行，或头表挂本行且扩展为空/备货/样品（历史脏数据）
        const short stockTypeStocking = 2;
        const short stockTypeSample = 3;
        var extends = (await _stockOutItemExtendRepo.FindAsync(e =>
                !e.IsDeleted && itemIds.Contains(e.Id)))
            .Where(e =>
            {
                var extLine = e.SellOrderItemId?.Trim();
                if (string.Equals(extLine, lineId, StringComparison.OrdinalIgnoreCase))
                    return true;
                var itemId = e.Id.Trim();
                var stockOutId = outItems
                    .FirstOrDefault(i => string.Equals(i.Id.Trim(), itemId, StringComparison.OrdinalIgnoreCase))
                    ?.StockOutId?.Trim();
                if (string.IsNullOrEmpty(stockOutId) || !headerLineIds.Contains(stockOutId))
                    return false;
                return string.IsNullOrWhiteSpace(extLine)
                       || e.StockType == stockTypeStocking
                       || e.StockType == stockTypeSample;
            })
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

    /// <summary>
    /// 备货拣货用量与成本：与操作面板 stockingUsage 同源（装箱明细 + IsStockingSupplement 拣货），
    /// 成本取在库层 <c>PurchasePriceUsd × PickedQty</c>。
    /// </summary>
    private async Task<(decimal UsedQty, decimal CostUsd)> LoadStockingPickCostAsync(
        string sellOrderItemId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_packingItemRepo == null || _pickingTaskItemRepo == null || _stockItemRepo == null)
            return (0m, 0m);

        var lineId = sellOrderItemId.Trim();
        var notifyIds = (await _stockOutRequestRepo.FindAsync(r => r.SalesOrderItemId == lineId))
            .Select(r => r.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var packingItems = (await _packingItemRepo.FindAsync(pi =>
                !pi.IsDeleted
                && ((pi.SellOrderItemId != null && pi.SellOrderItemId == lineId)
                    || (pi.StockOutNotifyId != null && notifyIds.Contains(pi.StockOutNotifyId)))))
            .ToList();
        if (packingItems.Count == 0)
            return (0m, 0m);

        var packingItemIds = packingItems
            .Select(pi => pi.Id.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var pickItems = (await _pickingTaskItemRepo.FindAsync(pti =>
                !pti.IsDeleted
                && pti.IsStockingSupplement
                && pti.PickedQty > 0
                && pti.PackingItemId != null
                && packingItemIds.Contains(pti.PackingItemId)))
            .ToList();

        if (_pickingTaskRepo != null && pickItems.Count > 0)
        {
            var taskIds = pickItems
                .Select(p => p.PickingTaskId.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var activeTaskIds = (await _pickingTaskRepo.FindAsync(t =>
                    taskIds.Contains(t.Id) && !t.IsDeleted))
                .Select(t => t.Id.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            pickItems = pickItems
                .Where(p => activeTaskIds.Contains(p.PickingTaskId.Trim()))
                .ToList();
        }

        if (pickItems.Count == 0)
            return (0m, 0m);

        var stockIds = pickItems
            .Select(p => p.StockItemId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        var priceByStockId = stockIds.Count == 0
            ? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            : (await _stockItemRepo.FindAsync(si => stockIds.Contains(si.Id)))
                .ToDictionary(si => si.Id.Trim(), si => si.PurchasePriceUsd, StringComparer.OrdinalIgnoreCase);

        decimal usedQty = 0m;
        decimal costUsd = 0m;
        foreach (var pick in pickItems)
        {
            var qty = Math.Max(0, pick.PickedQty);
            if (qty <= 0)
                continue;
            usedQty += qty;
            var sid = pick.StockItemId?.Trim() ?? string.Empty;
            if (priceByStockId.TryGetValue(sid, out var unit))
                costUsd += qty * unit;
        }

        return (usedQty, Math.Round(costUsd, 2, MidpointRounding.AwayFromZero));
    }
}
