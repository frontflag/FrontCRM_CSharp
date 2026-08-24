using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.StockOuts;

public sealed class StockOutRequestListQuery : IStockOutRequestListQuery
{
    public const int MaxPageSize = 2000;
    private const short PickingTaskStatusCompleted = 100;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public StockOutRequestListQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    /// <inheritdoc />
    public async Task<PagedResult<string>> GetPagedStockOutRequestIdsAsync(
        StockOutRequestListQueryRequest? filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);

        var scopedSo = await _dataPermission.ApplySellOrderDataScopeAsync(
            filter?.CurrentUserId,
            _db.SellOrders.AsNoTracking(),
            cancellationToken);

        var q =
            from r in _db.StockOutRequests.AsNoTracking()
            join so in scopedSo on r.SalesOrderId equals so.Id
            select new { r, so };

        if (filter != null)
        {
            if (filter.Status.HasValue)
            {
                var st = filter.Status.Value;
                q = q.Where(x => x.r.Status == st);
            }
            else
            {
                var wf = (filter.Workflow ?? "all").Trim().ToLowerInvariant();
                if (wf == "done")
                    q = q.Where(x => x.r.Status == StockOutRequestStatusCode.StockedOut);
                else if (wf == "pending_pick")
                {
                    q = q.Where(x =>
                        x.r.Status == StockOutRequestStatusCode.PendingPacking &&
                        !_db.PickingTasks.Any(pt =>
                            !pt.IsDeleted
                            && pt.Status == PickingTaskStatusCompleted
                            && pt.PackingId != null
                            && _db.PackingItems.Any(pi =>
                                !pi.IsDeleted
                                && pi.PackingId == pt.PackingId
                                && pi.StockOutNotifyId == x.r.Id)));
                }
                else if (wf == "picked_pending_out")
                {
                    q = q.Where(x =>
                        (x.r.Status == StockOutRequestStatusCode.PendingPacking
                            || x.r.Status == StockOutRequestStatusCode.Packed) &&
                        _db.PickingTasks.Any(pt =>
                            !pt.IsDeleted
                            && pt.Status == PickingTaskStatusCompleted
                            && pt.PackingId != null
                            && _db.PackingItems.Any(pi =>
                                !pi.IsDeleted
                                && pi.PackingId == pt.PackingId
                                && pi.StockOutNotifyId == x.r.Id)));
                }
            }

            if (filter.RegionType.HasValue)
                q = q.Where(x => x.r.RegionType == filter.RegionType.Value);

            if (filter.StockOutType.HasValue)
            {
                var stockOutType = filter.StockOutType.Value;
                q = q.Where(x => x.r.StockOutType == stockOutType);
            }

            if (!string.IsNullOrWhiteSpace(filter.CustomerName))
            {
                var k = filter.CustomerName.Trim().ToLowerInvariant();
                q = q.Where(x =>
                    (x.so != null && x.so.CustomerName != null && x.so.CustomerName.ToLower().Contains(k)) ||
                    (x.r.CustomerId != null && _db.Customers.Any(c =>
                        c.Id == x.r.CustomerId &&
                        ((c.OfficialName != null && c.OfficialName.ToLower().Contains(k)) ||
                         (c.NickName != null && c.NickName.ToLower().Contains(k))))));
            }

            if (!string.IsNullOrWhiteSpace(filter.SalesUserName))
            {
                var k = filter.SalesUserName.Trim().ToLowerInvariant();
                q = q.Where(x =>
                    x.so != null &&
                    x.so.SalesUserId != null &&
                    _db.Users.Any(u =>
                        u.Id == x.so.SalesUserId &&
                        ((u.RealName != null && u.RealName.ToLower().Contains(k)) ||
                         u.UserName.ToLower().Contains(k))));
            }

            if (!string.IsNullOrWhiteSpace(filter.MaterialModel))
            {
                var k = filter.MaterialModel.Trim().ToLowerInvariant();
                q = q.Where(x => x.r.MaterialCode.ToLower().Contains(k));
            }

            if (!string.IsNullOrWhiteSpace(filter.SalesOrderCode))
            {
                var k = filter.SalesOrderCode.Trim().ToLowerInvariant();
                q = q.Where(x => x.so.SellOrderCode.ToLower().Contains(k));
            }

            if (!string.IsNullOrWhiteSpace(filter.CustomerSo))
            {
                var k = filter.CustomerSo.Trim().ToLowerInvariant();
                q = q.Where(x =>
                    _db.SellOrderItems.Any(si =>
                        !si.IsDeleted
                        && si.Id == x.r.SalesOrderItemId
                        && si.CustomerSo != null
                        && si.CustomerSo.ToLower().Contains(k)));
            }

            if (!string.IsNullOrWhiteSpace(filter.CustomerPn))
            {
                var k = filter.CustomerPn.Trim().ToLowerInvariant();
                q = q.Where(x =>
                    _db.SellOrderItems.Any(si =>
                        !si.IsDeleted
                        && si.Id == x.r.SalesOrderItemId
                        && si.CustomerPn != null
                        && si.CustomerPn.ToLower().Contains(k)));
            }

            if (!string.IsNullOrWhiteSpace(filter.Brand))
            {
                var k = filter.Brand.Trim().ToLowerInvariant();
                q = q.Where(x =>
                    x.r.MaterialName != null && x.r.MaterialName.ToLower().Contains(k));
            }

            if (!string.IsNullOrWhiteSpace(filter.PackingCode))
            {
                var k = filter.PackingCode.Trim().ToLowerInvariant();
                q = q.Where(x =>
                    _db.PackingItems.Any(pi =>
                        !pi.IsDeleted
                        && pi.StockOutNotifyId != null
                        && pi.StockOutNotifyId == x.r.Id
                        && _db.Packings.Any(pk =>
                            !pk.IsDeleted
                            && pk.Id == pi.PackingId
                            && pk.Code.ToLower().Contains(k))));
            }

            if (!string.IsNullOrWhiteSpace(filter.StockOutCode))
            {
                var k = filter.StockOutCode.Trim().ToLowerInvariant();
                q = q.Where(x =>
                    _db.PackingItems.Any(pi =>
                        !pi.IsDeleted
                        && pi.StockOutNotifyId != null
                        && pi.StockOutNotifyId == x.r.Id
                        && _db.StockOutItems.Any(soi =>
                            !soi.IsDeleted
                            && soi.PackingId != null
                            && soi.PackingId == pi.PackingId
                            && _db.StockOuts.Any(soOut =>
                                !soOut.IsDeleted
                                && soOut.Id == soi.StockOutId
                                && soOut.StockOutType != StockOutTypeCode.Transfer
                                && soOut.StockOutCode.ToLower().Contains(k))))
                    || _db.PackingItems.Any(pi =>
                        !pi.IsDeleted
                        && pi.StockOutNotifyId != null
                        && pi.StockOutNotifyId == x.r.Id
                        && _db.PickingTaskItems.Any(pti =>
                            !pti.IsDeleted
                            && pti.PackingItemId != null
                            && pti.PackingItemId == pi.Id
                            && _db.StockOutItems.Any(soi =>
                                !soi.IsDeleted
                                && soi.PickingTaskItemId != null
                                && soi.PickingTaskItemId == pti.Id
                                && _db.StockOuts.Any(soOut =>
                                    !soOut.IsDeleted
                                    && soOut.Id == soi.StockOutId
                                    && soOut.StockOutType != StockOutTypeCode.Transfer
                                    && soOut.StockOutCode.ToLower().Contains(k))))));
            }

            if (filter.RequestDateFrom.HasValue)
                q = q.Where(x => x.r.RequestDate >= filter.RequestDateFrom.Value);

            if (filter.RequestDateTo.HasValue)
            {
                var toExclusive = filter.RequestDateTo.Value.Date.AddDays(1);
                q = q.Where(x => x.r.RequestDate < toExclusive);
            }

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var k = filter.Keyword.Trim().ToLowerInvariant();
                q = q.Where(x =>
                    x.r.RequestCode.ToLower().Contains(k) ||
                    (x.so != null && x.so.SellOrderCode.ToLower().Contains(k)) ||
                    x.r.MaterialCode.ToLower().Contains(k) ||
                    (x.r.MaterialName != null && x.r.MaterialName.ToLower().Contains(k)) ||
                    (x.so.CustomerName != null && x.so.CustomerName.ToLower().Contains(k)));
            }
        }

        var total = await q.CountAsync(cancellationToken);
        var ids = await q
            .OrderByDescending(x => x.r.CreateTime)
            .ThenByDescending(x => x.r.RequestDate)
            .ThenBy(x => x.r.Id)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(x => x.r.Id)
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
