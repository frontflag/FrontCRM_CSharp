using CRM.Core.Interfaces;
using CRM.Core.Models.RFQ;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.RfqListQueries;

/// <summary>需求明细列表筛选（列表分页与看板共用）。</summary>
internal static class RfqItemListFilter
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

        if (!string.IsNullOrWhiteSpace(request.CurrentUserId))
        {
            var summary = await rbacService.GetUserPermissionSummaryAsync(request.CurrentUserId.Trim());
            if (!summary.IsSysAdmin && summary.SaleDataScope != 0 && summary.PurchaseDataScope != 0)
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

        if (!string.IsNullOrWhiteSpace(request.CustomerKeyword))
        {
            var kw = request.CustomerKeyword.Trim().ToLowerInvariant();
            q = q.Where(x =>
                (x.Customer != null &&
                 ((x.Customer.OfficialName != null && x.Customer.OfficialName.ToLower().Contains(kw)) ||
                  (x.Customer.NickName != null && x.Customer.NickName.ToLower().Contains(kw)))) ||
                (x.Rfq.CustomerId != null && x.Rfq.CustomerId.ToLower().Contains(kw)));
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

        if (request.HasQuotesOnly == true)
        {
            q = q.Where(x =>
                db.Quotes.AsNoTracking().Any(quote =>
                    quote.RFQItemId != null &&
                    quote.RFQItemId == x.Item.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.RfqCode))
        {
            var rfqKw = request.RfqCode.Trim().ToLowerInvariant();
            q = q.Where(x => x.Rfq.RfqCode.ToLower().Contains(rfqKw));
        }

        if (request.Status.HasValue)
        {
            var st = request.Status.Value;
            if (st == 0)
            {
                q = q.Where(x =>
                    x.Item.Status == 0 &&
                    !db.Quotes.AsNoTracking().Any(quote =>
                        quote.RFQItemId != null &&
                        quote.RFQItemId == x.Item.Id));
            }
            else if (st == 1)
            {
                q = q.Where(x =>
                    x.Item.Status == 1 ||
                    (x.Item.Status == 0 &&
                     db.Quotes.AsNoTracking().Any(quote =>
                         quote.RFQItemId != null &&
                         quote.RFQItemId == x.Item.Id)));
            }
            else
            {
                q = q.Where(x => x.Item.Status == st);
            }
        }

        return q;
    }
}
