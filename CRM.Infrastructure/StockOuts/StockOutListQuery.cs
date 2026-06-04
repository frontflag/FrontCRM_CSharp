using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.StockOuts;

public sealed class StockOutListQuery : IStockOutListQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;

    public StockOutListQuery(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<PagedResult<string>> GetPagedStockOutIdsAsync(
        StockOutListQueryRequest? filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);

        var q = _db.StockOuts.AsNoTracking()
            .Where(so => so.StockOutType != StockOutTypeCode.Transfer);

        if (filter != null && !string.IsNullOrWhiteSpace(filter.SourceCode))
        {
            var c = filter.SourceCode.Trim().ToLowerInvariant();
            q = q.Where(so => so.SourceCode != null && so.SourceCode.ToLower() == c);
        }
        else if (filter != null)
        {
            if (filter.Status.HasValue)
                q = q.Where(so => so.Status == filter.Status.Value);

            if (!string.IsNullOrWhiteSpace(filter.StockOutCode))
            {
                var k = filter.StockOutCode.Trim().ToLowerInvariant();
                q = q.Where(so => so.StockOutCode.ToLower().Contains(k));
            }

            if (!string.IsNullOrWhiteSpace(filter.PackingCode))
            {
                var k = filter.PackingCode.Trim().ToLowerInvariant();
                q = q.Where(so =>
                    _db.StockOutItems.Any(si =>
                        !si.IsDeleted
                        && si.StockOutId == so.Id
                        && si.PackingId != null
                        && _db.Packings.Any(pk =>
                            !pk.IsDeleted
                            && pk.Id == si.PackingId
                            && pk.Code.ToLower().Contains(k)))
                    || _db.StockOutItems.Any(si =>
                        !si.IsDeleted
                        && si.StockOutId == so.Id
                        && si.PickingTaskItemId != null
                        && _db.PickingTaskItems.Any(pti =>
                            !pti.IsDeleted
                            && pti.Id == si.PickingTaskItemId
                            && _db.PickingTasks.Any(pt =>
                                !pt.IsDeleted
                                && pt.Id == pti.PickingTaskId
                                && pt.PackingId != null
                                && _db.Packings.Any(pk =>
                                    !pk.IsDeleted
                                    && pk.Id == pt.PackingId
                                    && pk.Code.ToLower().Contains(k))))));
            }

            if (!string.IsNullOrWhiteSpace(filter.ShipmentMethod))
            {
                var k = filter.ShipmentMethod.Trim().ToLowerInvariant();
                q = q.Where(so =>
                    so.ShipmentMethod != null && so.ShipmentMethod.ToLower().Contains(k));
            }

            if (!string.IsNullOrWhiteSpace(filter.CustomerName))
            {
                var k = filter.CustomerName.Trim().ToLowerInvariant();
                q = q.Where(so =>
                    (so.CustomerId != null
                     && _db.Customers.Any(c =>
                         c.Id == so.CustomerId
                         && ((c.OfficialName != null && c.OfficialName.ToLower().Contains(k))
                             || (c.NickName != null && c.NickName.ToLower().Contains(k)))))
                    || (so.SellOrderItemId != null
                        && _db.SellOrderItems.Any(sol =>
                            sol.Id == so.SellOrderItemId
                            && _db.SellOrders.Any(o =>
                                o.Id == sol.SellOrderId
                                && o.CustomerName != null
                                && o.CustomerName.ToLower().Contains(k)))));
            }

            if (!string.IsNullOrWhiteSpace(filter.SalesUserName))
            {
                var k = filter.SalesUserName.Trim().ToLowerInvariant();
                q = q.Where(so =>
                    so.SellOrderItemId != null
                    && _db.SellOrderItems.Any(sol =>
                        sol.Id == so.SellOrderItemId
                        && _db.SellOrders.Any(o =>
                            o.Id == sol.SellOrderId
                            && ((o.SalesUserName != null && o.SalesUserName.ToLower().Contains(k))
                                || (o.SalesUserId != null
                                    && _db.Users.Any(u =>
                                        u.Id == o.SalesUserId
                                        && ((u.RealName != null && u.RealName.ToLower().Contains(k))
                                            || u.UserName.ToLower().Contains(k))))))));
            }

            if (!string.IsNullOrWhiteSpace(filter.Remark))
            {
                var k = filter.Remark.Trim().ToLowerInvariant();
                q = q.Where(so => so.Remark != null && so.Remark.ToLower().Contains(k));
            }

            if (filter.StockOutDateFrom.HasValue)
            {
                var d = filter.StockOutDateFrom.Value.Date;
                q = q.Where(so => so.StockOutDate != null && so.StockOutDate >= d);
            }

            if (filter.StockOutDateTo.HasValue)
            {
                var endEx = filter.StockOutDateTo.Value.Date.AddDays(1);
                q = q.Where(so => so.StockOutDate != null && so.StockOutDate < endEx);
            }

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var k = filter.Keyword.Trim().ToLowerInvariant();
                q = q.Where(so =>
                    so.StockOutCode.ToLower().Contains(k)
                    || (so.SourceCode != null && so.SourceCode.ToLower().Contains(k))
                    || (so.ShipmentMethod != null && so.ShipmentMethod.ToLower().Contains(k))
                    || (so.CourierTrackingNo != null && so.CourierTrackingNo.ToLower().Contains(k))
                    || (so.SellOrderItemId != null
                        && _db.SellOrderItems.Any(sol =>
                            sol.Id == so.SellOrderItemId
                            && sol.SellOrderItemCode != null
                            && sol.SellOrderItemCode.ToLower().Contains(k))));
            }
        }

        var total = await q.CountAsync(cancellationToken);
        var ids = await q
            .OrderByDescending(so => so.CreateTime)
            .ThenByDescending(so => so.Id)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(so => so.Id)
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
