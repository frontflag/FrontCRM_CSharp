using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Packings;

/// <summary>
/// 装箱状态对账：有未删除且已完成的关联出库 → 100；否则若当前为 50/100 → 回退到 40。
/// 关联口径：明细 packing_id，或出库头 SourceId=装箱单（按箱出库）。
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
        // excludeSo 在内存按 OrdinalIgnoreCase 过滤，避免 SQL 字符串大小写导致漏排
        var doneItemRows = await (
            from item in _db.StockOutItems
            join so in _db.StockOuts on item.StockOutId equals so.Id
            where !item.IsDeleted
                  && !so.IsDeleted
                  && item.PackingId != null
                  && ids.Contains(item.PackingId)
                  && (so.Status == 2 || so.Status == 4)
            select new { PackingId = item.PackingId!, StockOutId = so.Id, so.StockOutCode }
        ).ToListAsync(cancellationToken);

        // 按箱出库头 SourceId=装箱单主键（明细可能未写 packing_id）
        var doneSourceRows = await _db.StockOuts
            .Where(so =>
                !so.IsDeleted
                && so.SourceId != null
                && ids.Contains(so.SourceId)
                && (so.Status == 2 || so.Status == 4))
            .Select(so => new { PackingId = so.SourceId!, StockOutId = so.Id, so.StockOutCode })
            .ToListAsync(cancellationToken);

        var blockingByPacking = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in doneItemRows.Concat(doneSourceRows))
        {
            if (!string.IsNullOrEmpty(excludeSo)
                && string.Equals(row.StockOutId, excludeSo, StringComparison.OrdinalIgnoreCase))
                continue;

            var pid = row.PackingId.Trim();
            if (!blockingByPacking.TryGetValue(pid, out var codes))
            {
                codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                blockingByPacking[pid] = codes;
            }

            var code = row.StockOutCode?.Trim();
            if (!string.IsNullOrEmpty(code))
                codes.Add(code);
            else
                codes.Add(row.StockOutId.Trim());
        }

        foreach (var pid in ids)
        {
            if (!packingById.TryGetValue(pid, out var packing))
            {
                results.Add(new PackingStatusReconcileResult { PackingId = pid });
                continue;
            }

            var previous = packing.Status;
            var blockingCodes = blockingByPacking.TryGetValue(pid, out var set)
                ? set.OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList()
                : new List<string>();
            var liveDone = blockingCodes.Count > 0;
            var next = DeriveStatus(previous, liveDone);

            var result = new PackingStatusReconcileResult
            {
                PackingId = packing.Id,
                PackingCode = packing.Code,
                PreviousStatus = previous,
                CurrentStatus = next,
                HasLiveCompletedStockOut = liveDone,
                BlockingStockOutCodes = blockingCodes
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
    public static short DeriveStatus(short current, bool hasLiveCompletedStockOut)
    {
        if (hasLiveCompletedStockOut)
            return PackingStatusCode.StockOutFinished;

        if (current == PackingStatusCode.StockOutFinished
            || current == PackingStatusCode.PendingStockOut)
            return PackingStatusCode.Ready;

        return current;
    }
}
