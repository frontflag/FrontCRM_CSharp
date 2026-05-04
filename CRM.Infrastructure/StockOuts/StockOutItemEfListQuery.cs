using CRM.Core.Interfaces;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.StockOuts;

/// <summary>出库明细分页主键查询（与 <see cref="StockOutItemListQuery"/> 筛选语义对齐）。</summary>
public sealed class StockOutItemEfListQuery : IStockOutItemListQuery
{
    public const int MaxPageSize = 100;

    private readonly ApplicationDbContext _db;

    public StockOutItemEfListQuery(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<PagedResult<string>> GetPagedStockOutItemIdsAsync(
        StockOutItemListQuery? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        query ??= new StockOutItemListQuery();
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);

        var q =
            from si in _db.StockOutItems.AsNoTracking()
            join so in _db.StockOuts.AsNoTracking() on si.StockOutId equals so.Id
            join sol in _db.SellOrderItems.AsNoTracking() on so.SellOrderItemId equals sol.Id into solg
            from sol in solg.DefaultIfEmpty()
            join ord in _db.SellOrders.AsNoTracking() on sol.SellOrderId equals ord.Id into ordg
            from ord in ordg.DefaultIfEmpty()
            join hdrCust in _db.Customers.AsNoTracking() on so.CustomerId equals hdrCust.Id into hc
            from hdrCust in hc.DefaultIfEmpty()
            join u in _db.Users.AsNoTracking() on ord.SalesUserId equals u.Id into ug
            from u in ug.DefaultIfEmpty()
            select new { si, so, sol, ord, hdrCust, u };

        if (query.Status.HasValue)
        {
            var st = query.Status.Value;
            q = q.Where(x => x.so.Status == st);
        }

        if (!string.IsNullOrWhiteSpace(query.StockOutCode))
        {
            var k = query.StockOutCode.Trim().ToLowerInvariant();
            q = q.Where(x => x.so.StockOutCode.ToLower().Contains(k));
        }

        if (query.StockOutDateFrom.HasValue)
        {
            var d = query.StockOutDateFrom.Value.Date;
            q = q.Where(x => x.so.StockOutDate >= d);
        }

        if (query.StockOutDateTo.HasValue)
        {
            var endEx = query.StockOutDateTo.Value.Date.AddDays(1);
            q = q.Where(x => x.so.StockOutDate < endEx);
        }

        if (!string.IsNullOrWhiteSpace(query.CustomerName))
        {
            var k = query.CustomerName.Trim().ToLowerInvariant();
            q = q.Where(x =>
                (x.hdrCust != null &&
                 x.hdrCust.OfficialName != null &&
                 x.hdrCust.OfficialName.ToLower().Contains(k)) ||
                (x.hdrCust != null &&
                 x.hdrCust.NickName != null &&
                 x.hdrCust.NickName.ToLower().Contains(k)) ||
                (x.ord != null && x.ord.CustomerName != null && x.ord.CustomerName.ToLower().Contains(k)));
        }

        if (!string.IsNullOrWhiteSpace(query.SalesUserName))
        {
            var k = query.SalesUserName.Trim().ToLowerInvariant();
            q = q.Where(x =>
                (x.ord != null &&
                 x.ord.SalesUserName != null &&
                 x.ord.SalesUserName.ToLower().Contains(k)) ||
                (x.u != null && x.u.UserName != null && x.u.UserName.ToLower().Contains(k)));
        }

        if (!string.IsNullOrWhiteSpace(query.PurchasePn))
        {
            var k = query.PurchasePn.Trim().ToLowerInvariant();
            q = q.Where(x => x.si.PurchasePn != null && x.si.PurchasePn.ToLower().Contains(k));
        }

        if (!string.IsNullOrWhiteSpace(query.SellOrderItemCode))
        {
            var k = query.SellOrderItemCode.Trim().ToLowerInvariant();
            q = q.Where(x =>
                x.sol != null &&
                x.sol.SellOrderItemCode != null &&
                x.sol.SellOrderItemCode.ToLower().Contains(k));
        }

        if (!string.IsNullOrWhiteSpace(query.StockInCode))
        {
            var k = query.StockInCode.Trim().ToLowerInvariant();
            q = q.Where(x =>
                _db.StockOutItemExtends.Any(e =>
                    e.Id == x.si.Id &&
                    e.StockInItemId != null &&
                    _db.StockInItems.Any(sii =>
                        sii.Id == e.StockInItemId &&
                        _db.StockIns.Any(sin =>
                            sin.Id == sii.StockInId &&
                            sin.StockInCode != null &&
                            sin.StockInCode.ToLower().Contains(k)))));
        }

        var total = await q.CountAsync(cancellationToken);
        var ids = await q
            .OrderByDescending(x => x.so.StockOutDate)
            .ThenBy(x => x.so.StockOutCode)
            .ThenBy(x => x.si.Id)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(x => x.si.Id)
            .ToListAsync(cancellationToken);

        return new PagedResult<string>
        {
            Items = ids,
            TotalCount = total,
            PageIndex = p,
            PageSize = ps
        };
    }
}
