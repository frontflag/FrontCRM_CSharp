using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.RFQ;
using CRM.Core.Services;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.RfqListQueries;

/// <summary>需求明细列表：EF 数据库分页（行级数据范围与 <see cref="IDataPermissionService.GetRfqItemLineVisibilityPredicateAsync"/> 一致）。</summary>
public sealed class RfqItemListQuery : IRfqItemListQuery
{
    public const int MaxPageSize = 100;

    private readonly ApplicationDbContext _db;
    private readonly IRbacService _rbacService;
    private readonly IDataPermissionService _dataPermission;

    public RfqItemListQuery(
        ApplicationDbContext db,
        IRbacService rbacService,
        IDataPermissionService dataPermission)
    {
        _db = db;
        _rbacService = rbacService;
        _dataPermission = dataPermission;
    }

    /// <inheritdoc />
    public async Task<PagedResult<RFQItemListItem>> GetPagedAsync(
        RFQItemQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var q =
            from item in _db.RFQItems.AsNoTracking()
            join rfq in _db.RFQs.AsNoTracking() on item.RfqId equals rfq.Id
            join cust in _db.Customers.AsNoTracking() on rfq.CustomerId equals cust.Id into custGroup
            from cust in custGroup.DefaultIfEmpty()
            join su in _db.Users.AsNoTracking() on rfq.SalesUserId equals su.Id into suGroup
            from su in suGroup.DefaultIfEmpty()
            select new { item, rfq, cust, su };

        if (!string.IsNullOrWhiteSpace(request.CurrentUserId))
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(request.CurrentUserId.Trim());
            if (!summary.IsSysAdmin && summary.SaleDataScope != 0 && summary.PurchaseDataScope != 0)
            {
                if (summary.SaleDataScope == 4 && summary.PurchaseDataScope == 4)
                {
                    q = q.Where(_ => false);
                }
                else
                {
                    HashSet<string>? saleAllow = null;
                    if (summary.SaleDataScope == 2 || summary.SaleDataScope == 3)
                        saleAllow = await _dataPermission.GetAllowedUserIdsForDataScopeAsync(
                            summary,
                            includeChildren: summary.SaleDataScope == 3,
                            cancellationToken);

                    HashSet<string>? purchaseAllow = null;
                    if (summary.PurchaseDataScope == 2 || summary.PurchaseDataScope == 3)
                        purchaseAllow = await _dataPermission.GetAllowedUserIdsForDataScopeAsync(
                            summary,
                            includeChildren: summary.PurchaseDataScope == 3,
                            cancellationToken);

                    var uid = request.CurrentUserId.Trim();

                    q = q.Where(x =>
                        (
                            summary.SaleDataScope != 4 &&
                            (
                                (summary.SaleDataScope == 1 && x.rfq.SalesUserId != null && x.rfq.SalesUserId == uid) ||
                                ((summary.SaleDataScope == 2 || summary.SaleDataScope == 3) &&
                                 saleAllow != null &&
                                 x.rfq.SalesUserId != null &&
                                 saleAllow.Contains(x.rfq.SalesUserId))
                            )
                        )
                        ||
                        (
                            summary.PurchaseDataScope != 4 &&
                            (
                                (summary.PurchaseDataScope == 1 &&
                                 (x.item.AssignedPurchaserUserId1 == uid ||
                                  x.item.AssignedPurchaserUserId2 == uid)) ||
                                ((summary.PurchaseDataScope == 2 || summary.PurchaseDataScope == 3) &&
                                 purchaseAllow != null &&
                                 ((!string.IsNullOrWhiteSpace(x.item.AssignedPurchaserUserId1) &&
                                   purchaseAllow.Contains(x.item.AssignedPurchaserUserId1!)) ||
                                  (!string.IsNullOrWhiteSpace(x.item.AssignedPurchaserUserId2) &&
                                   purchaseAllow.Contains(x.item.AssignedPurchaserUserId2!))))
                            )
                        ));
                }
            }
        }

        if (request.StartDate.HasValue)
        {
            var start = request.StartDate.Value.Date;
            q = q.Where(x => x.rfq.CreateTime >= start);
        }

        if (request.EndDate.HasValue)
        {
            var endExclusive = request.EndDate.Value.Date.AddDays(1);
            q = q.Where(x => x.rfq.CreateTime < endExclusive);
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerKeyword))
        {
            var kw = request.CustomerKeyword.Trim().ToLowerInvariant();
            q = q.Where(x =>
                (x.cust != null &&
                 ((x.cust.OfficialName != null && x.cust.OfficialName.ToLower().Contains(kw)) ||
                  (x.cust.NickName != null && x.cust.NickName.ToLower().Contains(kw)))) ||
                (x.rfq.CustomerId != null && x.rfq.CustomerId.ToLower().Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(request.MaterialModel))
        {
            var kw = request.MaterialModel.Trim().ToLowerInvariant();
            if (request.CanViewCustomerInList)
            {
                q = q.Where(x =>
                    x.item.Mpn.ToLower().Contains(kw) ||
                    (x.item.CustomerMpn != null && x.item.CustomerMpn.ToLower().Contains(kw)));
            }
            else
                q = q.Where(x => x.item.Mpn.ToLower().Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(request.SalesUserId))
        {
            var sid = request.SalesUserId.Trim();
            q = q.Where(x =>
                x.rfq.SalesUserId != null &&
                x.rfq.SalesUserId == sid);
        }
        else if (!string.IsNullOrWhiteSpace(request.SalesUserKeyword))
        {
            var kw = request.SalesUserKeyword.Trim().ToLowerInvariant();
            q = q.Where(x =>
                (x.su != null &&
                 ((x.su.UserName != null && x.su.UserName.ToLower().Contains(kw)) ||
                  (x.su.RealName != null && x.su.RealName.ToLower().Contains(kw)))) ||
                (x.rfq.SalesUserId != null && x.rfq.SalesUserId.ToLower().Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(request.PurchaserUserId))
        {
            var pid = request.PurchaserUserId.Trim();
            q = q.Where(x =>
                x.item.AssignedPurchaserUserId1 == pid ||
                x.item.AssignedPurchaserUserId2 == pid);
        }

        if (request.HasQuotesOnly == true)
        {
            q = q.Where(x =>
                _db.Quotes.AsNoTracking().Any(q =>
                    q.RFQItemId != null &&
                    q.RFQItemId == x.item.Id));
        }

        var total = await q.CountAsync(cancellationToken);

        var ordered = q
            .OrderByDescending(x => x.rfq.CreateTime)
            .ThenBy(x => x.item.LineNo);

        var slice = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var itemIds = slice.Select(x => x.item.Id).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        var quotedIds = await _db.Quotes.AsNoTracking()
            .Where(quote => quote.RFQItemId != null && itemIds.Contains(quote.RFQItemId!))
            .Select(quote => quote.RFQItemId!)
            .Distinct()
            .ToListAsync(cancellationToken);
        var quotedSet = quotedIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var puIds = slice
            .SelectMany(x => new[] { x.item.AssignedPurchaserUserId1, x.item.AssignedPurchaserUserId2 })
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();
        var puUsers = await _db.Users.AsNoTracking()
            .Where(u => puIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var rows = new List<RFQItemListItem>(slice.Count);
        foreach (var x in slice)
        {
            var lineStatus = x.item.Status;
            if (lineStatus == 0 && quotedSet.Contains((x.item.Id ?? string.Empty).Trim()))
                lineStatus = 1;

            var customerName = x.cust != null
                ? (x.cust.OfficialName ?? x.cust.NickName ?? "")
                : null;

            puUsers.TryGetValue(x.item.AssignedPurchaserUserId1 ?? "", out var pu1);
            puUsers.TryGetValue(x.item.AssignedPurchaserUserId2 ?? "", out var pu2);

            rows.Add(new RFQItemListItem
            {
                Id = x.item.Id ?? string.Empty,
                RfqId = x.item.RfqId,
                RfqCode = x.rfq.RfqCode,
                RfqCreateTime = x.rfq.CreateTime,
                LineNo = x.item.LineNo,
                Mpn = x.item.Mpn,
                CustomerMpn = x.item.CustomerMpn,
                CustomerBrand = string.IsNullOrWhiteSpace(x.item.CustomerBrand) ? null : x.item.CustomerBrand.Trim(),
                Brand = x.item.Brand,
                Quantity = x.item.Quantity,
                Status = lineStatus,
                CustomerId = x.rfq.CustomerId,
                CustomerName = string.IsNullOrWhiteSpace(customerName) ? null : customerName,
                SalesUserId = x.rfq.SalesUserId,
                SalesUserName = EntityLookupService.FormatUserLoginName(x.su),
                AssignedPurchaserUserId1 = x.item.AssignedPurchaserUserId1,
                AssignedPurchaserUserId2 = x.item.AssignedPurchaserUserId2,
                AssignedPurchaserName1 = EntityLookupService.FormatUserLoginName(pu1),
                AssignedPurchaserName2 = EntityLookupService.FormatUserLoginName(pu2),
            });
        }

        return new PagedResult<RFQItemListItem>
        {
            Items = rows,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }
}
