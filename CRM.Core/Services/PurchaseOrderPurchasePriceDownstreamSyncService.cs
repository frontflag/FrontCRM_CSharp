using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services;

/// <inheritdoc />
public sealed class PurchaseOrderPurchasePriceDownstreamSyncService : IPurchaseOrderPurchasePriceDownstreamSyncService
{
    private const short FinancePaymentCancelled = -2;
    private const short FinancePaymentAuditFailed = -1;

    private readonly IRepository<StockInNotify> _notifyRepo;
    private readonly IRepository<StockIn> _stockInRepo;
    private readonly IRepository<StockInItem> _stockInItemRepo;
    private readonly IRepository<StockInItemExtend> _stockInItemExtendRepo;
    private readonly IRepository<StockItem> _stockItemRepo;
    private readonly IRepository<StockOutItem> _stockOutItemRepo;
    private readonly IRepository<StockOutItemExtend> _stockOutItemExtendRepo;
    private readonly IRepository<FinancePayment> _paymentRepo;
    private readonly IRepository<FinancePaymentItem> _payItemRepo;
    private readonly ILogger<PurchaseOrderPurchasePriceDownstreamSyncService> _logger;

    public PurchaseOrderPurchasePriceDownstreamSyncService(
        IRepository<StockInNotify> notifyRepo,
        IRepository<StockIn> stockInRepo,
        IRepository<StockInItem> stockInItemRepo,
        IRepository<StockInItemExtend> stockInItemExtendRepo,
        IRepository<StockItem> stockItemRepo,
        IRepository<StockOutItem> stockOutItemRepo,
        IRepository<StockOutItemExtend> stockOutItemExtendRepo,
        IRepository<FinancePayment> paymentRepo,
        IRepository<FinancePaymentItem> payItemRepo,
        ILogger<PurchaseOrderPurchasePriceDownstreamSyncService> logger)
    {
        _notifyRepo = notifyRepo;
        _stockInRepo = stockInRepo;
        _stockInItemRepo = stockInItemRepo;
        _stockInItemExtendRepo = stockInItemExtendRepo;
        _stockItemRepo = stockItemRepo;
        _stockOutItemRepo = stockOutItemRepo;
        _stockOutItemExtendRepo = stockOutItemExtendRepo;
        _paymentRepo = paymentRepo;
        _payItemRepo = payItemRepo;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderPurchasePriceDownstreamSyncResult> ApplyAsync(
        IReadOnlyList<PurchaseOrderItem> items,
        CancellationToken cancellationToken = default)
    {
        var result = new PurchaseOrderPurchasePriceDownstreamSyncResult();
        if (items.Count == 0)
            return result;

        var byLineId = items
            .Where(i => !string.IsNullOrWhiteSpace(i.Id))
            .GroupBy(i => i.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        if (byLineId.Count == 0)
            return result;

        var lineIds = byLineId.Keys.ToList();
        var observedOld = new Dictionary<string, (decimal Cost, short Currency, decimal ConvertPrice)>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var chunk in lineIds.Chunk(200))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunkList = chunk.ToList();
            result.ArrivalNoticesUpdated += await SyncArrivalNoticesAsync(chunkList, byLineId, observedOld);
            var (stockInItems, stockInExtends) = await SyncStockInItemsAsync(
                chunkList, byLineId, observedOld, result.InvoiceMatchWarnings);
            result.StockInItemsUpdated += stockInItems;
            result.StockInItemExtendsUpdated += stockInExtends;
            result.StockItemsUpdated += await SyncStockItemsAsync(chunkList, byLineId, observedOld);
            result.StockOutItemExtendsUpdated += await SyncStockOutItemExtendsAsync(chunkList, byLineId, observedOld);
            await CollectPaymentOverWarningsAsync(chunkList, byLineId, result.PaymentOverWarnings);
        }

        result.StockInHeadersUpdated = await RecalcAffectedStockInHeadersAsync(lineIds, cancellationToken);

        foreach (var (lineId, oldSnap) in observedOld)
        {
            if (!byLineId.TryGetValue(lineId, out var item))
                continue;
            if (oldSnap.Cost == item.Cost
                && oldSnap.Currency == item.Currency
                && oldSnap.ConvertPrice == item.ConvertPrice)
                continue;
            result.LineChanges.Add(new PurchaseOrderPurchasePriceLineChangeDto
            {
                PurchaseOrderItemId = item.Id,
                PurchaseOrderItemCode = item.PurchaseOrderItemCode,
                OldCost = oldSnap.Cost,
                NewCost = item.Cost,
                OldCurrency = oldSnap.Currency,
                NewCurrency = item.Currency,
                OldConvertPrice = oldSnap.ConvertPrice,
                NewConvertPrice = item.ConvertPrice
            });
        }

        _logger.LogInformation(
            "PO下游采购价刷新: Lines={Lines} Notices={Notices} StockIn={StockIn} StockInHead={StockInHead} StockItem={StockItem} StockOutExt={StockOutExt} InvoiceWarn={InvoiceWarn} PayWarn={PayWarn}",
            result.LineChanges.Count,
            result.ArrivalNoticesUpdated,
            result.StockInItemsUpdated,
            result.StockInHeadersUpdated,
            result.StockItemsUpdated,
            result.StockOutItemExtendsUpdated,
            result.InvoiceMatchWarnings.Count,
            result.PaymentOverWarnings.Count);

        return result;
    }

    private async Task<int> SyncArrivalNoticesAsync(
        List<string> lineIds,
        IReadOnlyDictionary<string, PurchaseOrderItem> byLineId,
        Dictionary<string, (decimal Cost, short Currency, decimal ConvertPrice)> observedOld)
    {
        var rows = (await _notifyRepo.FindAsync(n => lineIds.Contains(n.PurchaseOrderItemId))).ToList();
        var updated = 0;
        foreach (var row in rows)
        {
            var lineId = row.PurchaseOrderItemId?.Trim();
            if (string.IsNullOrEmpty(lineId) || !byLineId.TryGetValue(lineId, out var item))
                continue;

            RememberOld(observedOld, item, row.Cost, item.Currency, item.ConvertPrice);
            if (row.Cost == item.Cost)
                continue;

            row.Cost = item.Cost;
            row.ExpectTotal = Math.Round((decimal)row.ExpectQty * row.Cost, 2, MidpointRounding.AwayFromZero);
            row.ReceiveTotal = Math.Round((decimal)row.ReceiveQty * row.Cost, 2, MidpointRounding.AwayFromZero);
            row.ModifyTime = DateTime.UtcNow;
            await _notifyRepo.UpdateAsync(row);
            updated++;
        }

        return updated;
    }

    private async Task<(int Items, int Extends)> SyncStockInItemsAsync(
        List<string> lineIds,
        IReadOnlyDictionary<string, PurchaseOrderItem> byLineId,
        Dictionary<string, (decimal Cost, short Currency, decimal ConvertPrice)> observedOld,
        List<PurchaseOrderInvoiceMatchWarningDto> invoiceWarnings)
    {
        var extends = (await _stockInItemExtendRepo.FindAsync(e =>
                e.PurchaseOrderItemId != null && lineIds.Contains(e.PurchaseOrderItemId)))
            .ToList();
        if (extends.Count == 0)
            return (0, 0);

        var itemIds = extends.Select(e => e.Id).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var items = (await _stockInItemRepo.FindAsync(i => itemIds.Contains(i.Id))).ToList();
        var itemById = items.ToDictionary(i => i.Id, StringComparer.OrdinalIgnoreCase);

        var itemsUpdated = 0;
        var extendsUpdated = 0;
        foreach (var ext in extends)
        {
            var lineId = ext.PurchaseOrderItemId?.Trim();
            if (string.IsNullOrEmpty(lineId) || !byLineId.TryGetValue(lineId, out var poItem))
                continue;
            if (!itemById.TryGetValue(ext.Id, out var line))
                continue;

            RememberOld(observedOld, poItem, line.Price, line.Currency ?? poItem.Currency, poItem.ConvertPrice);
            var qty = line.Quantity;
            var amount = Math.Round(qty * poItem.Cost, 2, MidpointRounding.AwayFromZero);
            var invoiceToBe = Math.Round(amount - ext.InvoiceMatchDone, 2, MidpointRounding.AwayFromZero);
            var invoiceStatus = FinanceVerificationStatusCode.Resolve(amount, ext.InvoiceMatchDone);

            var lineChanged = line.Price != poItem.Cost
                || line.Currency != poItem.Currency
                || line.Amount != amount;
            if (lineChanged)
            {
                line.Price = poItem.Cost;
                line.Currency = poItem.Currency;
                line.Amount = amount;
                line.ModifyTime = DateTime.UtcNow;
                await _stockInItemRepo.UpdateAsync(line);
                itemsUpdated++;
            }

            if (ext.InvoiceMatchToBe != invoiceToBe || ext.InvoiceMatchStatus != invoiceStatus)
            {
                ext.InvoiceMatchToBe = invoiceToBe;
                ext.InvoiceMatchStatus = invoiceStatus;
                ext.ModifyTime = DateTime.UtcNow;
                await _stockInItemExtendRepo.UpdateAsync(ext);
                extendsUpdated++;
            }

            if (ext.InvoiceMatchDone > amount)
            {
                invoiceWarnings.Add(new PurchaseOrderInvoiceMatchWarningDto
                {
                    StockInItemId = line.Id,
                    StockInItemCode = line.StockInItemCode,
                    PurchaseOrderItemId = poItem.Id,
                    PurchaseOrderItemCode = poItem.PurchaseOrderItemCode,
                    Amount = amount,
                    InvoiceMatchDone = ext.InvoiceMatchDone,
                    InvoiceMatchToBe = invoiceToBe
                });
            }
        }

        return (itemsUpdated, extendsUpdated);
    }

    private async Task<int> SyncStockItemsAsync(
        List<string> lineIds,
        IReadOnlyDictionary<string, PurchaseOrderItem> byLineId,
        Dictionary<string, (decimal Cost, short Currency, decimal ConvertPrice)> observedOld)
    {
        var rows = (await _stockItemRepo.FindAsync(e =>
                e.PurchaseOrderItemId != null && lineIds.Contains(e.PurchaseOrderItemId)))
            .ToList();
        var updated = 0;
        foreach (var row in rows)
        {
            var lineId = row.PurchaseOrderItemId?.Trim();
            if (string.IsNullOrEmpty(lineId) || !byLineId.TryGetValue(lineId, out var item))
                continue;

            RememberOld(observedOld, item, row.PurchasePrice, row.PurchaseCurrency, row.PurchasePriceUsd);
            var newAmount = Math.Round(item.Cost * row.QtyInbound, 2, MidpointRounding.AwayFromZero);
            if (row.PurchasePrice == item.Cost
                && row.PurchaseCurrency == item.Currency
                && row.PurchasePriceUsd == item.ConvertPrice
                && row.PurchaseAmount == newAmount)
                continue;

            row.PurchasePrice = item.Cost;
            row.PurchaseCurrency = item.Currency;
            row.PurchasePriceUsd = item.ConvertPrice;
            row.PurchaseAmount = newAmount;
            row.SyncDenormalizedComputedFields();
            await _stockItemRepo.UpdateAsync(row);
            updated++;
        }

        return updated;
    }

    private async Task<int> SyncStockOutItemExtendsAsync(
        List<string> lineIds,
        IReadOnlyDictionary<string, PurchaseOrderItem> byLineId,
        Dictionary<string, (decimal Cost, short Currency, decimal ConvertPrice)> observedOld)
    {
        var rows = (await _stockOutItemExtendRepo.FindAsync(e =>
                e.PurchaseOrderItemId != null && lineIds.Contains(e.PurchaseOrderItemId)))
            .ToList();
        if (rows.Count == 0)
            return 0;

        var itemIds = rows.Select(r => r.Id).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var stockOutItems = itemIds.Count == 0
            ? new List<StockOutItem>()
            : (await _stockOutItemRepo.FindAsync(i => itemIds.Contains(i.Id))).ToList();
        var qtyByItemId = stockOutItems.ToDictionary(
            i => i.Id,
            i => i.ActualQty > 0 ? i.ActualQty : i.Quantity,
            StringComparer.OrdinalIgnoreCase);

        var updated = 0;
        foreach (var row in rows)
        {
            var lineId = row.PurchaseOrderItemId?.Trim();
            if (string.IsNullOrEmpty(lineId) || !byLineId.TryGetValue(lineId, out var item))
                continue;

            RememberOld(observedOld, item, row.PurchasePrice, row.PurchaseCurrency, row.PurchasePriceUsd);
            var qty = row.QtyStockOut > 0
                ? row.QtyStockOut
                : qtyByItemId.TryGetValue(row.Id, out var fallbackQty) ? fallbackQty : 0;
            var newProfit = StockItem.ComputeProfitOutBizUsd(
                row.SellOrderItemId,
                row.SalesPriceUsd,
                item.ConvertPrice,
                qty);

            if (row.PurchasePrice == item.Cost
                && row.PurchaseCurrency == item.Currency
                && row.PurchasePriceUsd == item.ConvertPrice
                && row.ProfitOutBizUsd == newProfit)
                continue;

            row.PurchasePrice = item.Cost;
            row.PurchaseCurrency = item.Currency;
            row.PurchasePriceUsd = item.ConvertPrice;
            row.ProfitOutBizUsd = newProfit;
            await _stockOutItemExtendRepo.UpdateAsync(row);
            updated++;
        }

        return updated;
    }

    private async Task<int> RecalcAffectedStockInHeadersAsync(
        IReadOnlyList<string> lineIds,
        CancellationToken cancellationToken)
    {
        var lineList = lineIds.ToList();
        var touchedExtends = (await _stockInItemExtendRepo.FindAsync(e =>
                e.PurchaseOrderItemId != null && lineList.Contains(e.PurchaseOrderItemId)))
            .ToList();
        if (touchedExtends.Count == 0)
            return 0;

        var headerIds = touchedExtends
            .Select(e => e.StockInId?.Trim())
            .Where(id => !string.IsNullOrEmpty(id))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (headerIds.Count == 0)
            return 0;

        var updated = 0;
        foreach (var headerChunk in headerIds.Chunk(200))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var headerList = headerChunk.ToList();
            var headers = (await _stockInRepo.FindAsync(h => headerList.Contains(h.Id))).ToList();
            var allItems = (await _stockInItemRepo.FindAsync(i => headerList.Contains(i.StockInId))).ToList();
            var itemsByHeader = allItems
                .GroupBy(i => i.StockInId.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (var header in headers)
            {
                if (!itemsByHeader.TryGetValue(header.Id, out var lines) || lines.Count == 0)
                    continue;
                var total = Math.Round(lines.Sum(l => l.Amount), 2, MidpointRounding.AwayFromZero);
                if (header.TotalAmount == total)
                    continue;
                header.TotalAmount = total;
                header.ModifyTime = DateTime.UtcNow;
                await _stockInRepo.UpdateAsync(header);
                updated++;
            }
        }

        return updated;
    }

    private async Task CollectPaymentOverWarningsAsync(
        List<string> lineIds,
        IReadOnlyDictionary<string, PurchaseOrderItem> byLineId,
        List<PurchaseOrderPaymentOverWarningDto> warnings)
    {
        var payItems = (await _payItemRepo.FindAsync(p =>
                p.PurchaseOrderItemId != null && lineIds.Contains(p.PurchaseOrderItemId)))
            .ToList();
        if (payItems.Count == 0)
            return;

        var paymentIds = payItems
            .Select(p => p.FinancePaymentId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var payments = paymentIds.Count == 0
            ? new List<FinancePayment>()
            : (await _paymentRepo.FindAsync(p => paymentIds.Contains(p.Id))).ToList();
        var validPaymentIds = payments
            .Where(p => p.Status != FinancePaymentCancelled && p.Status != FinancePaymentAuditFailed)
            .Select(p => p.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var group in payItems.GroupBy(p => p.PurchaseOrderItemId!.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            if (!byLineId.TryGetValue(group.Key, out var item))
                continue;
            var done = group
                .Where(p => validPaymentIds.Contains(p.FinancePaymentId))
                .Sum(p => p.VerificationDone);
            var lineAmount = Math.Round(item.Qty * item.Cost, 2, MidpointRounding.AwayFromZero);
            if (done <= lineAmount)
                continue;
            warnings.Add(new PurchaseOrderPaymentOverWarningDto
            {
                PurchaseOrderItemId = item.Id,
                PurchaseOrderItemCode = item.PurchaseOrderItemCode,
                LineAmount = lineAmount,
                PaymentDone = done
            });
        }
    }

    private static void RememberOld(
        Dictionary<string, (decimal Cost, short Currency, decimal ConvertPrice)> observedOld,
        PurchaseOrderItem item,
        decimal? cost,
        short? currency,
        decimal? convertPrice)
    {
        if (observedOld.ContainsKey(item.Id))
            return;
        var oldCost = cost ?? 0m;
        var oldCurrency = currency ?? 0;
        var oldConvert = convertPrice ?? item.ConvertPrice;
        if (oldCost == item.Cost && oldCurrency == item.Currency && oldConvert == item.ConvertPrice)
            return;
        observedOld[item.Id] = (oldCost, oldCurrency, oldConvert);
    }
}
