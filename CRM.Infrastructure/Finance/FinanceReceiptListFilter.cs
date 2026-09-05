using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Finance;

/// <summary>收款单列表筛选（分页与看板共用）。</summary>
internal static class FinanceReceiptListFilter
{
    public static async Task<IQueryable<FinanceReceipt>> BuildFilteredQueryAsync(
        ApplicationDbContext db,
        IDataPermissionService dataPermission,
        FinanceReceiptQueryRequest? request,
        CancellationToken cancellationToken)
    {
        var q = db.FinanceReceipts.AsNoTracking();
        q = await dataPermission.ApplyFinanceReceiptListDataScopeAsync(
            request?.CurrentUserId,
            q,
            db.SellOrders.AsNoTracking(),
            db.FinanceReceiptItems.AsNoTracking(),
            cancellationToken);

        if (request == null)
            return q;

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var k = request.Keyword.Trim().ToLowerInvariant();
            q = q.Where(r =>
                r.FinanceReceiptCode.ToLower().Contains(k) ||
                (r.CustomerName != null && r.CustomerName.ToLower().Contains(k)));
        }

        if (request.Status.HasValue)
            q = q.Where(r => r.Status == request.Status.Value);

        if (request.ReceiptPurpose.HasValue)
            q = ApplyReceiptPurposeFilter(db, q, request.ReceiptPurpose.Value);

        if (request.VerificationStatus.HasValue)
            q = ApplyVerificationStatusFilter(db, q, request.VerificationStatus.Value);

        if (request.StartDate.HasValue)
            q = q.Where(r => r.CreateTime >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            q = q.Where(r => r.CreateTime <= request.EndDate.Value.AddDays(1));

        if (request.ReceiptCurrency.HasValue)
        {
            var receiptCurrency = request.ReceiptCurrency.Value;
            q = q.Where(r => (short)r.ReceiptCurrency == receiptCurrency);
        }

        if (request.ReceiptDateFrom.HasValue)
        {
            var startUtc = PostgreSqlDateTime.ToUtc(request.ReceiptDateFrom.Value);
            q = q.Where(r => r.ReceiptDate != null && r.ReceiptDate >= startUtc);
        }

        if (request.ReceiptDateTo.HasValue)
        {
            var endExclusiveUtc = PostgreSqlDateTime.ToUtc(request.ReceiptDateTo.Value).AddDays(1);
            q = q.Where(r => r.ReceiptDate != null && r.ReceiptDate < endExclusiveUtc);
        }

        return q;
    }

    /// <summary>
    /// 整单用途：无明细视为普通(10)；有明细取 <c>Max(ReceiptPurpose)</c>（与列表列一致）。
    /// </summary>
    public static IQueryable<FinanceReceipt> ApplyReceiptPurposeFilter(
        ApplicationDbContext db,
        IQueryable<FinanceReceipt> q,
        short receiptPurpose)
    {
        var items = db.FinanceReceiptItems.AsNoTracking();
        return q.Where(r =>
            (items.Where(i => i.FinanceReceiptId == r.Id)
                .Select(i => (short?)i.ReceiptPurpose)
                .Max() ?? FinanceReceiptPurposeCode.Normal) == receiptPurpose);
    }

    /// <summary>
    /// 整单汇总：无明细→未核销；全部 0→未核销；全部 2→核销完成；否则→部分核销。
    /// </summary>
    public static IQueryable<FinanceReceipt> ApplyVerificationStatusFilter(
        ApplicationDbContext db,
        IQueryable<FinanceReceipt> q,
        short verificationStatus)
    {
        var items = db.FinanceReceiptItems.AsNoTracking();

        if (verificationStatus == FinanceVerificationStatusCode.Pending)
        {
            return q.Where(r =>
                !items.Any(i => i.FinanceReceiptId == r.Id)
                || items.Where(i => i.FinanceReceiptId == r.Id)
                    .All(i => i.VerificationStatus == FinanceVerificationStatusCode.Pending));
        }

        if (verificationStatus == FinanceVerificationStatusCode.Complete)
        {
            return q.Where(r =>
                items.Any(i => i.FinanceReceiptId == r.Id)
                && items.Where(i => i.FinanceReceiptId == r.Id)
                    .All(i => i.VerificationStatus == FinanceVerificationStatusCode.Complete));
        }

        if (verificationStatus == FinanceVerificationStatusCode.Partial)
        {
            return q.Where(r =>
                items.Any(i => i.FinanceReceiptId == r.Id)
                && items.Where(i => i.FinanceReceiptId == r.Id)
                    .Any(i => i.VerificationStatus != FinanceVerificationStatusCode.Pending)
                && items.Where(i => i.FinanceReceiptId == r.Id)
                    .Any(i => i.VerificationStatus != FinanceVerificationStatusCode.Complete));
        }

        return q;
    }

    public static short ResolveHeaderVerificationStatus(short minStatus, short maxStatus)
    {
        if (minStatus == maxStatus
            && (minStatus == FinanceVerificationStatusCode.Pending
                || minStatus == FinanceVerificationStatusCode.Complete))
            return minStatus;
        return FinanceVerificationStatusCode.Partial;
    }
}
