using CRM.Core.Interfaces;
using CRM.Core.Models.Customs;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services;

/// <inheritdoc />
public sealed class PurchaseOrderIdentityDownstreamSyncService : IPurchaseOrderIdentityDownstreamSyncService
{
    private const int MaxLoggedChanges = 80;
    private const int ArrivalPnMax = 128;
    private const int ArrivalBrandMax = 64;

    private readonly IRepository<StockInNotify> _notifyRepo;
    private readonly IRepository<StockInItem> _stockInItemRepo;
    private readonly IRepository<StockInItemExtend> _stockInItemExtendRepo;
    private readonly IRepository<StockItem> _stockItemRepo;
    private readonly IRepository<PackingItem> _packingItemRepo;
    private readonly IRepository<CustomsDeclarationItem> _customsItemRepo;
    private readonly IStockItemPurchaseIdentityRebucketService? _rebucket;
    private readonly ILogger<PurchaseOrderIdentityDownstreamSyncService> _logger;

    public PurchaseOrderIdentityDownstreamSyncService(
        IRepository<StockInNotify> notifyRepo,
        IRepository<StockInItem> stockInItemRepo,
        IRepository<StockInItemExtend> stockInItemExtendRepo,
        IRepository<StockItem> stockItemRepo,
        IRepository<PackingItem> packingItemRepo,
        IRepository<CustomsDeclarationItem> customsItemRepo,
        ILogger<PurchaseOrderIdentityDownstreamSyncService> logger,
        IStockItemPurchaseIdentityRebucketService? rebucket = null)
    {
        _notifyRepo = notifyRepo;
        _stockInItemRepo = stockInItemRepo;
        _stockInItemExtendRepo = stockInItemExtendRepo;
        _stockItemRepo = stockItemRepo;
        _packingItemRepo = packingItemRepo;
        _customsItemRepo = customsItemRepo;
        _rebucket = rebucket;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderIdentityDownstreamSyncResult> ApplyAsync(
        IReadOnlyList<PurchaseOrderItem> items,
        PurchaseOrderIdentitySnapshotField field,
        CancellationToken cancellationToken = default)
    {
        var result = new PurchaseOrderIdentityDownstreamSyncResult();
        if (items.Count == 0)
            return result;

        var byLineId = items
            .Where(i => !string.IsNullOrWhiteSpace(i.Id))
            .GroupBy(i => i.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        if (byLineId.Count == 0)
            return result;

        var lineIds = byLineId.Keys.ToList();
        foreach (var chunk in lineIds.Chunk(200))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunkList = chunk.ToList();
            result.ArrivalNoticesUpdated += await SyncArrivalNoticesAsync(chunkList, byLineId, field, result.Changes);
            result.StockInItemsUpdated += await SyncStockInItemsAsync(chunkList, byLineId, field, result.Changes);

            var stockItems = (await _stockItemRepo.FindAsync(s =>
                    s.PurchaseOrderItemId != null && chunkList.Contains(s.PurchaseOrderItemId)))
                .ToList();
            var stockItemIds = stockItems
                .Select(s => s.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            result.PackingItemsUpdated += await SyncPackingItemsAsync(
                stockItems, byLineId, field, result.Changes);
            result.CustomsDeclarationItemsUpdated += await SyncCustomsItemsAsync(
                stockItemIds, stockItems, byLineId, field, result.Changes);
            var stockItemFieldUpdates = await SyncStockItemsAsync(stockItems, byLineId, field, result.Changes);
            result.StockItemsUpdated += stockItemFieldUpdates;
            if (_rebucket != null && stockItems.Count > 0)
            {
                var rebucket = await _rebucket.EnsureAggregatesAsync(stockItems, cancellationToken);
                result.StockItemsMoved += rebucket.StockItemsMoved;
                result.StockAggregatesCreated += rebucket.StockAggregatesCreated;
                result.StockAggregatesRemoved += rebucket.StockAggregatesRemoved;
            }
        }

        _logger.LogInformation(
            "PO下游身份快照刷新: Field={Field} Lines={Lines} Notices={Notices} StockIn={StockIn} Packing={Packing} Customs={Customs} StockItem={StockItem} Moved={Moved} BucketsCreated={Buckets} BucketsRemoved={Removed}",
            field,
            byLineId.Count,
            result.ArrivalNoticesUpdated,
            result.StockInItemsUpdated,
            result.PackingItemsUpdated,
            result.CustomsDeclarationItemsUpdated,
            result.StockItemsUpdated,
            result.StockItemsMoved,
            result.StockAggregatesCreated,
            result.StockAggregatesRemoved);

        return result;
    }

    private async Task<int> SyncArrivalNoticesAsync(
        List<string> lineIds,
        IReadOnlyDictionary<string, PurchaseOrderItem> byLineId,
        PurchaseOrderIdentitySnapshotField field,
        List<PurchaseOrderIdentitySnapshotChangeDto> changes)
    {
        var rows = (await _notifyRepo.FindAsync(n => lineIds.Contains(n.PurchaseOrderItemId))).ToList();
        var updated = 0;
        foreach (var row in rows)
        {
            var lineId = row.PurchaseOrderItemId?.Trim();
            if (string.IsNullOrEmpty(lineId) || !byLineId.TryGetValue(lineId, out var item))
                continue;

            var target = field == PurchaseOrderIdentitySnapshotField.Pn
                ? Clip(item.PN, ArrivalPnMax)
                : Clip(item.Brand, ArrivalBrandMax);
            var current = field == PurchaseOrderIdentitySnapshotField.Pn ? row.Pn : row.Brand;
            if (TextEquals(current, target))
                continue;

            Remember(changes, "arrivalNotice", row.Id, row.NoticeCode, current, target);
            if (field == PurchaseOrderIdentitySnapshotField.Pn)
                row.Pn = target;
            else
                row.Brand = target;
            row.ModifyTime = DateTime.UtcNow;
            await _notifyRepo.UpdateAsync(row);
            updated++;
        }

        return updated;
    }

    private async Task<int> SyncStockInItemsAsync(
        List<string> lineIds,
        IReadOnlyDictionary<string, PurchaseOrderItem> byLineId,
        PurchaseOrderIdentitySnapshotField field,
        List<PurchaseOrderIdentitySnapshotChangeDto> changes)
    {
        var extends = (await _stockInItemExtendRepo.FindAsync(e =>
                e.PurchaseOrderItemId != null && lineIds.Contains(e.PurchaseOrderItemId)))
            .ToList();
        if (extends.Count == 0)
            return 0;

        var itemIds = extends.Select(e => e.Id).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        var items = (await _stockInItemRepo.FindAsync(i => itemIds.Contains(i.Id))).ToList();
        var byId = items.ToDictionary(i => i.Id, StringComparer.OrdinalIgnoreCase);
        var updated = 0;

        foreach (var ext in extends)
        {
            var lineId = ext.PurchaseOrderItemId?.Trim();
            if (string.IsNullOrEmpty(lineId) || !byLineId.TryGetValue(lineId, out var poItem))
                continue;
            if (!byId.TryGetValue(ext.Id, out var row))
                continue;

            var target = field == PurchaseOrderIdentitySnapshotField.Pn
                ? Clip(poItem.PN, 200)
                : Clip(poItem.Brand, 200);
            var current = field == PurchaseOrderIdentitySnapshotField.Pn ? row.PurchasePn : row.PurchaseBrand;
            if (TextEquals(current, target))
                continue;

            Remember(changes, "stockInItem", row.Id, row.StockInItemCode, current, target);
            if (field == PurchaseOrderIdentitySnapshotField.Pn)
                row.PurchasePn = target;
            else
                row.PurchaseBrand = target;
            row.ModifyTime = DateTime.UtcNow;
            await _stockInItemRepo.UpdateAsync(row);
            updated++;
        }

        return updated;
    }

    private async Task<int> SyncStockItemsAsync(
        List<StockItem> rows,
        IReadOnlyDictionary<string, PurchaseOrderItem> byLineId,
        PurchaseOrderIdentitySnapshotField field,
        List<PurchaseOrderIdentitySnapshotChangeDto> changes)
    {
        var updated = 0;
        foreach (var row in rows)
        {
            var lineId = row.PurchaseOrderItemId?.Trim();
            if (string.IsNullOrEmpty(lineId) || !byLineId.TryGetValue(lineId, out var poItem))
                continue;

            var target = field == PurchaseOrderIdentitySnapshotField.Pn
                ? Clip(poItem.PN, 200)
                : Clip(poItem.Brand, 200);
            var current = field == PurchaseOrderIdentitySnapshotField.Pn ? row.PurchasePn : row.PurchaseBrand;
            if (TextEquals(current, target))
                continue;

            Remember(changes, "stockItem", row.Id, row.StockItemCode, current, target);
            if (field == PurchaseOrderIdentitySnapshotField.Pn)
                row.PurchasePn = target;
            else
                row.PurchaseBrand = target;
            row.ModifyTime = DateTime.UtcNow;
            await _stockItemRepo.UpdateAsync(row);
            updated++;
        }

        return updated;
    }

    private async Task<int> SyncPackingItemsAsync(
        List<StockItem> stockItems,
        IReadOnlyDictionary<string, PurchaseOrderItem> byLineId,
        PurchaseOrderIdentitySnapshotField field,
        List<PurchaseOrderIdentitySnapshotChangeDto> changes)
    {
        var stockItemIds = stockItems
            .Select(s => s.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .ToList();
        if (stockItemIds.Count == 0)
            return 0;

        var poLineByStockItemId = stockItems
            .Where(s => !string.IsNullOrWhiteSpace(s.Id) && !string.IsNullOrWhiteSpace(s.PurchaseOrderItemId))
            .GroupBy(s => s.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().PurchaseOrderItemId!.Trim(), StringComparer.OrdinalIgnoreCase);

        var rows = (await _packingItemRepo.FindAsync(p =>
                p.StockItemId != null && stockItemIds.Contains(p.StockItemId)))
            .ToList();
        var updated = 0;
        foreach (var row in rows)
        {
            var stockItemId = row.StockItemId?.Trim();
            if (string.IsNullOrEmpty(stockItemId)
                || !poLineByStockItemId.TryGetValue(stockItemId, out var lineId)
                || !byLineId.TryGetValue(lineId, out var poItem))
                continue;

            var target = field == PurchaseOrderIdentitySnapshotField.Pn
                ? Clip(poItem.PN, 200)
                : Clip(poItem.Brand, 200);
            var current = field == PurchaseOrderIdentitySnapshotField.Pn ? row.Pn : row.Brand;
            if (TextEquals(current, target))
                continue;

            Remember(changes, "packingItem", row.Id, row.ItemCode, current, target);
            if (field == PurchaseOrderIdentitySnapshotField.Pn)
                row.Pn = target;
            else
                row.Brand = target;
            row.ModifyTime = DateTime.UtcNow;
            await _packingItemRepo.UpdateAsync(row);
            updated++;
        }

        return updated;
    }

    private async Task<int> SyncCustomsItemsAsync(
        List<string> stockItemIds,
        List<StockItem> stockItems,
        IReadOnlyDictionary<string, PurchaseOrderItem> byLineId,
        PurchaseOrderIdentitySnapshotField field,
        List<PurchaseOrderIdentitySnapshotChangeDto> changes)
    {
        var poLineByStockItemId = stockItems
            .Where(s => !string.IsNullOrWhiteSpace(s.Id) && !string.IsNullOrWhiteSpace(s.PurchaseOrderItemId))
            .GroupBy(s => s.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().PurchaseOrderItemId!.Trim(), StringComparer.OrdinalIgnoreCase);

        List<CustomsDeclarationItem> rows;
        if (stockItemIds.Count == 0)
            rows = new List<CustomsDeclarationItem>();
        else
            rows = (await _customsItemRepo.FindAsync(c =>
                    c.SourceStockItemId != null && stockItemIds.Contains(c.SourceStockItemId)))
                .ToList();

        var packingItemIds = (await _packingItemRepo.FindAsync(p =>
                p.StockItemId != null && stockItemIds.Contains(p.StockItemId)))
            .Select(p => p.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (packingItemIds.Count > 0)
        {
            var viaPacking = (await _customsItemRepo.FindAsync(c =>
                    c.PackingItemId != null && packingItemIds.Contains(c.PackingItemId)))
                .ToList();
            var seen = new HashSet<string>(rows.Select(r => r.Id), StringComparer.OrdinalIgnoreCase);
            foreach (var extra in viaPacking)
            {
                if (seen.Add(extra.Id))
                    rows.Add(extra);
            }
        }

        var packingById = packingItemIds.Count == 0
            ? new Dictionary<string, PackingItem>(StringComparer.OrdinalIgnoreCase)
            : (await _packingItemRepo.FindAsync(p => packingItemIds.Contains(p.Id)))
                .ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

        var updated = 0;
        foreach (var row in rows)
        {
            if (!TryResolvePoLineId(row, poLineByStockItemId, packingById, out var lineId)
                || !byLineId.TryGetValue(lineId, out var poItem))
                continue;

            var target = field == PurchaseOrderIdentitySnapshotField.Pn
                ? Clip(poItem.PN, 200)
                : Clip(poItem.Brand, 200);
            var current = field == PurchaseOrderIdentitySnapshotField.Pn ? row.PurchasePn : row.PurchaseBrand;
            if (TextEquals(current, target))
                continue;

            Remember(changes, "customsItem", row.Id, null, current, target);
            if (field == PurchaseOrderIdentitySnapshotField.Pn)
                row.PurchasePn = target;
            else
                row.PurchaseBrand = target;
            row.ModifyTime = DateTime.UtcNow;
            await _customsItemRepo.UpdateAsync(row);
            updated++;
        }

        return updated;
    }

    private static bool TryResolvePoLineId(
        CustomsDeclarationItem row,
        IReadOnlyDictionary<string, string> poLineByStockItemId,
        IReadOnlyDictionary<string, PackingItem> packingById,
        out string lineId)
    {
        lineId = string.Empty;
        var sourceStockId = row.SourceStockItemId?.Trim();
        if (!string.IsNullOrEmpty(sourceStockId)
            && poLineByStockItemId.TryGetValue(sourceStockId, out var fromStock)
            && !string.IsNullOrWhiteSpace(fromStock))
        {
            lineId = fromStock;
            return true;
        }

        var packingItemId = row.PackingItemId?.Trim();
        if (!string.IsNullOrEmpty(packingItemId)
            && packingById.TryGetValue(packingItemId, out var packing)
            && !string.IsNullOrWhiteSpace(packing.StockItemId)
            && poLineByStockItemId.TryGetValue(packing.StockItemId.Trim(), out var fromPacking)
            && !string.IsNullOrWhiteSpace(fromPacking))
        {
            lineId = fromPacking;
            return true;
        }

        return false;
    }

    private static void Remember(
        List<PurchaseOrderIdentitySnapshotChangeDto> changes,
        string nodeType,
        string nodeId,
        string? nodeCode,
        string? before,
        string? after)
    {
        if (changes.Count >= MaxLoggedChanges)
            return;
        changes.Add(new PurchaseOrderIdentitySnapshotChangeDto
        {
            NodeType = nodeType,
            NodeId = nodeId,
            NodeCode = nodeCode,
            Before = before,
            After = after
        });
    }

    private static string? Clip(string? value, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLen ? trimmed : trimmed[..maxLen];
    }

    private static bool TextEquals(string? a, string? b)
    {
        var x = string.IsNullOrWhiteSpace(a) ? null : a.Trim();
        var y = string.IsNullOrWhiteSpace(b) ? null : b.Trim();
        return string.Equals(x, y, StringComparison.Ordinal);
    }
}
