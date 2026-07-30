using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Packings;

/// <summary>
/// 装箱状态对账：有未删除且已完成的关联出库 → 100；否则若当前为 50/100 → 回退到 40。
/// </summary>
public sealed class PackingStatusReconcileService : IPackingStatusReconcileService
{
    private readonly ApplicationDbContext _db;

    public PackingStatusReconcileService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PackingStatusReconcileResult> ReconcileAsync(
        string packingId,
        string? actingUserId = null,
        string? excludingStockOutId = null,
        bool saveChanges = true,
        CancellationToken cancellationToken = default)
    {
        var results = await ReconcileManyAsync(
            new[] { packingId },
            actingUserId,
            excludingStockOutId,
            saveChanges,
            cancellationToken);
        return results.Count > 0
            ? results[0]
            : new PackingStatusReconcileResult { PackingId = packingId?.Trim() ?? string.Empty };
    }

    public async Task<IReadOnlyList<PackingStatusReconcileResult>> ReconcileManyAsync(
        IReadOnlyCollection<string> packingIds,
        string? actingUserId = null,
        string? excludingStockOutId = null,
        bool saveChanges = true,
        CancellationToken cancellationToken = default)
    {
        var ids = (packingIds ?? Array.Empty<string>())
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();

        if (ids.Count == 0)
            return Array.Empty<PackingStatusReconcileResult>();

        var excludeSo = excludingStockOutId?.Trim();
        var actor = ActingUserIdNormalizer.Normalize(actingUserId);
        var now = DateTime.UtcNow;
        var results = new List<PackingStatusReconcileResult>(ids.Count);

        var packings = await _db.Packings
            .Where(p => ids.Contains(p.Id) && !p.IsDeleted)
            .ToListAsync(cancellationToken);

        var packingById = packings.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

        // 有效已完成出库：明细 packing_id 命中，且出库主单未删除、状态为已出库(2)/完成类(4)
        var donePackingIdSet = await (
            from item in _db.StockOutItems
            join so in _db.StockOuts on item.StockOutId equals so.Id
            where !item.IsDeleted
                  && !so.IsDeleted
                  && item.PackingId != null
                  && ids.Contains(item.PackingId)
                  && (so.Status == 2 || so.Status == 4)
                  && (excludeSo == null || so.Id != excludeSo)
            select item.PackingId!
        ).Distinct().ToListAsync(cancellationToken);

        var hasDone = new HashSet<string>(donePackingIdSet, StringComparer.OrdinalIgnoreCase);

        foreach (var pid in ids)
        {
            if (!packingById.TryGetValue(pid, out var packing))
            {
                results.Add(new PackingStatusReconcileResult { PackingId = pid });
                continue;
            }

            var previous = packing.Status;
            var liveDone = hasDone.Contains(pid);
            var next = DeriveStatus(previous, liveDone);

            var result = new PackingStatusReconcileResult
            {
                PackingId = packing.Id,
                PackingCode = packing.Code,
                PreviousStatus = previous,
                CurrentStatus = next,
                HasLiveCompletedStockOut = liveDone
            };

            if (next != previous)
            {
                packing.Status = next;
                packing.ModifyTime = now;
                packing.ModifyByUserId = actor;
            }

            results.Add(result);
        }

        if (saveChanges)
            await _db.SaveChangesAsync(cancellationToken);

        return results;
    }

    /// <summary>
    /// 有有效已完成出库 → 100；无则仅从 50/100 回退到 40；其它状态保持。
    /// </summary>
    internal static short DeriveStatus(short current, bool hasLiveCompletedStockOut)
    {
        if (hasLiveCompletedStockOut)
            return PackingStatusCode.StockOutFinished;

        if (current == PackingStatusCode.StockOutFinished
            || current == PackingStatusCode.PendingStockOut)
            return PackingStatusCode.Ready;

        return current;
    }
}
