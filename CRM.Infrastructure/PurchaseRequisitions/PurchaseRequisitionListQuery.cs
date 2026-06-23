using CRM.Core.Interfaces;
using CRM.Core.Models.Purchase;
using CRM.Core.Services;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.PurchaseRequisitions;

/// <summary>采购申请列表：数据库侧分页（替代控制器内全表 <c>GetAllAsync</c> 再 <c>Skip</c>/<c>Take</c>）。</summary>
public sealed class PurchaseRequisitionListQuery : IPurchaseRequisitionListQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public PurchaseRequisitionListQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    /// <inheritdoc />
    public async Task<PagedResult<PurchaseRequisitionListPageRow>> GetPagedAsync(
        PurchaseRequisitionListQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var scopedPr = await _dataPermission.ApplyPurchaseRequisitionListDataScopeAsync(
            request.CurrentUserId,
            _db.PurchaseRequisitions.AsNoTracking(),
            _db.SellOrders.AsNoTracking(),
            cancellationToken);

        var q =
            from pr in scopedPr
            join so in _db.SellOrders.AsNoTracking() on pr.SellOrderId equals so.Id into soJoin
            from so in soJoin.DefaultIfEmpty()
            select new
            {
                pr,
                SellOrderCode = so != null ? so.SellOrderCode : null,
                SellOrderSalesUserId = so != null ? so.SalesUserId : null,
                SellOrderSalesUserName = so != null ? so.SalesUserName : null
            };

        if (!string.IsNullOrWhiteSpace(request.SellOrderId))
        {
            var sid = request.SellOrderId.Trim();
            q = q.Where(x => x.pr.SellOrderId == sid);
        }

        if (request.Status.HasValue)
            q = q.Where(x => x.pr.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim().ToLowerInvariant();
            q = q.Where(x =>
                x.pr.BillCode.ToLower().Contains(kw) ||
                (x.pr.PN != null && x.pr.PN.ToLower().Contains(kw)) ||
                (x.pr.Brand != null && x.pr.Brand.ToLower().Contains(kw)) ||
                (x.pr.Remark != null && x.pr.Remark.ToLower().Contains(kw)) ||
                (x.SellOrderCode != null && x.SellOrderCode.ToLower().Contains(kw)));
        }

        var total = await q.CountAsync(cancellationToken);

        var slice = await q
            .OrderByDescending(x => x.pr.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.pr,
                x.SellOrderCode,
                x.SellOrderSalesUserId,
                x.SellOrderSalesUserName
            })
            .ToListAsync(cancellationToken);

        var lineIds = slice
            .Select(x => x.pr.SellOrderItemId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        IReadOnlyDictionary<string, string> quoteSalesUserIdByLineId =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (lineIds.Count > 0)
        {
            var lineQuoteSalesUsers = await (
                from item in _db.SellOrderItems.AsNoTracking()
                where lineIds.Contains(item.Id) && item.QuoteId != null
                join quote in _db.Quotes.AsNoTracking() on item.QuoteId equals quote.Id
                select new { ItemId = item.Id, quote.SalesUserId }
            ).ToListAsync(cancellationToken);
            quoteSalesUserIdByLineId = lineQuoteSalesUsers
                .Where(x => !string.IsNullOrWhiteSpace(x.SalesUserId))
                .ToDictionary(x => x.ItemId.Trim(), x => x.SalesUserId!.Trim(), StringComparer.OrdinalIgnoreCase);
        }

        var userIds = slice
            .SelectMany(x => new[]
            {
                x.pr.PurchaseUserId,
                x.pr.CreateByUserId,
                x.pr.SalesUserId,
                x.SellOrderSalesUserId,
                quoteSalesUserIdByLineId.TryGetValue(x.pr.SellOrderItemId.Trim(), out var qsu) ? qsu : null
            })
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // 销售订单 sales_user_name 可能存登录账号，纳入按 UserName 反查
        var salesUserNameHints = slice
            .Select(x => x.SellOrderSalesUserName?.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (salesUserNameHints.Count > 0)
        {
            var hintUsers = await _db.Users.AsNoTracking()
                .Where(u => salesUserNameHints.Contains(u.UserName))
                .ToListAsync(cancellationToken);
            foreach (var u in hintUsers)
            {
                if (!string.IsNullOrWhiteSpace(u.Id) && !userIds.Contains(u.Id, StringComparer.OrdinalIgnoreCase))
                    userIds.Add(u.Id.Trim());
            }
        }

        IReadOnlyDictionary<string, string> userDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        IReadOnlyDictionary<string, string> userLoginNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        IReadOnlyDictionary<string, string> loginByUserName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (userIds.Count > 0)
        {
            var idSet = userIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var users = await _db.Users.AsNoTracking()
                .Where(u => idSet.Contains(u.Id))
                .ToListAsync(cancellationToken);
            userDisplayNames = users.ToDictionary(
                u => u.Id,
                u => EntityLookupService.FormatUserDisplayName(u) ?? u.UserName.Trim(),
                StringComparer.OrdinalIgnoreCase);
            userLoginNames = users.ToDictionary(
                u => u.Id,
                u => EntityLookupService.FormatUserLoginName(u) ?? u.UserName.Trim(),
                StringComparer.OrdinalIgnoreCase);
            loginByUserName = users
                .Where(u => !string.IsNullOrWhiteSpace(u.UserName))
                .GroupBy(u => u.UserName.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => EntityLookupService.FormatUserLoginName(g.First()) ?? g.Key,
                    StringComparer.OrdinalIgnoreCase);
        }

        string? DisplayNameFor(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;
            var key = userId.Trim();
            return userDisplayNames.TryGetValue(key, out var name) ? name : null;
        }

        string? LoginAccountFor(string? userIdOrLogin)
        {
            if (string.IsNullOrWhiteSpace(userIdOrLogin)) return null;
            var key = userIdOrLogin.Trim();
            if (userLoginNames.TryGetValue(key, out var byId)) return byId;
            if (loginByUserName.TryGetValue(key, out var byLogin)) return byLogin;
            return null;
        }

        var items = slice.Select(x =>
        {
            // 销售订单主表为准；采购申请上 sales_user_id 仅作历史冗余兜底
            var salesUserId = !string.IsNullOrWhiteSpace(x.SellOrderSalesUserId)
                ? x.SellOrderSalesUserId.Trim()
                : x.pr.SalesUserId?.Trim();
            if (string.IsNullOrWhiteSpace(salesUserId)
                && !string.IsNullOrWhiteSpace(x.pr.SellOrderItemId)
                && quoteSalesUserIdByLineId.TryGetValue(x.pr.SellOrderItemId.Trim(), out var quoteSalesUserId))
            {
                salesUserId = quoteSalesUserId;
            }

            var salesUserAccount = LoginAccountFor(salesUserId)
                ?? LoginAccountFor(x.SellOrderSalesUserName)
                ?? x.SellOrderSalesUserName?.Trim();

            return new PurchaseRequisitionListPageRow
        {
            Id = x.pr.Id,
            BillCode = x.pr.BillCode,
            SellOrderId = x.pr.SellOrderId,
            SellOrderItemId = x.pr.SellOrderItemId,
            SellOrderCode = x.SellOrderCode,
            PN = x.pr.PN,
            Brand = x.pr.Brand,
            Qty = x.pr.Qty,
            ExpectedPurchaseTime = x.pr.ExpectedPurchaseTime,
            Status = x.pr.Status,
            Type = x.pr.Type,
            SalesUserId = salesUserId,
            SalesUserAccount = salesUserAccount,
            PurchaseUserId = x.pr.PurchaseUserId,
            PurchaseUserName = DisplayNameFor(x.pr.PurchaseUserId),
            PurchaseUserAccount = LoginAccountFor(x.pr.PurchaseUserId),
            QuoteVendorId = x.pr.QuoteVendorId,
            QuoteCost = x.pr.QuoteCost,
            Remark = x.pr.Remark,
            CreateTime = x.pr.CreateTime,
            CreateUserAccount = LoginAccountFor(x.pr.CreateByUserId)
        };
        }).ToList();

        return new PagedResult<PurchaseRequisitionListPageRow>
        {
            Items = items,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }
}
