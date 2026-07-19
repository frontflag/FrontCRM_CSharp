using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.RFQ;
using CRM.Core.Services;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.RfqListQueries;

/// <summary>需求明细列表：EF 数据库分页（行级数据范围与 <see cref="IDataPermissionService.GetRfqItemLineVisibilityPredicateAsync"/> 一致）。</summary>
public sealed partial class RfqItemListQuery : IRfqItemListQuery
{
    public const int MaxPageSize = 100;

    private readonly ApplicationDbContext _db;
    private readonly IRbacService _rbacService;
    private readonly IDataPermissionService _dataPermission;
    private readonly IPurchaseQuoterPoolService _purchaseQuoterPoolService;

    public RfqItemListQuery(
        ApplicationDbContext db,
        IRbacService rbacService,
        IDataPermissionService dataPermission,
        IPurchaseQuoterPoolService purchaseQuoterPoolService)
    {
        _db = db;
        _rbacService = rbacService;
        _dataPermission = dataPermission;
        _purchaseQuoterPoolService = purchaseQuoterPoolService;
    }

    /// <inheritdoc />
    public async Task<PagedResult<RFQItemListItem>> GetPagedAsync(
        RFQItemQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var q = await RfqItemListFilter.BuildFilteredJoinQueryAsync(
            _db, _rbacService, _dataPermission, _purchaseQuoterPoolService, request, cancellationToken);

        var total = await q.CountAsync(cancellationToken);

        var ordered = q
            .OrderByDescending(x => x.Rfq.CreateTime)
            .ThenBy(x => x.Item.LineNo);

        var slice = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var itemIds = slice.Select(x => x.Item.Id).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        var quotedIds = await _db.Quotes.AsNoTracking()
            .Where(quote => quote.RFQItemId != null && itemIds.Contains(quote.RFQItemId!))
            .Select(quote => quote.RFQItemId!)
            .Distinct()
            .ToListAsync(cancellationToken);
        var quotedSet = quotedIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var puIds = slice
            .SelectMany(x => new[] { x.Item.AssignedPurchaserUserId1, x.Item.AssignedPurchaserUserId2 })
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();
        var puUsers = await _db.Users.AsNoTracking()
            .Where(u => puIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var createByIds = slice
            .Select(x => x.Rfq.CreateByUserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();
        var createUsers = createByIds.Count == 0
            ? new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase)
            : await _db.Users.AsNoTracking()
                .Where(u => createByIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var rows = new List<RFQItemListItem>(slice.Count);
        foreach (var x in slice)
        {
            var lineStatus = x.Item.Status;
            if (lineStatus == 0 && quotedSet.Contains((x.Item.Id ?? string.Empty).Trim()))
                lineStatus = 1;

            var customerName = x.Customer != null
                ? (x.Customer.OfficialName ?? x.Customer.NickName ?? "")
                : null;

            puUsers.TryGetValue(x.Item.AssignedPurchaserUserId1 ?? "", out var pu1);
            puUsers.TryGetValue(x.Item.AssignedPurchaserUserId2 ?? "", out var pu2);
            createUsers.TryGetValue(x.Rfq.CreateByUserId ?? "", out var createUser);

            rows.Add(new RFQItemListItem
            {
                Id = x.Item.Id ?? string.Empty,
                RfqId = x.Item.RfqId,
                RfqCode = x.Rfq.RfqCode,
                RfqCreateTime = x.Rfq.CreateTime,
                LineNo = x.Item.LineNo,
                Mpn = x.Item.Mpn,
                CustomerMpn = x.Item.CustomerMpn,
                CustomerBrand = string.IsNullOrWhiteSpace(x.Item.CustomerBrand) ? null : x.Item.CustomerBrand.Trim(),
                Brand = x.Item.Brand,
                Quantity = x.Item.Quantity,
                PriceCurrency = x.Item.PriceCurrency,
                Status = lineStatus,
                CustomerId = x.Rfq.CustomerId,
                CustomerName = string.IsNullOrWhiteSpace(customerName) ? null : customerName,
                SalesUserId = x.Rfq.SalesUserId,
                SalesUserName = EntityLookupService.FormatUserLoginName(x.SalesUser),
                CreateByUserId = x.Rfq.CreateByUserId,
                CreateUserName = EntityLookupService.FormatUserLoginName(createUser),
                AssignedPurchaserUserId1 = x.Item.AssignedPurchaserUserId1,
                AssignedPurchaserUserId2 = x.Item.AssignedPurchaserUserId2,
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
