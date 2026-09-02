using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Sales;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services;

/// <inheritdoc />
public sealed class SalesOrderIdentityDownstreamSyncService : ISalesOrderIdentityDownstreamSyncService
{
    private const int MaxLoggedChanges = 80;
    private const int TextMax = 200;

    private readonly IRepository<StockOutRequest> _notifyRepo;
    private readonly IRepository<PackingItem> _packingItemRepo;
    private readonly IRepository<PackingItemExtend> _packingItemExtendRepo;
    private readonly IRepository<FinanceReceivable> _receivableRepo;
    private readonly ILogger<SalesOrderIdentityDownstreamSyncService> _logger;

    public SalesOrderIdentityDownstreamSyncService(
        IRepository<StockOutRequest> notifyRepo,
        IRepository<PackingItem> packingItemRepo,
        IRepository<PackingItemExtend> packingItemExtendRepo,
        IRepository<FinanceReceivable> receivableRepo,
        ILogger<SalesOrderIdentityDownstreamSyncService> logger)
    {
        _notifyRepo = notifyRepo;
        _packingItemRepo = packingItemRepo;
        _packingItemExtendRepo = packingItemExtendRepo;
        _receivableRepo = receivableRepo;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SalesOrderIdentityDownstreamSyncResult> ApplyAsync(
        IReadOnlyList<SellOrderItem> items,
        SalesOrderIdentitySnapshotField field,
        CancellationToken cancellationToken = default)
    {
        var result = new SalesOrderIdentityDownstreamSyncResult();
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
            result.StockOutNotifiesUpdated += await SyncNotifiesAsync(chunkList, byLineId, field, result.Changes);
            result.PackingItemsUpdated += await SyncPackingItemsAsync(chunkList, byLineId, field, result.Changes);
            result.PackingItemExtendsUpdated += await SyncPackingItemExtendsAsync(
                chunkList, byLineId, field, result.Changes);
            result.ReceivablesUpdated += await SyncReceivablesAsync(chunkList, byLineId, field, result.Changes);
        }

        _logger.LogInformation(
            "SO下游身份快照刷新: Field={Field} Lines={Lines} Notifies={Notifies} Packing={Packing} PackingExtend={PackingExtend} Receivable={Receivable}",
            field,
            byLineId.Count,
            result.StockOutNotifiesUpdated,
            result.PackingItemsUpdated,
            result.PackingItemExtendsUpdated,
            result.ReceivablesUpdated);

        return result;
    }

    private async Task<int> SyncNotifiesAsync(
        List<string> lineIds,
        IReadOnlyDictionary<string, SellOrderItem> byLineId,
        SalesOrderIdentitySnapshotField field,
        List<SalesOrderIdentitySnapshotChangeDto> changes)
    {
        var rows = (await _notifyRepo.FindAsync(n =>
                !n.IsDeleted && lineIds.Contains(n.SalesOrderItemId)))
            .ToList();
        var updated = 0;
        foreach (var row in rows)
        {
            var lineId = row.SalesOrderItemId?.Trim();
            if (string.IsNullOrEmpty(lineId) || !byLineId.TryGetValue(lineId, out var item))
                continue;

            if (field == SalesOrderIdentitySnapshotField.Pn)
            {
                var target = Clip(item.PN, TextMax) ?? string.Empty;
                if (TextEquals(row.MaterialCode, target))
                    continue;
                Remember(changes, "stockOutNotify", row.Id, row.RequestCode, row.MaterialCode, target);
                row.MaterialCode = target;
            }
            else
            {
                var target = Clip(item.Brand, TextMax);
                if (TextEquals(row.MaterialName, target))
                    continue;
                Remember(changes, "stockOutNotify", row.Id, row.RequestCode, row.MaterialName, target);
                row.MaterialName = target;
            }

            row.ModifyTime = DateTime.UtcNow;
            await _notifyRepo.UpdateAsync(row);
            updated++;
        }

        return updated;
    }

    private async Task<int> SyncPackingItemsAsync(
        List<string> lineIds,
        IReadOnlyDictionary<string, SellOrderItem> byLineId,
        SalesOrderIdentitySnapshotField field,
        List<SalesOrderIdentitySnapshotChangeDto> changes)
    {
        var rows = (await _packingItemRepo.FindAsync(p =>
                !p.IsDeleted
                && p.SellOrderItemId != null
                && lineIds.Contains(p.SellOrderItemId)))
            .ToList();
        var updated = 0;
        foreach (var row in rows)
        {
            var lineId = row.SellOrderItemId?.Trim();
            if (string.IsNullOrEmpty(lineId) || !byLineId.TryGetValue(lineId, out var item))
                continue;

            var target = field == SalesOrderIdentitySnapshotField.Pn
                ? Clip(item.PN, TextMax)
                : Clip(item.Brand, TextMax);
            var current = field == SalesOrderIdentitySnapshotField.Pn ? row.Pn : row.Brand;
            if (TextEquals(current, target))
                continue;

            Remember(changes, "packingItem", row.Id, row.ItemCode, current, target);
            if (field == SalesOrderIdentitySnapshotField.Pn)
                row.Pn = target;
            else
                row.Brand = target;
            row.ModifyTime = DateTime.UtcNow;
            await _packingItemRepo.UpdateAsync(row);
            updated++;
        }

        return updated;
    }

    private async Task<int> SyncPackingItemExtendsAsync(
        List<string> lineIds,
        IReadOnlyDictionary<string, SellOrderItem> byLineId,
        SalesOrderIdentitySnapshotField field,
        List<SalesOrderIdentitySnapshotChangeDto> changes)
    {
        var rows = (await _packingItemExtendRepo.FindAsync(e =>
                !e.IsDeleted
                && e.SellOrderItemId != null
                && lineIds.Contains(e.SellOrderItemId)))
            .ToList();
        var updated = 0;
        foreach (var row in rows)
        {
            var lineId = row.SellOrderItemId?.Trim();
            if (string.IsNullOrEmpty(lineId) || !byLineId.TryGetValue(lineId, out var item))
                continue;

            var target = field == SalesOrderIdentitySnapshotField.Pn
                ? Clip(item.CustomerPn, TextMax)
                : Clip(item.CustomerBrand, TextMax);
            var current = field == SalesOrderIdentitySnapshotField.Pn ? row.CustomerPn : row.CustomerBrand;
            if (TextEquals(current, target))
                continue;

            Remember(changes, "packingItemExtend", row.Id, null, current, target);
            if (field == SalesOrderIdentitySnapshotField.Pn)
                row.CustomerPn = target;
            else
                row.CustomerBrand = target;
            row.ModifyTime = DateTime.UtcNow;
            await _packingItemExtendRepo.UpdateAsync(row);
            updated++;
        }

        return updated;
    }

    private async Task<int> SyncReceivablesAsync(
        List<string> lineIds,
        IReadOnlyDictionary<string, SellOrderItem> byLineId,
        SalesOrderIdentitySnapshotField field,
        List<SalesOrderIdentitySnapshotChangeDto> changes)
    {
        var rows = (await _receivableRepo.FindAsync(r =>
                !r.IsDeleted
                && r.SellOrderItemId != null
                && lineIds.Contains(r.SellOrderItemId)))
            .ToList();
        var updated = 0;
        foreach (var row in rows)
        {
            var lineId = row.SellOrderItemId?.Trim();
            if (string.IsNullOrEmpty(lineId) || !byLineId.TryGetValue(lineId, out var item))
                continue;

            var target = field == SalesOrderIdentitySnapshotField.Pn
                ? Clip(item.PN, TextMax)
                : Clip(item.Brand, TextMax);
            var current = field == SalesOrderIdentitySnapshotField.Pn ? row.PN : row.Brand;
            if (TextEquals(current, target))
                continue;

            Remember(changes, "receivable", row.Id, row.ReceivableCode, current, target);
            if (field == SalesOrderIdentitySnapshotField.Pn)
                row.PN = target;
            else
                row.Brand = target;
            await _receivableRepo.UpdateAsync(row);
            updated++;
        }

        return updated;
    }

    private static void Remember(
        List<SalesOrderIdentitySnapshotChangeDto> changes,
        string nodeType,
        string nodeId,
        string? nodeCode,
        string? before,
        string? after)
    {
        if (changes.Count >= MaxLoggedChanges)
            return;
        changes.Add(new SalesOrderIdentitySnapshotChangeDto
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
