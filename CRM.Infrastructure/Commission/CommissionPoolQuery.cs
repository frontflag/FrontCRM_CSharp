using CRM.Core.Constants;
using CRM.Core.Models.Inventory;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Commission;

public sealed class CommissionPoolCandidate
{
    public required string StockOutItemId { get; init; }
    public required string StockOutId { get; init; }
    public required string StockOutCode { get; init; }
    public DateOnly? StockOutDate { get; init; }
    public string? SellOrderId { get; init; }
    public string? SellOrderCode { get; init; }
    public string? SellOrderItemId { get; init; }
    public string? SellOrderItemCode { get; init; }
    public string? PurchaseOrderId { get; init; }
    public string? PurchaseOrderCode { get; init; }
    public string? PurchaseOrderItemId { get; init; }
    public string? PurchaseOrderItemCode { get; init; }
    public decimal GpUsd { get; init; }
    public DateOnly? ReceiptDate { get; init; }
    public short ReceiptProgressStatus { get; init; }
    public string? SalesUserId { get; init; }
    public string? PurchaseUserId { get; init; }
}

public sealed class CommissionPersonStockOutRow
{
    public required string StockOutItemId { get; init; }
    public required string StockOutId { get; init; }
    public required string StockOutCode { get; init; }
    public DateOnly? StockOutDate { get; init; }
    public short ReceiptProgressStatus { get; init; }
    public DateOnly? ReceiptDate { get; init; }
    public decimal GpUsd { get; init; }
}

public sealed class CommissionPoolQuery
{
    readonly ILogger<CommissionPoolQuery> _logger;

    public CommissionPoolQuery(ILogger<CommissionPoolQuery> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<CommissionPoolCandidate>> LoadAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var raw = await (
            from ext in db.StockOutItemExtends.AsNoTracking()
            join item in db.StockOutItems.AsNoTracking() on ext.Id equals item.Id
            join so in db.StockOuts.AsNoTracking() on item.StockOutId equals so.Id
            join soie in db.SellOrderItemExtends.AsNoTracking() on ext.SellOrderItemId equals soie.Id
            where !ext.IsDeleted
                  && !item.IsDeleted
                  && !so.IsDeleted
                  && so.Status == StockOutStatusCode.Completed
                  && ext.SellOrderItemId != null
            select new RawRow(
                item.Id,
                item.StockOutId,
                so.StockOutCode,
                so.StockOutDate,
                item.PackingId,
                ext.StockItemId ?? item.StockItemId,
                ext.SellOrderItemId,
                ext.SellOrderItemCode,
                ext.PurchaseOrderItemId,
                ext.PurchaseOrderItemCode,
                ext.ProfitOutBizUsd,
                ext.QtyStockOut > 0 ? ext.QtyStockOut : item.Quantity,
                ext.SalesPriceUsd,
                ext.PurchasePriceUsd)
        ).ToListAsync(cancellationToken);

        if (raw.Count == 0)
            return [];

        var sellItemIds = raw.Select(x => x.SellOrderItemId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var poItemIds = raw.Select(x => x.PurchaseOrderItemId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var stockItemIds = raw.Select(x => x.StockItemId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var packingIds = raw.Select(x => x.PackingId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();

        var sellItems = await db.SellOrderItems.AsNoTracking()
            .Where(x => sellItemIds.Contains(x.Id))
            .Select(x => new { x.Id, x.SellOrderId, x.ConvertPrice, x.Price })
            .ToListAsync(cancellationToken);
        var sellOrderIds = sellItems.Select(x => x.SellOrderId).Distinct().ToList();
        var sellOrders = await db.SellOrders.AsNoTracking()
            .Where(x => sellOrderIds.Contains(x.Id))
            .Select(x => new { x.Id, x.SellOrderCode, x.SalesUserId })
            .ToListAsync(cancellationToken);

        var poItems = poItemIds.Count == 0
            ? []
            : await db.PurchaseOrderItems.AsNoTracking()
                .Where(x => poItemIds.Contains(x.Id) && !x.IsDeleted)
                .Select(x => new { x.Id, x.PurchaseOrderId })
                .ToListAsync(cancellationToken);
        var poIds = poItems.Select(x => x.PurchaseOrderId).Distinct().ToList();
        var purchaseOrders = poIds.Count == 0
            ? []
            : await db.PurchaseOrders.AsNoTracking()
                .Where(x => poIds.Contains(x.Id) && !x.IsDeleted)
                .Select(x => new { x.Id, x.PurchaseOrderCode, x.PurchaseUserId })
                .ToListAsync(cancellationToken);

        var stockItems = stockItemIds.Count == 0
            ? []
            : await db.StockItems.AsNoTracking()
                .Where(x => stockItemIds.Contains(x.Id))
                .Select(x => new { x.Id, x.SalespersonId, x.PurchaserId })
                .ToListAsync(cancellationToken);

        var packings = packingIds.Count == 0
            ? []
            : await db.Packings.AsNoTracking()
                .Where(x => packingIds.Contains(x.Id) && !x.IsDeleted)
                .Select(x => new { x.Id, x.SalesId })
                .ToListAsync(cancellationToken);

        var sellItemToOrder = sellItems.ToDictionary(x => x.Id, x => x.SellOrderId);
        var sellConvertMap = sellItems.ToDictionary(x => x.Id, x => (decimal?)x.ConvertPrice);
        var sellPriceMap = sellItems.ToDictionary(x => x.Id, x => x.Price);
        var sellOrderMap = sellOrders.ToDictionary(x => x.Id);
        var poItemMap = poItems.ToDictionary(x => x.Id);
        var poMap = purchaseOrders.ToDictionary(x => x.Id);
        var stockMap = stockItems.ToDictionary(x => x.Id);
        var packingMap = packings.ToDictionary(x => x.Id);

        var receivableFacts = await LoadReceivableFactsAsync(
            db,
            raw.Select(x => x.StockOutId).Distinct().ToList(),
            cancellationToken);

        var result = new List<CommissionPoolCandidate>(raw.Count);
        foreach (var row in raw)
        {
            if (string.IsNullOrWhiteSpace(row.SellOrderItemId))
                continue;
            var stockOutDate = row.StockOutDate.HasValue
                ? CommissionShanghai.ToDate(row.StockOutDate.Value)
                : (DateOnly?)null;
            var (receiptStatus, receiptDate) = ResolveReceipt(
                row,
                sellPriceMap,
                receivableFacts,
                stockOutDate);
            if (receiptStatus == FinanceVerificationStatusCode.Complete && !receiptDate.HasValue)
            {
                _logger.LogWarning(
                    "提成入池：出库 {StockOutId} 销售明细 {SellOrderItemId} 应收已核销完成但无法回放核销日",
                    row.StockOutId,
                    row.SellOrderItemId);
            }

            sellItemToOrder.TryGetValue(row.SellOrderItemId, out var sellOrderId);
            sellOrderMap.TryGetValue(sellOrderId ?? "", out var sellOrder);
            packingMap.TryGetValue(row.PackingId ?? "", out var packing);
            stockMap.TryGetValue(row.StockItemId ?? "", out var stock);

            string? purchaseOrderId = null;
            string? purchaseOrderCode = null;
            string? purchaseUserId = null;
            if (!string.IsNullOrWhiteSpace(row.PurchaseOrderItemId)
                && poItemMap.TryGetValue(row.PurchaseOrderItemId, out var poItem)
                && poMap.TryGetValue(poItem.PurchaseOrderId, out var po))
            {
                purchaseOrderId = po.Id;
                purchaseOrderCode = po.PurchaseOrderCode;
                purchaseUserId = FirstNonEmpty(po.PurchaseUserId, stock?.PurchaserId);
            }

            result.Add(new CommissionPoolCandidate
            {
                StockOutItemId = row.StockOutItemId,
                StockOutId = row.StockOutId,
                StockOutCode = row.StockOutCode,
                StockOutDate = row.StockOutDate.HasValue
                    ? CommissionShanghai.ToDate(row.StockOutDate.Value)
                    : null,
                SellOrderId = sellOrder?.Id,
                SellOrderCode = sellOrder?.SellOrderCode,
                SellOrderItemId = row.SellOrderItemId,
                SellOrderItemCode = row.SellOrderItemCode,
                PurchaseOrderId = purchaseOrderId,
                PurchaseOrderCode = purchaseOrderCode,
                PurchaseOrderItemId = row.PurchaseOrderItemId,
                PurchaseOrderItemCode = row.PurchaseOrderItemCode,
                GpUsd = ResolveLineGp(row, sellConvertMap.GetValueOrDefault(row.SellOrderItemId)),
                ReceiptDate = receiptDate,
                ReceiptProgressStatus = receiptStatus,
                SalesUserId = FirstNonEmpty(sellOrder?.SalesUserId, packing?.SalesId, stock?.SalespersonId),
                PurchaseUserId = purchaseUserId
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<CommissionPersonStockOutRow>> LoadUnlockedForUserAsync(
        ApplicationDbContext db,
        short roleType,
        string userId,
        CancellationToken cancellationToken)
    {
        userId = userId.Trim();
        var isPurchase = roleType == (short)CommissionRoleType.Purchase;

        List<string> sellItemIds;
        List<string> packingIds;
        List<string> salesStockItemIds;
        List<string> poItemIds;
        List<string> purchaseStockItemIds;
        if (isPurchase)
        {
            var poIds = await db.PurchaseOrders.AsNoTracking()
                .Where(x => x.PurchaseUserId == userId && !x.IsDeleted)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            poItemIds = poIds.Count == 0
                ? []
                : await db.PurchaseOrderItems.AsNoTracking()
                    .Where(x => poIds.Contains(x.PurchaseOrderId) && !x.IsDeleted)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);
            purchaseStockItemIds = await db.StockItems.AsNoTracking()
                .Where(x => x.PurchaserId == userId)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            sellItemIds = [];
            packingIds = [];
            salesStockItemIds = [];
            if (poItemIds.Count == 0 && purchaseStockItemIds.Count == 0)
                return [];
        }
        else
        {
            var sellOrderIds = await db.SellOrders.AsNoTracking()
                .Where(x => x.SalesUserId == userId && !x.IsDeleted)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            sellItemIds = sellOrderIds.Count == 0
                ? []
                : await db.SellOrderItems.AsNoTracking()
                    .Where(x => sellOrderIds.Contains(x.SellOrderId) && !x.IsDeleted)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);
            packingIds = await db.Packings.AsNoTracking()
                .Where(x => x.SalesId == userId && !x.IsDeleted)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            salesStockItemIds = await db.StockItems.AsNoTracking()
                .Where(x => x.SalespersonId == userId)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            poItemIds = [];
            purchaseStockItemIds = [];
            if (sellItemIds.Count == 0 && packingIds.Count == 0 && salesStockItemIds.Count == 0)
                return [];
        }

        var raw = await (
            from ext in db.StockOutItemExtends.AsNoTracking()
            join item in db.StockOutItems.AsNoTracking() on ext.Id equals item.Id
            join so in db.StockOuts.AsNoTracking() on item.StockOutId equals so.Id
            where !ext.IsDeleted
                  && !item.IsDeleted
                  && !so.IsDeleted
                  && (so.Status == StockOutStatusCode.Ready || so.Status == StockOutStatusCode.Completed)
                  && !db.CommissionLockeds.Any(l => l.RoleType == roleType && l.StockOutItemId == item.Id)
                  && (
                      (ext.SellOrderItemId != null && sellItemIds.Contains(ext.SellOrderItemId))
                      || (item.PackingId != null && packingIds.Contains(item.PackingId))
                      || (ext.StockItemId != null && salesStockItemIds.Contains(ext.StockItemId))
                      || (item.StockItemId != null && salesStockItemIds.Contains(item.StockItemId))
                      || (ext.PurchaseOrderItemId != null && poItemIds.Contains(ext.PurchaseOrderItemId))
                      || (ext.StockItemId != null && purchaseStockItemIds.Contains(ext.StockItemId))
                      || (item.StockItemId != null && purchaseStockItemIds.Contains(item.StockItemId)))
            select new RawRow(
                item.Id,
                item.StockOutId,
                so.StockOutCode,
                so.StockOutDate,
                item.PackingId,
                ext.StockItemId ?? item.StockItemId,
                ext.SellOrderItemId,
                ext.SellOrderItemCode,
                ext.PurchaseOrderItemId,
                ext.PurchaseOrderItemCode,
                ext.ProfitOutBizUsd,
                ext.QtyStockOut > 0 ? ext.QtyStockOut : item.Quantity,
                ext.SalesPriceUsd,
                ext.PurchasePriceUsd)
        ).ToListAsync(cancellationToken);

        if (raw.Count == 0)
            return [];

        var allSellItemIds = raw
            .Select(x => x.SellOrderItemId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct()
            .ToList();
        var allPoItemIds = raw.Select(x => x.PurchaseOrderItemId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var allStockItemIds = raw.Select(x => x.StockItemId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var allPackingIds = raw.Select(x => x.PackingId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();

        var sellItems = allSellItemIds.Count == 0
            ? []
            : await db.SellOrderItems.AsNoTracking()
                .Where(x => allSellItemIds.Contains(x.Id))
                .Select(x => new { x.Id, x.SellOrderId, x.ConvertPrice, x.Price })
                .ToListAsync(cancellationToken);
        var sellOrderIdsForMap = sellItems.Select(x => x.SellOrderId).Distinct().ToList();
        var sellOrders = sellOrderIdsForMap.Count == 0
            ? []
            : await db.SellOrders.AsNoTracking()
                .Where(x => sellOrderIdsForMap.Contains(x.Id))
                .Select(x => new { x.Id, x.SellOrderCode, x.SalesUserId })
                .ToListAsync(cancellationToken);

        var poItems = allPoItemIds.Count == 0
            ? []
            : await db.PurchaseOrderItems.AsNoTracking()
                .Where(x => allPoItemIds.Contains(x.Id) && !x.IsDeleted)
                .Select(x => new { x.Id, x.PurchaseOrderId })
                .ToListAsync(cancellationToken);
        var poIdsForMap = poItems.Select(x => x.PurchaseOrderId).Distinct().ToList();
        var purchaseOrders = poIdsForMap.Count == 0
            ? []
            : await db.PurchaseOrders.AsNoTracking()
                .Where(x => poIdsForMap.Contains(x.Id) && !x.IsDeleted)
                .Select(x => new { x.Id, x.PurchaseOrderCode, x.PurchaseUserId })
                .ToListAsync(cancellationToken);

        var stockItems = allStockItemIds.Count == 0
            ? []
            : await db.StockItems.AsNoTracking()
                .Where(x => allStockItemIds.Contains(x.Id))
                .Select(x => new { x.Id, x.SalespersonId, x.PurchaserId })
                .ToListAsync(cancellationToken);

        var packings = allPackingIds.Count == 0
            ? []
            : await db.Packings.AsNoTracking()
                .Where(x => allPackingIds.Contains(x.Id) && !x.IsDeleted)
                .Select(x => new { x.Id, x.SalesId })
                .ToListAsync(cancellationToken);

        var sellItemToOrder = sellItems.ToDictionary(x => x.Id, x => x.SellOrderId);
        var sellConvertMap = sellItems.ToDictionary(x => x.Id, x => (decimal?)x.ConvertPrice);
        var sellPriceMap = sellItems.ToDictionary(x => x.Id, x => x.Price);
        var sellOrderMap = sellOrders.ToDictionary(x => x.Id);
        var poItemMap = poItems.ToDictionary(x => x.Id);
        var poMap = purchaseOrders.ToDictionary(x => x.Id);
        var stockMap = stockItems.ToDictionary(x => x.Id);
        var packingMap = packings.ToDictionary(x => x.Id);

        var receivableFacts = await LoadReceivableFactsAsync(
            db,
            raw.Select(x => x.StockOutId).Distinct().ToList(),
            cancellationToken);

        var result = new List<CommissionPersonStockOutRow>(raw.Count);
        foreach (var row in raw)
        {
            sellItemToOrder.TryGetValue(row.SellOrderItemId ?? "", out var sellOrderId);
            sellOrderMap.TryGetValue(sellOrderId ?? "", out var sellOrder);
            packingMap.TryGetValue(row.PackingId ?? "", out var packing);
            stockMap.TryGetValue(row.StockItemId ?? "", out var stock);

            string? purchaseUserId = null;
            if (!string.IsNullOrWhiteSpace(row.PurchaseOrderItemId)
                && poItemMap.TryGetValue(row.PurchaseOrderItemId, out var poItem)
                && poMap.TryGetValue(poItem.PurchaseOrderId, out var po))
            {
                purchaseUserId = FirstNonEmpty(po.PurchaseUserId, stock?.PurchaserId);
            }

            var salesUserId = FirstNonEmpty(sellOrder?.SalesUserId, packing?.SalesId, stock?.SalespersonId);
            var ownerId = isPurchase ? purchaseUserId : salesUserId;
            if (!string.Equals(ownerId, userId, StringComparison.OrdinalIgnoreCase))
                continue;

            var stockOutDate = row.StockOutDate.HasValue
                ? CommissionShanghai.ToDate(row.StockOutDate.Value)
                : (DateOnly?)null;
            var (status, receiptDate) = ResolveReceipt(row, sellPriceMap, receivableFacts, stockOutDate);

            result.Add(new CommissionPersonStockOutRow
            {
                StockOutItemId = row.StockOutItemId,
                StockOutId = row.StockOutId,
                StockOutCode = row.StockOutCode,
                StockOutDate = stockOutDate,
                ReceiptProgressStatus = status,
                ReceiptDate = receiptDate,
                GpUsd = ResolveLineGp(
                    row,
                    string.IsNullOrWhiteSpace(row.SellOrderItemId)
                        ? null
                        : sellConvertMap.GetValueOrDefault(row.SellOrderItemId))
            });
        }

        return result;
    }

    async Task<Dictionary<string, List<CommissionStockOutReceivableFact>>> LoadReceivableFactsAsync(
        ApplicationDbContext db,
        List<string> stockOutIds,
        CancellationToken cancellationToken)
    {
        if (stockOutIds.Count == 0)
            return new Dictionary<string, List<CommissionStockOutReceivableFact>>(StringComparer.Ordinal);

        var receivables = await db.FinanceReceivables.AsNoTracking()
            .Where(x => stockOutIds.Contains(x.StockOutId))
            .Select(x => new { x.Id, x.StockOutId, x.SellOrderItemId, x.Amount, x.VerificationStatus })
            .ToListAsync(cancellationToken);
        if (receivables.Count == 0)
            return new Dictionary<string, List<CommissionStockOutReceivableFact>>(StringComparer.Ordinal);

        var receivableIds = receivables.Select(x => x.Id).ToList();
        var writeOffs = await db.FinanceReceivableWriteOffs.AsNoTracking()
            .Where(x => receivableIds.Contains(x.FinanceReceivableId))
            .Select(x => new { x.FinanceReceivableId, x.FinanceReceiptId, x.Amount, x.CreateTime })
            .ToListAsync(cancellationToken);

        var receiptIds = writeOffs
            .Select(x => x.FinanceReceiptId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();
        var receipts = receiptIds.Count == 0
            ? []
            : await db.FinanceReceipts.AsNoTracking()
                .Where(x => receiptIds.Contains(x.Id) && !x.IsDeleted)
                .Select(x => new { x.Id, x.ReceiptDate })
                .ToListAsync(cancellationToken);
        var receiptMap = receipts.ToDictionary(x => x.Id);
        var woByRecv = writeOffs
            .GroupBy(x => x.FinanceReceivableId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new Dictionary<string, List<CommissionStockOutReceivableFact>>(StringComparer.Ordinal);
        foreach (var recv in receivables)
        {
            var steps = new List<CommissionWriteOffStep>();
            if (woByRecv.TryGetValue(recv.Id, out var wos))
            {
                foreach (var wo in wos)
                {
                    DateOnly eventDate;
                    if (!string.IsNullOrWhiteSpace(wo.FinanceReceiptId)
                        && receiptMap.TryGetValue(wo.FinanceReceiptId, out var receipt)
                        && receipt.ReceiptDate.HasValue)
                    {
                        eventDate = CommissionShanghai.ToDate(receipt.ReceiptDate.Value);
                    }
                    else
                    {
                        eventDate = CommissionShanghai.ToDate(wo.CreateTime);
                    }

                    steps.Add(new CommissionWriteOffStep(wo.Amount, eventDate, wo.CreateTime));
                }
            }

            var key = ReceivableKey(recv.StockOutId, recv.SellOrderItemId);
            if (!result.TryGetValue(key, out var list))
            {
                list = [];
                result[key] = list;
            }

            list.Add(new CommissionStockOutReceivableFact(recv.VerificationStatus, recv.Amount, steps));
        }

        return result;
    }

    static (short Status, DateOnly? ReceiptDate) ResolveReceipt(
        RawRow row,
        Dictionary<string, decimal> sellPriceMap,
        Dictionary<string, List<CommissionStockOutReceivableFact>> receivableFacts,
        DateOnly? stockOutDate)
    {
        receivableFacts.TryGetValue(ReceivableKey(row.StockOutId, row.SellOrderItemId), out var facts);
        var lineAmount = 0m;
        if (!string.IsNullOrWhiteSpace(row.SellOrderItemId)
            && sellPriceMap.TryGetValue(row.SellOrderItemId, out var price))
        {
            lineAmount = Math.Round(row.Qty * price, 2, MidpointRounding.AwayFromZero);
        }

        return CommissionStockOutReceivableProgress.Resolve(facts, stockOutDate, lineAmount);
    }

    static string ReceivableKey(string? stockOutId, string? sellItemId) =>
        $"{(stockOutId ?? "").Trim().ToLowerInvariant()}\u001f{(sellItemId ?? "").Trim().ToLowerInvariant()}";

    static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }

        return null;
    }

    static decimal ResolveLineGp(RawRow row, decimal? soConvertPrice)
    {
        if (row.GpUsd != 0m)
            return decimal.Round(row.GpUsd, 2, MidpointRounding.AwayFromZero);
        return StockItem.ComputeProfitOutBizUsd(
            row.SellOrderItemId,
            row.SalesPriceUsd ?? soConvertPrice,
            row.PurchasePriceUsd,
            row.Qty);
    }

    readonly record struct RawRow(
        string StockOutItemId,
        string StockOutId,
        string StockOutCode,
        DateTime? StockOutDate,
        string? PackingId,
        string? StockItemId,
        string? SellOrderItemId,
        string? SellOrderItemCode,
        string? PurchaseOrderItemId,
        string? PurchaseOrderItemCode,
        decimal GpUsd,
        int Qty,
        decimal? SalesPriceUsd,
        decimal PurchasePriceUsd);
}
