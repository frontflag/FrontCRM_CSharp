using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.RFQ;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.RfqListQueries;

/// <summary>需求明细列表筛选（列表分页与看板共用）。</summary>
internal static partial class RfqItemListFilter
{
    public static async Task<IQueryable<RfqItemListJoin>> BuildFilteredJoinQueryAsync(
        ApplicationDbContext db,
        IRbacService rbacService,
        IDataPermissionService dataPermission,
        IPurchaseQuoterPoolService purchaseQuoterPoolService,
        RFQItemQueryRequest request,
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
            select new RfqItemListJoin
            {
                Item = item,
                Rfq = rfq,
                Customer = cust,
                SalesUser = su
            };

        HashSet<string>? referenceCustomerReveal = null;
        var referenceCustomerSelfOnly = false;
        var referenceRestrictCustomerSearch = false;

        if (!string.IsNullOrWhiteSpace(request.CurrentUserId))
        {
            var summary = await rbacService.GetUserPermissionSummaryAsync(request.CurrentUserId.Trim());
            if (request.ForRfqItemReference)
            {
                if (!RfqItemReferenceAccessRules.CanEnterPage(summary))
                    q = q.Where(_ => false);
                else if (request.CanViewCustomerInList &&
                         RfqItemReferenceAccessRules.NeedsSalespersonCustomerMask(summary))
                {
                    referenceRestrictCustomerSearch = true;
                    if (RfqItemReferenceAccessRules.UsesOrgSubtreeCustomerReveal(summary))
                    {
                        referenceCustomerReveal = await dataPermission.GetAllowedUserIdsForDataScopeAsync(
                            summary,
                            includeChildren: true,
                            cancellationToken);
                    }
                    else
                        referenceCustomerSelfOnly = true;
                }
            }
            else if (RfqItemListDataScopeRules.ShouldApplyJobPageScope(summary))
            {
                if (summary.SaleDataScope == 4 && summary.PurchaseDataScope == 4)
                {
                    q = q.Where(_ => false);
                }
                else
                {
                    var protectionMinutes = await purchaseQuoterPoolService.GetDemandProtectionMinutesAsync(cancellationToken);
                    var protectionCutoffUtc = RfqDemandProtectionRules.ProtectionCutoffUtc(
                        protectionMinutes,
                        DateTime.UtcNow);
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

                    q = q.Where(x =>
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
        }

        if (request.StartDate.HasValue)
        {
            var start = SalesAnalyticsDateFilter.ToUtcDateStart(request.StartDate.Value);
            q = q.Where(x => x.Rfq.CreateTime >= start);
        }

        if (request.EndDate.HasValue)
        {
            var endExclusive = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(request.EndDate.Value);
            q = q.Where(x => x.Rfq.CreateTime < endExclusive);
        }

        if (request.ItemCreateStartUtc.HasValue)
        {
            var start = PostgreSqlDateTime.ToUtc(request.ItemCreateStartUtc.Value);
            q = q.Where(x => x.Item.CreateTime >= start);
        }

        if (request.ItemCreateEndExclusiveUtc.HasValue)
        {
            var endExclusive = PostgreSqlDateTime.ToUtc(request.ItemCreateEndExclusiveUtc.Value);
            q = q.Where(x => x.Item.CreateTime < endExclusive);
        }

        if (request.QuoteCreateStartUtc.HasValue || request.QuoteCreateEndExclusiveUtc.HasValue)
        {
            var qStart = request.QuoteCreateStartUtc.HasValue
                ? PostgreSqlDateTime.ToUtc(request.QuoteCreateStartUtc.Value)
                : (DateTime?)null;
            var qEnd = request.QuoteCreateEndExclusiveUtc.HasValue
                ? PostgreSqlDateTime.ToUtc(request.QuoteCreateEndExclusiveUtc.Value)
                : (DateTime?)null;
            q = q.Where(x =>
                db.Quotes.AsNoTracking().Any(quote =>
                    quote.RFQItemId != null
                    && quote.RFQItemId == x.Item.Id
                    && (qStart == null || quote.CreateTime >= qStart)
                    && (qEnd == null || quote.CreateTime < qEnd)));
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerKeyword))
        {
            var kw = request.CustomerKeyword.Trim().ToLowerInvariant();
            var selfId = request.CurrentUserId?.Trim();
            var reveal = referenceCustomerReveal;
            var selfOnly = referenceCustomerSelfOnly;
            q = q.Where(x =>
                (!referenceRestrictCustomerSearch ||
                 (x.Rfq.SalesUserId != null &&
                  ((selfOnly && selfId != null && x.Rfq.SalesUserId == selfId) ||
                   (!selfOnly && reveal != null && reveal.Contains(x.Rfq.SalesUserId))))) &&
                ((x.Customer != null &&
                  ((x.Customer.OfficialName != null && x.Customer.OfficialName.ToLower().Contains(kw)) ||
                   (x.Customer.NickName != null && x.Customer.NickName.ToLower().Contains(kw)))) ||
                 (x.Rfq.CustomerId != null && x.Rfq.CustomerId.ToLower().Contains(kw))));
        }

        if (!string.IsNullOrWhiteSpace(request.MaterialModel))
        {
            var kw = request.MaterialModel.Trim().ToLowerInvariant();
            if (request.CanViewCustomerInList)
            {
                q = q.Where(x =>
                    x.Item.Mpn.ToLower().Contains(kw) ||
                    (x.Item.CustomerMpn != null && x.Item.CustomerMpn.ToLower().Contains(kw)));
            }
            else
                q = q.Where(x => x.Item.Mpn.ToLower().Contains(kw));
        }

        if (request.BrandId is > 0)
        {
            var brandId = request.BrandId.Value;
            q = q.Where(x => x.Item.BrandId == brandId);
        }

        if (!string.IsNullOrWhiteSpace(request.SalesUserId))
        {
            var sid = request.SalesUserId.Trim();
            q = q.Where(x =>
                x.Rfq.SalesUserId != null &&
                x.Rfq.SalesUserId == sid);
        }
        else if (!string.IsNullOrWhiteSpace(request.SalesUserKeyword))
        {
            var kw = request.SalesUserKeyword.Trim().ToLowerInvariant();
            q = q.Where(x =>
                (x.SalesUser != null &&
                 ((x.SalesUser.UserName != null && x.SalesUser.UserName.ToLower().Contains(kw)) ||
                  (x.SalesUser.RealName != null && x.SalesUser.RealName.ToLower().Contains(kw)))) ||
                (x.Rfq.SalesUserId != null && x.Rfq.SalesUserId.ToLower().Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(request.PurchaserUserId))
        {
            var pid = request.PurchaserUserId.Trim();
            q = q.Where(x =>
                x.Item.AssignedPurchaserUserId1 == pid ||
                x.Item.AssignedPurchaserUserId2 == pid);
        }

        var quickKnown = RfqItemListQuickFilterCodes.IsKnown(request.QuickFilter);

        if (!quickKnown && request.HasQuotesOnly == true)
        {
            q = q.Where(x =>
                db.Quotes.AsNoTracking().Any(quote =>
                    !quote.IsDeleted &&
                    quote.RFQItemId != null &&
                    quote.RFQItemId == x.Item.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.RfqCode))
        {
            var rfqKw = request.RfqCode.Trim().ToLowerInvariant();
            q = q.Where(x => x.Rfq.RfqCode.ToLower().Contains(rfqKw));
        }

        if (!quickKnown && request.Status.HasValue)
        {
            var st = request.Status.Value;
            if (st == 0)
            {
                // 与报价条数统计一致：仅计未软删报价
                q = q.Where(x =>
                    x.Item.Status == 0 &&
                    !db.Quotes.AsNoTracking().Any(quote =>
                        !quote.IsDeleted &&
                        quote.RFQItemId != null &&
                        quote.RFQItemId == x.Item.Id));
            }
            else if (st == 1)
            {
                q = q.Where(x =>
                    x.Item.Status == 1 ||
                    (x.Item.Status == 0 &&
                     db.Quotes.AsNoTracking().Any(quote =>
                         !quote.IsDeleted &&
                         quote.RFQItemId != null &&
                         quote.RFQItemId == x.Item.Id)));
            }
            else
            {
                q = q.Where(x => x.Item.Status == st);
            }
        }

        q = ApplyQuickFilter(db, q, request.QuickFilter);

        // 报表透镜 + 排除主单已取消
        if (RfqItemAnalyticsDatasets.IsReportScope(request.AnalyticsDataset))
        {
            q = ApplyReportNotCancelled(q);
            q = ApplyReportViewLens(db, q, request);
        }

        if (request.QuotableByMeOnly && !string.IsNullOrWhiteSpace(request.CurrentUserId))
        {
            var actorId = request.CurrentUserId.Trim();
            var quoteSummary = await rbacService.GetUserPermissionSummaryAsync(actorId);
            if (!quoteSummary.IsSysAdmin && !RfqItemQuoteAccessRules.IsPurchaseDepartmentDirector(quoteSummary))
            {
                var protectionMinutes = await purchaseQuoterPoolService.GetDemandProtectionMinutesAsync(cancellationToken);
                var protectionCutoffUtc = RfqDemandProtectionRules.ProtectionCutoffUtc(
                    protectionMinutes,
                    DateTime.UtcNow);
                var protectionPoolEnabled = RfqDemandProtectionRules.CanParticipateInProtectionPool(quoteSummary);
                q = q.Where(x =>
                    x.Item.AssignedPurchaserUserId1 == actorId ||
                    x.Item.AssignedPurchaserUserId2 == actorId ||
                    (protectionPoolEnabled &&
                     (protectionMinutes <= 0 || x.Item.CreateTime <= protectionCutoffUtc)));
            }
        }

        return q;
    }

    /// <summary>报表硬过滤：排除主单已取消。</summary>
    public static IQueryable<RfqItemListJoin> ApplyReportNotCancelled(IQueryable<RfqItemListJoin> q) =>
        q.Where(x => x.Rfq.Status != (short)RfqMainStatus.Cancelled);

    private static IQueryable<RfqItemListJoin> ApplyReportViewLens(
        ApplicationDbContext db,
        IQueryable<RfqItemListJoin> q,
        RFQItemQueryRequest request)
    {
        var viewLevel = (request.AnalyticsViewLevel ?? string.Empty).Trim().ToLowerInvariant();
        if (viewLevel == SalesAnalyticsViewLevels.Personal
            && !string.IsNullOrWhiteSpace(request.SalesUserId))
        {
            var uid = request.SalesUserId.Trim();
            return q.Where(x => x.Rfq.SalesUserId == uid);
        }

        if (viewLevel == SalesAnalyticsViewLevels.Department)
        {
            var deptId = request.AnalyticsDepartmentId?.Trim();
            if (string.IsNullOrWhiteSpace(deptId))
                return q;

            if (string.Equals(deptId, SalesAnalyticsScopeValidator.UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase))
            {
                var withPrimary = db.RbacUserDepartments.AsNoTracking()
                    .Where(ud => ud.IsPrimary)
                    .Select(ud => ud.UserId);
                return q.Where(x =>
                    x.Rfq.SalesUserId == null
                    || !withPrimary.Contains(x.Rfq.SalesUserId));
            }

            var userIdsInDept = db.RbacUserDepartments.AsNoTracking()
                .Where(ud => ud.IsPrimary && ud.DepartmentId == deptId)
                .Select(ud => ud.UserId);
            return q.Where(x => x.Rfq.SalesUserId != null && userIdsInDept.Contains(x.Rfq.SalesUserId));
        }

        return q;
    }
}
