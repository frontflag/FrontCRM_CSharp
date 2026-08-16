using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Sales;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services;

/// <inheritdoc />
public sealed class SalesOrderSalesPriceDownstreamSyncService : ISalesOrderSalesPriceDownstreamSyncService
{
    private readonly IRepository<PackingItemExtend> _packingItemExtendRepo;
    private readonly IRepository<StockItem> _stockItemRepo;
    private readonly IRepository<StockOut> _stockOutRepo;
    private readonly IRepository<StockOutItem> _stockOutItemRepo;
    private readonly IRepository<StockOutItemExtend> _stockOutItemExtendRepo;
    private readonly IRepository<FinanceReceivable> _receivableRepo;
    private readonly ILogger<SalesOrderSalesPriceDownstreamSyncService> _logger;

    public SalesOrderSalesPriceDownstreamSyncService(
        IRepository<PackingItemExtend> packingItemExtendRepo,
        IRepository<StockItem> stockItemRepo,
        IRepository<StockOut> stockOutRepo,
        IRepository<StockOutItem> stockOutItemRepo,
        IRepository<StockOutItemExtend> stockOutItemExtendRepo,
        IRepository<FinanceReceivable> receivableRepo,
        ILogger<SalesOrderSalesPriceDownstreamSyncService> logger)
    {
        _packingItemExtendRepo = packingItemExtendRepo;
        _stockItemRepo = stockItemRepo;
        _stockOutRepo = stockOutRepo;
        _stockOutItemRepo = stockOutItemRepo;
        _stockOutItemExtendRepo = stockOutItemExtendRepo;
        _receivableRepo = receivableRepo;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SalesOrderSalesPriceDownstreamSyncResult> ApplyAsync(
        IReadOnlyList<SellOrderItem> items,
        CancellationToken cancellationToken = default)
    {
        var result = new SalesOrderSalesPriceDownstreamSyncResult();
        if (items.Count == 0)
            return result;

        var byLineId = items
            .Where(i => !string.IsNullOrWhiteSpace(i.Id))
            .GroupBy(i => i.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        if (byLineId.Count == 0)
            return result;

        var lineIds = byLineId.Keys.ToList();
        var observedOldPrice = new Dictionary<string, (decimal Price, short Currency, decimal ConvertPrice)>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var chunk in lineIds.Chunk(200))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunkList = chunk.ToList();
            result.PackingItemExtendsUpdated += await SyncPackingExtendsAsync(chunkList, byLineId, observedOldPrice);
            result.StockItemsUpdated += await SyncStockItemsAsync(chunkList, byLineId, observedOldPrice);
            result.StockOutItemExtendsUpdated += await SyncStockOutItemExtendsAsync(chunkList, byLineId, observedOldPrice);
            result.ReceivablesUpdated += await SyncReceivablesAsync(chunkList, byLineId, observedOldPrice, result.ReceivableWarnings);
        }

        result.StockOutHeadersUpdated = await RecalcAffectedStockOutHeadersAsync(lineIds, cancellationToken);

        foreach (var (lineId, oldSnap) in observedOldPrice)
        {
            if (!byLineId.TryGetValue(lineId, out var item))
                continue;
            if (oldSnap.Price == item.Price
                && oldSnap.Currency == item.Currency
                && oldSnap.ConvertPrice == item.ConvertPrice)
                continue;
            result.LineChanges.Add(new SalesOrderSalesPriceLineChangeDto
            {
                SellOrderItemId = item.Id,
                SellOrderItemCode = item.SellOrderItemCode,
                OldPrice = oldSnap.Price,
                NewPrice = item.Price,
                OldCurrency = oldSnap.Currency,
                NewCurrency = item.Currency,
                OldConvertPrice = oldSnap.ConvertPrice,
                NewConvertPrice = item.ConvertPrice
            });
        }

        _logger.LogInformation(
            "SO下游销售价刷新: Lines={Lines} Packing={Packing} StockItem={StockItem} StockOutExt={StockOutExt} StockOutHead={StockOutHead} Receivable={Receivable} Warnings={Warnings}",
            result.LineChanges.Count,
            result.PackingItemExtendsUpdated,
            result.StockItemsUpdated,
            result.StockOutItemExtendsUpdated,
            result.StockOutHeadersUpdated,
            result.ReceivablesUpdated,
            result.ReceivableWarnings.Count);

        return result;
    }

    private async Task<int> SyncPackingExtendsAsync(
        List<string> lineIds,
        IReadOnlyDictionary<string, SellOrderItem> byLineId,
        Dictionary<string, (decimal Price, short Currency, decimal ConvertPrice)> observedOldPrice)
    {
        var rows = (await _packingItemExtendRepo.FindAsync(e =>
                e.SellOrderItemId != null && lineIds.Contains(e.SellOrderItemId)))
            .ToList();
        var updated = 0;
        foreach (var row in rows)
        {
            var lineId = row.SellOrderItemId?.Trim();
            if (string.IsNullOrEmpty(lineId) || !byLineId.TryGetValue(lineId, out var item))
                continue;

            RememberOld(observedOldPrice, item, row.Price, row.PriceCurrency, row.PriceConvertPrice);
            if (row.Price == item.Price
                && row.PriceCurrency == item.Currency
                && row.PriceConvertPrice == item.ConvertPrice)
                continue;

            row.Price = item.Price;
            row.PriceCurrency = item.Currency;
            row.PriceConvertPrice = item.ConvertPrice;
            await _packingItemExtendRepo.UpdateAsync(row);
            updated++;
        }

        return updated;
    }

    private async Task<int> SyncStockItemsAsync(
        List<string> lineIds,
        IReadOnlyDictionary<string, SellOrderItem> byLineId,
        Dictionary<string, (decimal Price, short Currency, decimal ConvertPrice)> observedOldPrice)
    {
        var rows = (await _stockItemRepo.FindAsync(e =>
                e.SellOrderItemId != null && lineIds.Contains(e.SellOrderItemId)))
            .ToList();
        var updated = 0;
        foreach (var row in rows)
        {
            var lineId = row.SellOrderItemId?.Trim();
            if (string.IsNullOrEmpty(lineId) || !byLineId.TryGetValue(lineId, out var item))
                continue;

            RememberOld(observedOldPrice, item, row.SalesPrice, row.SalesCurrency, row.SalesPriceUsd);
            if (row.SalesPrice == item.Price
                && row.SalesCurrency == item.Currency
                && row.SalesPriceUsd == item.ConvertPrice)
                continue;

            row.SalesPrice = item.Price;
            row.SalesCurrency = item.Currency;
            row.SalesPriceUsd = item.ConvertPrice;
            row.SyncDenormalizedComputedFields();
            await _stockItemRepo.UpdateAsync(row);
            updated++;
        }

        return updated;
    }

    private async Task<int> SyncStockOutItemExtendsAsync(
        List<string> lineIds,
        IReadOnlyDictionary<string, SellOrderItem> byLineId,
        Dictionary<string, (decimal Price, short Currency, decimal ConvertPrice)> observedOldPrice)
    {
        var rows = (await _stockOutItemExtendRepo.FindAsync(e =>
                e.SellOrderItemId != null && lineIds.Contains(e.SellOrderItemId)))
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
            var lineId = row.SellOrderItemId?.Trim();
            if (string.IsNullOrEmpty(lineId) || !byLineId.TryGetValue(lineId, out var item))
                continue;

            RememberOld(observedOldPrice, item, row.SalesPrice, row.SalesCurrency, row.SalesPriceUsd);
            var qty = row.QtyStockOut > 0
                ? row.QtyStockOut
                : qtyByItemId.TryGetValue(row.Id, out var fallbackQty) ? fallbackQty : 0;
            var newProfit = StockItem.ComputeProfitOutBizUsd(
                item.Id,
                item.ConvertPrice,
                row.PurchasePriceUsd,
                qty);

            if (row.SalesPrice == item.Price
                && row.SalesCurrency == item.Currency
                && row.SalesPriceUsd == item.ConvertPrice
                && row.ProfitOutBizUsd == newProfit)
                continue;

            row.SalesPrice = item.Price;
            row.SalesCurrency = item.Currency;
            row.SalesPriceUsd = item.ConvertPrice;
            row.ProfitOutBizUsd = newProfit;
            await _stockOutItemExtendRepo.UpdateAsync(row);
            updated++;
        }

        return updated;
    }

    private async Task<int> RecalcAffectedStockOutHeadersAsync(
        IReadOnlyList<string> lineIds,
        CancellationToken cancellationToken)
    {
        var lineList = lineIds.ToList();
        var touchedExtends = (await _stockOutItemExtendRepo.FindAsync(e =>
                e.SellOrderItemId != null && lineList.Contains(e.SellOrderItemId)))
            .ToList();
        if (touchedExtends.Count == 0)
            return 0;

        var touchedItemIds = touchedExtends
            .Select(e => e.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var touchedItems = (await _stockOutItemRepo.FindAsync(i => touchedItemIds.Contains(i.Id))).ToList();
        var headerIds = touchedItems
            .Select(i => i.StockOutId?.Trim())
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
            var headers = (await _stockOutRepo.FindAsync(h => headerList.Contains(h.Id))).ToList();
            var allItems = (await _stockOutItemRepo.FindAsync(i => headerList.Contains(i.StockOutId))).ToList();
            var allItemIds = allItems.Select(i => i.Id).ToList();
            var allExtends = allItemIds.Count == 0
                ? new List<StockOutItemExtend>()
                : (await _stockOutItemExtendRepo.FindAsync(e => allItemIds.Contains(e.Id))).ToList();
            var extById = allExtends.ToDictionary(e => e.Id, StringComparer.OrdinalIgnoreCase);
            var itemsByHeader = allItems
                .GroupBy(i => i.StockOutId.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (var header in headers)
            {
                if (!itemsByHeader.TryGetValue(header.Id, out var lines) || lines.Count == 0)
                    continue;

                decimal total = 0m;
                foreach (var line in lines)
                {
                    extById.TryGetValue(line.Id, out var ext);
                    var qty = ext != null && ext.QtyStockOut > 0
                        ? ext.QtyStockOut
                        : (line.ActualQty > 0 ? line.ActualQty : line.Quantity);
                    var price = ext?.SalesPrice ?? 0m;
                    total += qty * price;
                }

                var rounded = Math.Round(total, 2, MidpointRounding.AwayFromZero);
                if (header.TotalAmount == rounded)
                    continue;
                header.TotalAmount = rounded;
                header.ModifyTime = DateTime.UtcNow;
                await _stockOutRepo.UpdateAsync(header);
                updated++;
            }
        }

        return updated;
    }

    private async Task<int> SyncReceivablesAsync(
        List<string> lineIds,
        IReadOnlyDictionary<string, SellOrderItem> byLineId,
        Dictionary<string, (decimal Price, short Currency, decimal ConvertPrice)> observedOldPrice,
        List<SalesOrderReceivableAmountWarningDto> warnings)
    {
        var rows = (await _receivableRepo.FindAsync(r => lineIds.Contains(r.SellOrderItemId))).ToList();
        var updated = 0;
        foreach (var row in rows)
        {
            var lineId = row.SellOrderItemId?.Trim();
            if (string.IsNullOrEmpty(lineId) || !byLineId.TryGetValue(lineId, out var item))
                continue;

            RememberOld(observedOldPrice, item, row.UnitPrice, row.Currency, null);
            var amount = Math.Round(row.OutboundQty * item.Price, 2, MidpointRounding.AwayFromZero);
            var verifiedToBe = Math.Round(amount - row.VerifiedDone, 2, MidpointRounding.AwayFromZero);
            var invoiceToBe = Math.Round(amount - row.InvoiceMatchDone, 2, MidpointRounding.AwayFromZero);
            var verificationStatus = FinanceVerificationStatusCode.Resolve(amount, row.VerifiedDone);
            var invoiceStatus = FinanceVerificationStatusCode.Resolve(amount, row.InvoiceMatchDone);

            if (row.UnitPrice == item.Price
                && row.Currency == item.Currency
                && row.Amount == amount
                && row.VerifiedToBe == verifiedToBe
                && row.InvoiceMatchToBe == invoiceToBe
                && row.VerificationStatus == verificationStatus
                && row.InvoiceMatchStatus == invoiceStatus)
            {
                AddWarningIfNeeded(warnings, row, item, amount, verifiedToBe, invoiceToBe);
                continue;
            }

            row.UnitPrice = item.Price;
            row.Currency = item.Currency;
            row.Amount = amount;
            row.VerifiedToBe = verifiedToBe;
            row.VerificationStatus = verificationStatus;
            row.InvoiceMatchToBe = invoiceToBe;
            row.InvoiceMatchStatus = invoiceStatus;
            row.ModifyTime = DateTime.UtcNow;
            await _receivableRepo.UpdateAsync(row);
            updated++;
            AddWarningIfNeeded(warnings, row, item, amount, verifiedToBe, invoiceToBe);
        }

        return updated;
    }

    private static void AddWarningIfNeeded(
        List<SalesOrderReceivableAmountWarningDto> warnings,
        FinanceReceivable row,
        SellOrderItem item,
        decimal amount,
        decimal verifiedToBe,
        decimal invoiceToBe)
    {
        var verifiedOver = row.VerifiedDone > amount;
        var invoiceOver = row.InvoiceMatchDone > amount;
        if (!verifiedOver && !invoiceOver)
            return;

        warnings.Add(new SalesOrderReceivableAmountWarningDto
        {
            ReceivableId = row.Id,
            ReceivableCode = row.ReceivableCode,
            SellOrderItemId = item.Id,
            SellOrderItemCode = item.SellOrderItemCode,
            Amount = amount,
            VerifiedDone = row.VerifiedDone,
            VerifiedToBe = verifiedToBe,
            InvoiceMatchDone = row.InvoiceMatchDone,
            InvoiceMatchToBe = invoiceToBe,
            VerifiedOverAmount = verifiedOver,
            InvoiceMatchOverAmount = invoiceOver
        });
    }

    private static void RememberOld(
        Dictionary<string, (decimal Price, short Currency, decimal ConvertPrice)> observedOldPrice,
        SellOrderItem item,
        decimal? price,
        short? currency,
        decimal? convertPrice)
    {
        if (observedOldPrice.ContainsKey(item.Id))
            return;
        var oldPrice = price ?? 0m;
        var oldCurrency = currency ?? 0;
        var oldConvert = convertPrice ?? item.ConvertPrice;
        if (oldPrice == item.Price && oldCurrency == item.Currency && oldConvert == item.ConvertPrice)
            return;
        observedOldPrice[item.Id] = (oldPrice, oldCurrency, oldConvert);
    }
}
