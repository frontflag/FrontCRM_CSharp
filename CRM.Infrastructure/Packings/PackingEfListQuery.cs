using CRM.Core.Interfaces;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Packings;

public sealed class PackingEfListQuery : IPackingListQuery
{
    public const int MaxPageSize = 200;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public PackingEfListQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    public async Task<PagedResult<string>> GetPagedPackingIdsAsync(
        PackingListQueryRequest? filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);

        var q = _db.Packings.AsNoTracking().Where(x => !x.IsDeleted);
        q = await _dataPermission.ApplyPackingListDataScopeAsync(
            filter?.CurrentUserId,
            q,
            _db.Customers.AsNoTracking(),
            cancellationToken);

        if (filter != null)
        {
            if (!string.IsNullOrWhiteSpace(filter.PackingCode))
            {
                var code = filter.PackingCode.Trim().ToLowerInvariant();
                q = q.Where(x => x.Code.ToLower().Contains(code));
            }

            if (filter.Status.HasValue)
                q = q.Where(x => x.Status == filter.Status.Value);

            if (filter.StockOutType.HasValue)
                q = q.Where(x => x.StockOutType == filter.StockOutType.Value);

            if (filter.MaterialType.HasValue)
                q = q.Where(x => x.MaterialType == filter.MaterialType.Value);

            if (!string.IsNullOrWhiteSpace(filter.CustomerName))
            {
                var k = filter.CustomerName.Trim().ToLowerInvariant();
                q = q.Where(x =>
                    x.CustomerId != null &&
                    _db.Customers.Any(c =>
                        c.Id == x.CustomerId &&
                        ((c.OfficialName != null && c.OfficialName.ToLower().Contains(k)) ||
                         (c.NickName != null && c.NickName.ToLower().Contains(k)))));
            }

            if (!string.IsNullOrWhiteSpace(filter.SalesUserName))
            {
                var k = filter.SalesUserName.Trim().ToLowerInvariant();
                q = q.Where(x =>
                    x.SalesId != null &&
                    _db.Users.Any(u =>
                        u.Id == x.SalesId &&
                        ((u.RealName != null && u.RealName.ToLower().Contains(k)) ||
                         u.UserName.ToLower().Contains(k))));
            }

            if (filter.CreateTimeFrom.HasValue)
                q = q.Where(x => x.CreateTime >= filter.CreateTimeFrom.Value);

            if (filter.CreateTimeTo.HasValue)
            {
                var toExclusive = filter.CreateTimeTo.Value.Date.AddDays(1);
                q = q.Where(x => x.CreateTime < toExclusive);
            }
        }

        var total = await q.CountAsync(cancellationToken);
        var ids = await q
            .OrderByDescending(x => x.CreateTime)
            .ThenByDescending(x => x.Id)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        return new PagedResult<string>
        {
            Items = ids,
            TotalCount = total,
            PageIndex = p,
            PageSize = ps
        };
    }

    public async Task<PagedResult<string>> GetPagedPackingItemIdsAsync(
        string? keyword,
        string? packingCode,
        int page,
        int pageSize,
        string? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);

        var scopedPk = await _dataPermission.ApplyPackingListDataScopeAsync(
            currentUserId,
            _db.Packings.AsNoTracking().Where(x => !x.IsDeleted),
            _db.Customers.AsNoTracking(),
            cancellationToken);

        var q = from pi in _db.PackingItems.AsNoTracking()
                join pk in scopedPk on pi.PackingId equals pk.Id
                where !pi.IsDeleted
                select new { pi, pk };

        if (!string.IsNullOrWhiteSpace(packingCode))
        {
            var c = packingCode.Trim().ToLowerInvariant();
            q = q.Where(x => x.pk.Code.ToLower().Contains(c));
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLowerInvariant();
            q = q.Where(x =>
                x.pk.Code.ToLower().Contains(k) ||
                (x.pi.Pn != null && x.pi.Pn.ToLower().Contains(k)) ||
                (x.pi.Brand != null && x.pi.Brand.ToLower().Contains(k)) ||
                (x.pi.SellOrderId != null && _db.SellOrders.Any(so =>
                    so.Id == x.pi.SellOrderId && so.SellOrderCode.ToLower().Contains(k))));
        }

        var total = await q.CountAsync(cancellationToken);
        var ids = await q
            .OrderByDescending(x => x.pi.CreateTime)
            .ThenByDescending(x => x.pi.Id)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(x => x.pi.Id)
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
