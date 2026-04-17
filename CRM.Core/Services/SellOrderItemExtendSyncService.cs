using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services;

/// <inheritdoc />
public class SellOrderItemExtendSyncService : ISellOrderItemExtendSyncService
{
    private const short PoItemStatusConfirmed = 30;
    private const short StockInCompleted = 2;
    private const short StockInTypePurchase = 1;
    private const short SalesStockOutType = 1;
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
    private readonly IRepository<FinanceReceiptItem> _receiptItemRepo;
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
        IRepository<FinanceReceiptItem> receiptItemRepo,
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
        _receiptItemRepo = receiptItemRepo;
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

        var requests = (await _stockOutRequestRepo.FindAsync(r => r.SalesOrderItemId == id))
            .ToList();
        var notifySum = requests.Where(r => r.Status != 2).Sum(r => r.Quantity);
        ext.QtyStockOutNotify = notifySum;
        ext.QtyStockOutNotifyNot = Math.Max(0m, soItem.Qty - notifySum);

        // 实出数量 / 出库进度：stockout 头表 SellOrderItemId = 本销售明细、销售出库，状态为已出库(2) 或已完成(4)，累计 TotalQuantity 与销售明细数量比（与入库口径对称）
        var completedStockOuts = (await _stockOutRepo.FindAsync(o =>
                (o.Status == StockOutCompleted || o.Status == StockOutFinished)
                && o.StockOutType == SalesStockOutType
                && o.SellOrderItemId != null
                && o.SellOrderItemId == id))
            .ToList();
        var sumStockOut = completedStockOuts.Sum(o => o.TotalQuantity);
        ext.QtyStockOutActual = sumStockOut;

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

        var receiptItems = (await _receiptItemRepo.FindAsync(x =>
                x.SellOrderItemId != null && x.SellOrderItemId == id))
            .ToList();
        var verifiedSum = receiptItems.Sum(x => x.VerifiedAmount);
        ext.ReceiptAmountFinish = verifiedSum;
        ext.ReceiptAmountNot = Math.Max(0m, ext.ReceiptAmount - verifiedSum);

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
                    && s.StockInType == StockInTypePurchase))
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

        ApplyProfitFields(soItem, ext, poItems);

        ext.ModifyTime = DateTime.UtcNow;
        await _extendRepo.UpdateAsync(ext);

        _logger.LogInformation(
            "[SellLineStockOutSync] Recalculate done SellOrderItemId={SellOrderItemId} LineQty={LineQty} QtyStockOutActual={QtyStockOutActual} StockOutProgressStatus={StockOutProgressStatus} (0=待 1=部分 2=完成)",
            id, qtyLine, ext.QtyStockOutActual, ext.StockOutProgressStatus);
    }

    private static void ApplyProfitFields(SellOrderItem soItem, SellOrderItemExtend ext, List<PurchaseOrderItem> poItems)
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
        ext.ProfitOutBizUsd = Math.Round((soItem.ConvertPrice - avgCostUsd) * outQty, 2, MidpointRounding.AwayFromZero);
        var revOut = Math.Round(outQty * soItem.ConvertPrice, 2, MidpointRounding.AwayFromZero);
        var costOut = Math.Round(outQty * avgCostUsd, 2, MidpointRounding.AwayFromZero);
        ext.ProfitOutRateBiz = costOut > 0m
            ? Math.Round(revOut / costOut, 6, MidpointRounding.AwayFromZero)
            : 0m;
        // 财务 USD：出库时点汇率与加权成本方案未接入前，与业务 USD 同口径写入
        ext.ProfitOutFinUsd = ext.ProfitOutBizUsd;
        ext.ProfitOutRateFin = ext.ProfitOutRateBiz;
    }
}
