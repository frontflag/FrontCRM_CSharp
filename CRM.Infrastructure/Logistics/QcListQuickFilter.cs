using CRM.Core.Constants;
using CRM.Core.Models.Document;
using CRM.Core.Models.Inventory;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Logistics;

internal static class QcListQuickFilter
{
    private const short StatusRejected = -1;
    private const short StatusPartial = 10;
    private const short StatusPassed = 100;
    private const string QcDocumentBizType = "QC";

    public static IQueryable<QCInfo> Apply(ApplicationDbContext db, IQueryable<QCInfo> q, string preset)
    {
        if (!QcListQuickFilterCodes.IsKnown(preset))
            return q;

        var today = DateTime.UtcNow.Date;

        return preset.Trim() switch
        {
            QcListQuickFilterCodes.QcToday => WhereCreateOnDay(q, today),
            QcListQuickFilterCodes.QcTodayYesterday => WhereCreateInRange(q, today.AddDays(-1), today),
            QcListQuickFilterCodes.QcWithin3Days => WhereCreateInRange(q, today.AddDays(-2), today),
            QcListQuickFilterCodes.QcWithin7Days => WhereCreateInRange(q, today.AddDays(-6), today),
            QcListQuickFilterCodes.QcWithin30Days => WhereCreateInRange(q, today.AddDays(-29), today),

            QcListQuickFilterCodes.StatusPassed => q.Where(x => x.Status == StatusPassed),
            QcListQuickFilterCodes.StatusPartial => q.Where(x => x.Status == StatusPartial && x.ModifyTime != null),
            QcListQuickFilterCodes.StatusRejected => q.Where(x => x.Status == StatusRejected),

            QcListQuickFilterCodes.HasQcImages => WhereHasQcImages(db, q),
            QcListQuickFilterCodes.NoQcImages => WhereNoQcImages(db, q),

            _ => q
        };
    }

    /// <summary>可翻译为 SQL 的质检图片文档查询（mime / 扩展名 / 文件名后缀，与前端 qcImageDocument 口径一致）。</summary>
    private static IQueryable<UploadDocument> QcImageDocuments(ApplicationDbContext db) =>
        db.UploadDocuments
            .Where(d => d.BizType == QcDocumentBizType
                        && (
                            (d.MimeType != null && d.MimeType.ToLower().StartsWith("image/"))
                            || (d.FileExtension != null && (
                                d.FileExtension.ToLower() == ".jpg"
                                || d.FileExtension.ToLower() == ".jpeg"
                                || d.FileExtension.ToLower() == ".png"
                                || d.FileExtension.ToLower() == ".gif"
                                || d.FileExtension.ToLower() == ".webp"
                                || d.FileExtension.ToLower() == ".bmp"))
                            || (d.OriginalFileName != null && (
                                d.OriginalFileName.ToLower().EndsWith(".jpg")
                                || d.OriginalFileName.ToLower().EndsWith(".jpeg")
                                || d.OriginalFileName.ToLower().EndsWith(".png")
                                || d.OriginalFileName.ToLower().EndsWith(".gif")
                                || d.OriginalFileName.ToLower().EndsWith(".webp")
                                || d.OriginalFileName.ToLower().EndsWith(".bmp")))));

    /// <summary>可翻译为 SQL 的质检图片 BizId 集合。</summary>
    private static IQueryable<string> QcImageDocumentBizIds(ApplicationDbContext db) =>
        QcImageDocuments(db).Select(d => d.BizId).Distinct();

    /// <summary>对本页质检 Id 批量 GROUP BY 图片数量；无图 Id 不在字典中（调用方视为 0）。</summary>
    public static async Task<Dictionary<string, int>> CountQcImagesByBizIdsAsync(
        ApplicationDbContext db,
        IReadOnlyCollection<string> qcIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (qcIds == null || qcIds.Count == 0) return result;

        var idSet = qcIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (idSet.Count == 0) return result;

        var rows = await QcImageDocuments(db)
            .Where(d => idSet.Contains(d.BizId))
            .GroupBy(d => d.BizId)
            .Select(g => new { BizId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.BizId)) continue;
            result[row.BizId] = row.Count;
        }

        return result;
    }

    private static IQueryable<QCInfo> WhereHasQcImages(ApplicationDbContext db, IQueryable<QCInfo> q)
    {
        var imageQcIds = QcImageDocumentBizIds(db);
        return q.Where(x => imageQcIds.Contains(x.Id));
    }

    private static IQueryable<QCInfo> WhereNoQcImages(ApplicationDbContext db, IQueryable<QCInfo> q)
    {
        var imageQcIds = QcImageDocumentBizIds(db);
        return q.Where(x => !imageQcIds.Contains(x.Id));
    }

    private static IQueryable<QCInfo> WhereCreateOnDay(IQueryable<QCInfo> q, DateTime day)
    {
        var end = day.AddDays(1);
        return q.Where(x => x.CreateTime >= day && x.CreateTime < end);
    }

    private static IQueryable<QCInfo> WhereCreateInRange(
        IQueryable<QCInfo> q,
        DateTime fromInclusive,
        DateTime toInclusive)
    {
        var end = toInclusive.AddDays(1);
        return q.Where(x => x.CreateTime >= fromInclusive && x.CreateTime < end);
    }
}
