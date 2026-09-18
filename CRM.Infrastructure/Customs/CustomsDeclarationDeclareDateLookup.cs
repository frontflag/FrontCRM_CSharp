using CRM.Core.Constants;
using CRM.Core.Models.Customs;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Customs;

/// <summary>
/// 申报日期实时取报关装箱对应报关出库单的实际出库日期（状态=出库完成）；未完成则为空。
/// </summary>
public static class CustomsDeclarationDeclareDateLookup
{
    public sealed class DeclarationDateRow
    {
        public CustomsDeclaration Declaration { get; set; } = null!;
        public DateTime? DeclareDate { get; set; }
    }

    public static IQueryable<DeclarationDateRow> WithDeclareDate(
        IQueryable<CustomsDeclaration> declarations,
        ApplicationDbContext db)
    {
        return declarations.Select(d => new DeclarationDateRow
        {
            Declaration = d,
            DeclareDate = db.StockOuts
                .Where(so =>
                    !so.IsDeleted
                    && so.StockOutType == StockOutTypeCode.Customs
                    && so.Status == StockOutStatusCode.Completed
                    && so.StockOutDate != null
                    && db.Packings.Any(p =>
                        !p.IsDeleted
                        && ((d.PackingId != null && d.PackingId != "" && p.Id == d.PackingId)
                            || (p.CustomsDeclarationId != null && p.CustomsDeclarationId == d.Id))
                        && (so.SourceId == p.Id
                            || db.StockOutItems.Any(i =>
                                !i.IsDeleted
                                && i.StockOutId == so.Id
                                && i.PackingId != null
                                && i.PackingId == p.Id))))
                .Max(so => (DateTime?)so.StockOutDate)
        });
    }

    public static IQueryable<CustomsDeclaration> WhereDeclareDateRange(
        IQueryable<CustomsDeclaration> declarations,
        ApplicationDbContext db,
        DateTime? fromInclusive,
        DateTime? toExclusive)
    {
        var hasFrom = fromInclusive.HasValue;
        var fromVal = fromInclusive ?? default;
        var hasTo = toExclusive.HasValue;
        var toVal = toExclusive ?? default;

        return declarations.Where(d => db.StockOuts.Any(so =>
            !so.IsDeleted
            && so.StockOutType == StockOutTypeCode.Customs
            && so.Status == StockOutStatusCode.Completed
            && so.StockOutDate != null
            && (!hasFrom || so.StockOutDate >= fromVal)
            && (!hasTo || so.StockOutDate < toVal)
            && db.Packings.Any(p =>
                !p.IsDeleted
                && ((d.PackingId != null && d.PackingId != "" && p.Id == d.PackingId)
                    || (p.CustomsDeclarationId != null && p.CustomsDeclarationId == d.Id))
                && (so.SourceId == p.Id
                    || db.StockOutItems.Any(i =>
                        !i.IsDeleted
                        && i.StockOutId == so.Id
                        && i.PackingId != null
                        && i.PackingId == p.Id)))));
    }

    /// <summary>装箱单 Id → 出库完成报关出库单的实际出库日期（多条时取最晚）。</summary>
    public static async Task<IReadOnlyDictionary<string, DateTime>> LoadByPackingIdsAsync(
        ApplicationDbContext db,
        IReadOnlyCollection<string> packingIds,
        CancellationToken cancellationToken = default)
    {
        var ids = packingIds
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (ids.Count == 0)
            return new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        var headerHits = await db.StockOuts.AsNoTracking()
            .Where(so =>
                !so.IsDeleted
                && so.StockOutType == StockOutTypeCode.Customs
                && so.Status == StockOutStatusCode.Completed
                && so.StockOutDate != null
                && so.SourceId != null
                && ids.Contains(so.SourceId))
            .Select(so => new { PackingId = so.SourceId!, Date = so.StockOutDate!.Value })
            .ToListAsync(cancellationToken);

        var itemHits = await (
            from so in db.StockOuts.AsNoTracking()
            join i in db.StockOutItems.AsNoTracking() on so.Id equals i.StockOutId
            where !so.IsDeleted
                  && !i.IsDeleted
                  && so.StockOutType == StockOutTypeCode.Customs
                  && so.Status == StockOutStatusCode.Completed
                  && so.StockOutDate != null
                  && i.PackingId != null
                  && ids.Contains(i.PackingId)
            select new { PackingId = i.PackingId!, Date = so.StockOutDate!.Value }
        ).ToListAsync(cancellationToken);

        var map = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        foreach (var hit in headerHits.Concat(itemHits))
        {
            var pid = hit.PackingId.Trim();
            if (pid.Length == 0)
                continue;
            if (!map.TryGetValue(pid, out var existing) || hit.Date > existing)
                map[pid] = hit.Date;
        }

        return map;
    }

    public static DateTime? ForPacking(
        string? packingId,
        IReadOnlyDictionary<string, DateTime> dateByPacking)
    {
        var pid = packingId?.Trim();
        if (string.IsNullOrEmpty(pid))
            return null;
        return dateByPacking.TryGetValue(pid, out var dt) ? dt : null;
    }
}
