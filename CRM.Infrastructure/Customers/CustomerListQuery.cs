using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Customers;

/// <summary>客户列表：EF 侧 Count + Skip/Take，与《翻页查询规范》主表列表约定一致。</summary>
public sealed class CustomerListQuery : ICustomerListQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public CustomerListQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    /// <inheritdoc />
    public async Task<PagedResult<CustomerInfo>> GetCustomersPagedAsync(
        CustomerQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        if (request.FavoriteCustomerIds != null && request.FavoriteCustomerIds.Count == 0)
        {
            return new PagedResult<CustomerInfo>
            {
                Items = Array.Empty<CustomerInfo>(),
                TotalCount = 0,
                PageIndex = page,
                PageSize = pageSize
            };
        }

        var q = _db.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var k = request.Keyword.Trim().ToLowerInvariant();
            q = q.Where(c =>
                (c.CustomerCode != null && c.CustomerCode.ToLower().Contains(k)) ||
                (c.OfficialName != null && c.OfficialName.ToLower().Contains(k)) ||
                (c.NickName != null && c.NickName.ToLower().Contains(k)) ||
                (c.EnglishOfficialName != null && c.EnglishOfficialName.ToLower().Contains(k)) ||
                (c.Industry != null && c.Industry.ToLower().Contains(k)));
        }

        if (request.Level.HasValue)
            q = q.Where(c => c.Level == request.Level.Value);

        if (request.Type.HasValue)
            q = q.Where(c => c.Type == request.Type.Value);

        if (!string.IsNullOrWhiteSpace(request.Industry))
        {
            var ind = request.Industry.Trim();
            q = q.Where(c => c.Industry == ind);
        }

        if (!string.IsNullOrWhiteSpace(request.Region))
        {
            var r = request.Region.Trim();
            q = q.Where(c =>
                (c.City != null && c.City.Contains(r)) ||
                (c.Province != null && c.Province.Contains(r)));
        }

        if (request.CreatedFrom.HasValue)
        {
            var from = request.CreatedFrom.Value.Date;
            q = q.Where(c => c.CreateTime >= from);
        }

        if (request.CreatedTo.HasValue)
        {
            var toExclusive = request.CreatedTo.Value.Date.AddDays(1);
            q = q.Where(c => c.CreateTime < toExclusive);
        }

        if (!string.IsNullOrWhiteSpace(request.SalesUserId))
            q = q.Where(c => c.SalesUserId == request.SalesUserId);

        if (request.Status.HasValue)
            q = q.Where(c => c.Status == request.Status.Value);

        if (request.FavoriteCustomerIds is { Count: > 0 } fav)
        {
            var favList = fav.ToList();
            q = q.Where(c => favList.Contains(c.Id));
        }

        q = await _dataPermission.ApplyCustomerListDataScopeAsync(request.CurrentUserId, q, cancellationToken);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(c => c.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        await HydrateContactsAsync(items, cancellationToken);

        return new PagedResult<CustomerInfo>
        {
            Items = items,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<PagedResult<CustomerInfo>> GetDeletedCustomersPagedAsync(
        int page,
        int pageSize,
        string? keyword,
        string? currentUserId,
        CancellationToken cancellationToken = default)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);

        var q = _db.Customers.AsNoTracking().IgnoreQueryFilters().Where(c => c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLowerInvariant();
            q = q.Where(c =>
                (c.CustomerCode != null && c.CustomerCode.ToLower().Contains(kw)) ||
                (c.OfficialName != null && c.OfficialName.ToLower().Contains(kw)) ||
                (c.NickName != null && c.NickName.ToLower().Contains(kw)) ||
                (c.EnglishOfficialName != null && c.EnglishOfficialName.ToLower().Contains(kw)));
        }

        q = await _dataPermission.ApplyCustomerListDataScopeAsync(currentUserId, q, cancellationToken);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(c => c.DeletedAt)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        await HydrateContactsAsync(items, cancellationToken);

        return new PagedResult<CustomerInfo>
        {
            Items = items,
            TotalCount = total,
            PageIndex = p,
            PageSize = ps
        };
    }

    /// <inheritdoc />
    public async Task<PagedResult<CustomerInfo>> GetBlackListCustomersPagedAsync(
        int page,
        int pageSize,
        string? keyword,
        string? currentUserId,
        CancellationToken cancellationToken = default)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);

        var q = _db.Customers.AsNoTracking().Where(c => c.BlackList && !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLowerInvariant();
            q = q.Where(c =>
                (c.CustomerCode != null && c.CustomerCode.ToLower().Contains(kw)) ||
                (c.OfficialName != null && c.OfficialName.ToLower().Contains(kw)) ||
                (c.NickName != null && c.NickName.ToLower().Contains(kw)) ||
                (c.EnglishOfficialName != null && c.EnglishOfficialName.ToLower().Contains(kw)));
        }

        q = await _dataPermission.ApplyCustomerListDataScopeAsync(currentUserId, q, cancellationToken);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(c => c.BlackListAt ?? c.ModifyTime ?? c.CreateTime)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        await HydrateContactsAsync(items, cancellationToken);

        return new PagedResult<CustomerInfo>
        {
            Items = items,
            TotalCount = total,
            PageIndex = p,
            PageSize = ps
        };
    }

    /// <inheritdoc />
    public async Task<PagedResult<CustomerInfo>> GetFrozenCustomersPagedAsync(
        int page,
        int pageSize,
        string? keyword,
        string? currentUserId,
        CancellationToken cancellationToken = default)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);

        var q = _db.Customers.AsNoTracking().Where(c => c.DisenableStatus && !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLowerInvariant();
            q = q.Where(c =>
                (c.CustomerCode != null && c.CustomerCode.ToLower().Contains(kw)) ||
                (c.OfficialName != null && c.OfficialName.ToLower().Contains(kw)) ||
                (c.NickName != null && c.NickName.ToLower().Contains(kw)) ||
                (c.EnglishOfficialName != null && c.EnglishOfficialName.ToLower().Contains(kw)));
        }

        q = await _dataPermission.ApplyCustomerListDataScopeAsync(currentUserId, q, cancellationToken);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(c => c.ModifyTime ?? c.CreateTime)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        await HydrateContactsAsync(items, cancellationToken);

        return new PagedResult<CustomerInfo>
        {
            Items = items,
            TotalCount = total,
            PageIndex = p,
            PageSize = ps
        };
    }

    private async Task HydrateContactsAsync(IReadOnlyList<CustomerInfo> customers, CancellationToken cancellationToken)
    {
        if (customers.Count == 0)
            return;

        var ids = customers.Select(c => c.Id).Distinct().ToList();
        var contactRows = await _db.CustomerContacts.AsNoTracking()
            .Where(c => ids.Contains(c.CustomerId))
            .ToListAsync(cancellationToken);

        var byCustomer = contactRows.GroupBy(x => x.CustomerId).ToDictionary(g => g.Key, g => (ICollection<CustomerContactInfo>)g.ToList());
        foreach (var c in customers)
            c.Contacts = byCustomer.TryGetValue(c.Id, out var list) ? list : new List<CustomerContactInfo>();
    }
}
