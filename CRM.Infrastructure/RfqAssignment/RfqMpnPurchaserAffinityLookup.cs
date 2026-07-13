using CRM.Core.Interfaces.RfqAssignment;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Quote;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.RfqAssignment;

public sealed class RfqMpnPurchaserAffinityLookup : IRfqMpnPurchaserAffinityLookup
{
    private static readonly short[] ExcludedPurchaseOrderStatuses = [-1, -2];

    private readonly ApplicationDbContext _db;

    public RfqMpnPurchaserAffinityLookup(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<string>> GetPurchasersFromPurchaseHistoryAsync(
        string mpn,
        IReadOnlySet<string> poolUserIds,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        if (maxCount <= 0 || poolUserIds.Count == 0)
            return Array.Empty<string>();

        var key = RfqMpnMatch.NormalizeKey(mpn);
        if (string.IsNullOrEmpty(key))
            return Array.Empty<string>();

        var rows = await (
            from item in _db.PurchaseOrderItems.AsNoTracking()
            join po in _db.PurchaseOrders.AsNoTracking() on item.PurchaseOrderId equals po.Id
            where !item.IsDeleted
                  && !po.IsDeleted
                  && item.PN != null
                  && po.PurchaseUserId != null
                  && !ExcludedPurchaseOrderStatuses.Contains(po.Status)
            select new HistoryRow(
                item.PN!,
                po.PurchaseUserId!,
                po.CreateTime,
                po.PurchaseOrderCode,
                po.Id)
        ).ToListAsync(cancellationToken);

        return RankDistinctPurchasers(
            rows.Where(r => RfqMpnMatch.IsExactMatch(r.Mpn, mpn) && poolUserIds.Contains(r.PurchaserUserId)),
            maxCount);
    }

    public async Task<IReadOnlyList<string>> GetPurchasersFromQuoteHistoryAsync(
        string mpn,
        IReadOnlySet<string> poolUserIds,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        if (maxCount <= 0 || poolUserIds.Count == 0)
            return Array.Empty<string>();

        var key = RfqMpnMatch.NormalizeKey(mpn);
        if (string.IsNullOrEmpty(key))
            return Array.Empty<string>();

        var closed = (short)QuoteMainStatus.Closed;
        var rows = await _db.Quotes.AsNoTracking()
            .Where(q => !q.IsDeleted
                        && q.Mpn != null
                        && q.PurchaseUserId != null
                        && q.Status != closed)
            .Select(q => new HistoryRow(
                q.Mpn!,
                q.PurchaseUserId!,
                q.CreateTime,
                q.QuoteCode,
                q.Id))
            .ToListAsync(cancellationToken);

        return RankDistinctPurchasers(
            rows.Where(r => RfqMpnMatch.IsExactMatch(r.Mpn, mpn) && poolUserIds.Contains(r.PurchaserUserId)),
            maxCount);
    }

    private static IReadOnlyList<string> RankDistinctPurchasers(IEnumerable<HistoryRow> matched, int maxCount) =>
        matched
            .GroupBy(r => r.PurchaserUserId, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var best = g
                    .OrderByDescending(x => x.EventTime)
                    .ThenByDescending(x => x.TieCode, StringComparer.OrdinalIgnoreCase)
                    .ThenByDescending(x => x.TieId, StringComparer.OrdinalIgnoreCase)
                    .First();
                return new RankedPurchaser(
                    g.Key,
                    best.EventTime,
                    best.TieCode,
                    best.TieId);
            })
            .OrderByDescending(x => x.EventTime)
            .ThenByDescending(x => x.TieCode, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(x => x.TieId, StringComparer.OrdinalIgnoreCase)
            .Take(maxCount)
            .Select(x => x.PurchaserUserId)
            .ToList();

    private sealed record HistoryRow(
        string Mpn,
        string PurchaserUserId,
        DateTime EventTime,
        string TieCode,
        string TieId);

    private sealed record RankedPurchaser(
        string PurchaserUserId,
        DateTime EventTime,
        string TieCode,
        string TieId);
}
