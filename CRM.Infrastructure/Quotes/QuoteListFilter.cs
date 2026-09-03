using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Quote;
using CRM.Core.Models.RFQ;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Quotes;

/// <summary>报价列表筛选（列表分页与看板共用）。</summary>
internal static class QuoteListFilter
{
    public static async Task<IQueryable<Quote>> BuildFilteredQuotesQueryAsync(
        ApplicationDbContext db,
        IDataPermissionService dataPermission,
        QuoteQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = db.Quotes.AsNoTracking().Where(x => !x.IsDeleted);
        var skipQuoteScope = request.ForRfqItemReference && !string.IsNullOrWhiteSpace(request.RfqItemId);
        if (!skipQuoteScope)
        {
            q = await dataPermission.ApplyQuoteListDataScopeAsync(
                request.CurrentUserId,
                q,
                db.RFQs.AsNoTracking(),
                db.RFQItems.AsNoTracking(),
                cancellationToken);
        }

        if (request.Status.HasValue)
            q = q.Where(x => x.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.RfqItemId))
        {
            var rid = request.RfqItemId.Trim();
            q = q.Where(x => x.RFQItemId != null && x.RFQItemId == rid);
        }

        q = ApplyKeywordToQuotes(db, q, request.Keyword);

        if (!string.IsNullOrWhiteSpace(request.ExactMpn))
        {
            var mpnKey = request.ExactMpn.Trim().ToLowerInvariant();
            q = q.Where(quote => quote.Mpn != null && quote.Mpn.ToLower() == mpnKey);
        }

        if (!string.IsNullOrWhiteSpace(request.ExactBrand))
        {
            var brandKey = request.ExactBrand.Trim().ToLowerInvariant();
            q = q.Where(quote =>
                db.QuoteItems.AsNoTracking().Any(qi =>
                    !qi.IsDeleted
                    && qi.QuoteId == quote.Id
                    && qi.Brand != null
                    && qi.Brand.ToLower() == brandKey));
        }

        if (QuoteAnalyticsDatasets.IsReportScope(request.AnalyticsDataset))
        {
            q = ApplyQuoteCreateDate(q, request.StartDate, request.EndDate);
            q = ApplyReportViewLens(db, q, request);
        }
        else
        {
            q = ApplyRfqCreateDateToQuotes(db, q, request.StartDate, request.EndDate);
        }

        return q;
    }

    /// <summary>方案 A：并联需求明细（keyword + 日期 + 数据权限；不含 quote status）。</summary>
    public static async Task<IQueryable<QuoteDemandJoin>> BuildParallelDemandQueryAsync(
        ApplicationDbContext db,
        IRbacService rbacService,
        IDataPermissionService dataPermission,
        IPurchaseQuoterPoolService purchaseQuoterPoolService,
        QuoteQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q =
            from item in db.RFQItems.AsNoTracking()
            where !item.IsDeleted
            join rfq in db.RFQs.AsNoTracking() on item.RfqId equals rfq.Id
            join cust in db.Customers.AsNoTracking() on rfq.CustomerId equals cust.Id into custGroup
            from cust in custGroup.DefaultIfEmpty()
            join su in db.Users.AsNoTracking() on rfq.SalesUserId equals su.Id into suGroup
            from su in suGroup.DefaultIfEmpty()
            select new QuoteDemandJoin
            {
                Item = item,
                Rfq = rfq,
                Customer = cust,
                SalesUser = su
            };

        q = await ApplyDemandDataScopeAsync(db, rbacService, dataPermission, purchaseQuoterPoolService, request, q, cancellationToken);
        q = ApplyKeywordToDemand(db, q, request.Keyword);

        if (QuoteAnalyticsDatasets.IsReportScope(request.AnalyticsDataset))
            q = ApplyReportScopeDemandFilter(db, q, request);
        else
            q = ApplyRfqCreateDateToDemand(q, request.StartDate, request.EndDate);

        return q;
    }

    private static IQueryable<Quote> ApplyKeywordToQuotes(
        ApplicationDbContext db,
        IQueryable<Quote> q,
        string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return q;

        var k = keyword.Trim();
        var kl = k.ToLower();
        return q.Where(quote =>
            (quote.QuoteCode != null && quote.QuoteCode.ToLower().Contains(kl)) ||
            (quote.Mpn != null && quote.Mpn.ToLower().Contains(kl)) ||
            (quote.Remark != null && quote.Remark.ToLower().Contains(kl)) ||
            db.Set<RFQ>().Any(r =>
                r.Id == quote.RFQId &&
                r.RfqCode != null &&
                r.RfqCode.ToLower().Contains(kl)) ||
            db.Set<CustomerInfo>().Any(c =>
                quote.CustomerId != null &&
                c.Id == quote.CustomerId &&
                ((c.OfficialName != null && c.OfficialName.ToLower().Contains(kl)) ||
                 (c.NickName != null && c.NickName.ToLower().Contains(kl)) ||
                 (c.CustomerCode != null && c.CustomerCode.ToLower().Contains(kl)))) ||
            db.Set<RFQ>().Any(r =>
                r.Id == quote.RFQId &&
                r.CustomerId != null &&
                db.Set<CustomerInfo>().Any(c2 =>
                    c2.Id == r.CustomerId &&
                    ((c2.OfficialName != null && c2.OfficialName.ToLower().Contains(kl)) ||
                     (c2.NickName != null && c2.NickName.ToLower().Contains(kl)) ||
                     (c2.CustomerCode != null && c2.CustomerCode.ToLower().Contains(kl))))) ||
            db.Users.Any(u =>
                quote.SalesUserId != null &&
                u.Id == quote.SalesUserId &&
                u.UserName != null &&
                u.UserName.ToLower().Contains(kl)) ||
            db.Users.Any(u =>
                quote.PurchaseUserId != null &&
                u.Id == quote.PurchaseUserId &&
                u.UserName != null &&
                u.UserName.ToLower().Contains(kl)) ||
            db.QuoteItems.Any(qi =>
                !qi.IsDeleted &&
                qi.QuoteId == quote.Id &&
                ((qi.Brand != null && qi.Brand.ToLower().Contains(kl)) ||
                 (qi.Mpn != null && qi.Mpn.ToLower().Contains(kl)))));
    }

    private static IQueryable<Quote> ApplyRfqCreateDateToQuotes(
        ApplicationDbContext db,
        IQueryable<Quote> q,
        DateTime? startDate,
        DateTime? endDate)
    {
        if (startDate.HasValue)
        {
            var start = SalesAnalyticsDateFilter.ToUtcDateStart(startDate.Value);
            q = q.Where(quote =>
                quote.RFQId != null &&
                db.RFQs.Any(r => r.Id == quote.RFQId && r.CreateTime >= start));
        }

        if (endDate.HasValue)
        {
            var endExclusive = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(endDate.Value);
            q = q.Where(quote =>
                quote.RFQId != null &&
                db.RFQs.Any(r => r.Id == quote.RFQId && r.CreateTime < endExclusive));
        }

        return q;
    }

    private static IQueryable<Quote> ApplyQuoteCreateDate(
        IQueryable<Quote> q,
        DateTime? startDate,
        DateTime? endDate)
    {
        if (startDate.HasValue)
        {
            var start = SalesAnalyticsDateFilter.ToUtcDateStart(startDate.Value);
            q = q.Where(quote => quote.CreateTime >= start);
        }

        if (endDate.HasValue)
        {
            var endExclusive = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(endDate.Value);
            q = q.Where(quote => quote.CreateTime < endExclusive);
        }

        return q;
    }

    private static IQueryable<Quote> ApplyReportViewLens(
        ApplicationDbContext db,
        IQueryable<Quote> q,
        QuoteQueryRequest request)
    {
        var viewLevel = (request.AnalyticsViewLevel ?? string.Empty).Trim().ToLowerInvariant();
        if (viewLevel == SalesAnalyticsViewLevels.Personal
            && !string.IsNullOrWhiteSpace(request.PurchaseUserId))
        {
            var uid = request.PurchaseUserId.Trim();
            return q.Where(quote => quote.PurchaseUserId == uid);
        }

        if (viewLevel == SalesAnalyticsViewLevels.Department)
        {
            var deptId = request.AnalyticsDepartmentId?.Trim();
            if (string.IsNullOrWhiteSpace(deptId))
                return q;

            if (string.Equals(deptId, PurchaseAnalyticsScopeValidator.UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase))
            {
                var withPrimary = db.RbacUserDepartments.AsNoTracking()
                    .Where(ud => ud.IsPrimary)
                    .Select(ud => ud.UserId);
                return q.Where(quote =>
                    quote.PurchaseUserId == null
                    || !withPrimary.Contains(quote.PurchaseUserId));
            }

            var userIdsInDept = db.RbacUserDepartments.AsNoTracking()
                .Where(ud => ud.IsPrimary && ud.DepartmentId == deptId)
                .Select(ud => ud.UserId);
            return q.Where(quote => quote.PurchaseUserId != null && userIdsInDept.Contains(quote.PurchaseUserId));
        }

        return q;
    }

    /// <summary>reportScope：并联需求仅保留在报价创建日 + 采购员透镜范围内有报价的需求行。</summary>
    private static IQueryable<QuoteDemandJoin> ApplyReportScopeDemandFilter(
        ApplicationDbContext db,
        IQueryable<QuoteDemandJoin> q,
        QuoteQueryRequest request)
    {
        DateTime? start = null;
        DateTime? endExclusive = null;
        if (request.StartDate.HasValue)
            start = SalesAnalyticsDateFilter.ToUtcDateStart(request.StartDate.Value);
        if (request.EndDate.HasValue)
            endExclusive = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(request.EndDate.Value);

        var viewLevel = (request.AnalyticsViewLevel ?? string.Empty).Trim().ToLowerInvariant();
        var personalUserId = request.PurchaseUserId?.Trim();
        var deptId = request.AnalyticsDepartmentId?.Trim();

        return q.Where(x => db.Quotes.Any(quote =>
            !quote.IsDeleted
            && quote.RFQItemId == x.Item.Id
            && (!start.HasValue || quote.CreateTime >= start.Value)
            && (!endExclusive.HasValue || quote.CreateTime < endExclusive.Value)
            && (
                viewLevel != SalesAnalyticsViewLevels.Personal
                || string.IsNullOrWhiteSpace(personalUserId)
                || quote.PurchaseUserId == personalUserId)
            && (
                viewLevel != SalesAnalyticsViewLevels.Department
                || string.IsNullOrWhiteSpace(deptId)
                || (
                    string.Equals(deptId, PurchaseAnalyticsScopeValidator.UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase)
                        ? quote.PurchaseUserId == null
                          || !db.RbacUserDepartments.Any(ud =>
                              ud.IsPrimary && ud.UserId == quote.PurchaseUserId)
                        : quote.PurchaseUserId != null
                          && db.RbacUserDepartments.Any(ud =>
                              ud.IsPrimary && ud.DepartmentId == deptId && ud.UserId == quote.PurchaseUserId)))));
    }

    private static IQueryable<QuoteDemandJoin> ApplyKeywordToDemand(
        ApplicationDbContext db,
        IQueryable<QuoteDemandJoin> q,
        string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return q;

        var kl = keyword.Trim().ToLowerInvariant();
        return q.Where(x =>
            (x.Rfq.RfqCode != null && x.Rfq.RfqCode.ToLower().Contains(kl)) ||
            (x.Item.Mpn != null && x.Item.Mpn.ToLower().Contains(kl)) ||
            (x.Item.Brand != null && x.Item.Brand.ToLower().Contains(kl)) ||
            (x.Item.CustomerMpn != null && x.Item.CustomerMpn.ToLower().Contains(kl)) ||
            (x.Customer != null &&
             ((x.Customer.OfficialName != null && x.Customer.OfficialName.ToLower().Contains(kl)) ||
              (x.Customer.NickName != null && x.Customer.NickName.ToLower().Contains(kl)) ||
              (x.Customer.CustomerCode != null && x.Customer.CustomerCode.ToLower().Contains(kl)))) ||
            (x.SalesUser != null &&
             ((x.SalesUser.UserName != null && x.SalesUser.UserName.ToLower().Contains(kl)) ||
              (x.SalesUser.RealName != null && x.SalesUser.RealName.ToLower().Contains(kl)))) ||
            db.Users.Any(u =>
                (x.Item.AssignedPurchaserUserId1 != null && u.Id == x.Item.AssignedPurchaserUserId1 &&
                 u.UserName != null && u.UserName.ToLower().Contains(kl)) ||
                (x.Item.AssignedPurchaserUserId2 != null && u.Id == x.Item.AssignedPurchaserUserId2 &&
                 u.UserName != null && u.UserName.ToLower().Contains(kl))));
    }

    private static IQueryable<QuoteDemandJoin> ApplyRfqCreateDateToDemand(
        IQueryable<QuoteDemandJoin> q,
        DateTime? startDate,
        DateTime? endDate)
    {
        if (startDate.HasValue)
        {
            var start = SalesAnalyticsDateFilter.ToUtcDateStart(startDate.Value);
            q = q.Where(x => x.Rfq.CreateTime >= start);
        }

        if (endDate.HasValue)
        {
            var endExclusive = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(endDate.Value);
            q = q.Where(x => x.Rfq.CreateTime < endExclusive);
        }

        return q;
    }

    private static async Task<IQueryable<QuoteDemandJoin>> ApplyDemandDataScopeAsync(
        ApplicationDbContext db,
        IRbacService rbacService,
        IDataPermissionService dataPermission,
        IPurchaseQuoterPoolService purchaseQuoterPoolService,
        QuoteQueryRequest request,
        IQueryable<QuoteDemandJoin> q,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentUserId))
            return q;

        var summary = await rbacService.GetUserPermissionSummaryAsync(request.CurrentUserId.Trim());
        if (!RfqItemListDataScopeRules.ShouldApplyJobPageScope(summary))
            return q;

        if (summary.SaleDataScope == 4 && summary.PurchaseDataScope == 4)
            return q.Where(_ => false);

        var protectionMinutes = await purchaseQuoterPoolService.GetDemandProtectionMinutesAsync(cancellationToken);
        var protectionCutoffUtc = RfqDemandProtectionRules.ProtectionCutoffUtc(protectionMinutes, DateTime.UtcNow);
        var protectionPoolEnabled = RfqDemandProtectionRules.CanParticipateInProtectionPool(summary);

        HashSet<string>? saleAllow = null;
        if (summary.SaleDataScope == 2 || summary.SaleDataScope == 3)
            saleAllow = await dataPermission.GetAllowedUserIdsForDataScopeAsync(
                summary,
                includeChildren: summary.SaleDataScope == 3,
                cancellationToken);

        HashSet<string>? purchaseAllow = null;
        if (summary.PurchaseDataScope == 2 || summary.PurchaseDataScope == 3)
            purchaseAllow = await dataPermission.GetAllowedUserIdsForDataScopeAsync(
                summary,
                includeChildren: summary.PurchaseDataScope == 3,
                cancellationToken);

        var uid = request.CurrentUserId.Trim();

        return q.Where(x =>
            (
                summary.SaleDataScope != 4 &&
                (
                    (summary.SaleDataScope == 1 && x.Rfq.SalesUserId != null && x.Rfq.SalesUserId == uid) ||
                    ((summary.SaleDataScope == 2 || summary.SaleDataScope == 3) &&
                     saleAllow != null &&
                     x.Rfq.SalesUserId != null &&
                     saleAllow.Contains(x.Rfq.SalesUserId))
                )
            )
            ||
            (
                summary.PurchaseDataScope != 4 &&
                (
                    (summary.PurchaseDataScope == 1 &&
                     (x.Item.AssignedPurchaserUserId1 == uid ||
                      x.Item.AssignedPurchaserUserId2 == uid)) ||
                    ((summary.PurchaseDataScope == 2 || summary.PurchaseDataScope == 3) &&
                     purchaseAllow != null &&
                     ((!string.IsNullOrWhiteSpace(x.Item.AssignedPurchaserUserId1) &&
                       purchaseAllow.Contains(x.Item.AssignedPurchaserUserId1!)) ||
                      (!string.IsNullOrWhiteSpace(x.Item.AssignedPurchaserUserId2) &&
                       purchaseAllow.Contains(x.Item.AssignedPurchaserUserId2!)))) ||
                    (protectionPoolEnabled &&
                     (protectionMinutes <= 0 || x.Item.CreateTime <= protectionCutoffUtc))
                )
            ));
    }
}

internal sealed class QuoteDemandJoin
{
    public RFQItem Item { get; init; } = null!;
    public RFQ Rfq { get; init; } = null!;
    public CustomerInfo? Customer { get; init; }
    public User? SalesUser { get; init; }
}
