using CRM.Core.Interfaces;
using CRM.Core.Models.Vendor;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Vendors;

/// <summary>供应商列表：EF 侧 Count + Skip/Take。</summary>
public sealed class VendorListQuery : IVendorListQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public VendorListQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    /// <inheritdoc />
    public async Task<PagedResult<VendorInfo>> GetVendorsPagedAsync(
        VendorQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        if (request.FavoriteVendorIds != null && request.FavoriteVendorIds.Count == 0)
        {
            return new PagedResult<VendorInfo>
            {
                Items = Array.Empty<VendorInfo>(),
                TotalCount = 0,
                PageIndex = page,
                PageSize = pageSize
            };
        }

        var q = _db.Vendors.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim().ToLowerInvariant();
            q = q.Where(e =>
                (e.Code != null && e.Code.ToLower().Contains(keyword)) ||
                (e.OfficialName != null && e.OfficialName.ToLower().Contains(keyword)) ||
                (e.NickName != null && e.NickName.ToLower().Contains(keyword)) ||
                (e.EnglishOfficialName != null && e.EnglishOfficialName.ToLower().Contains(keyword)));
        }

        if (request.Status.HasValue)
            q = q.Where(e => e.Status == request.Status.Value);

        if (request.Level.HasValue)
            q = q.Where(e => e.Level == request.Level.Value);

        if (!string.IsNullOrWhiteSpace(request.Industry))
        {
            var ind = request.Industry.Trim();
            q = q.Where(e => e.Industry != null && e.Industry.Contains(ind));
        }

        if (request.Credit.HasValue)
            q = q.Where(e => e.Credit == request.Credit.Value);

        if (request.AscriptionType.HasValue)
            q = q.Where(e => e.AscriptionType == request.AscriptionType.Value);

        if (!string.IsNullOrWhiteSpace(request.PurchaseUserId))
        {
            var pid = request.PurchaseUserId.Trim();
            q = q.Where(e => e.PurchaseUserId == pid);
        }

        if (request.CreatedFrom.HasValue)
        {
            var from = request.CreatedFrom.Value.Date;
            q = q.Where(e => e.CreateTime >= from);
        }

        if (request.CreatedTo.HasValue)
        {
            var toExclusive = request.CreatedTo.Value.Date.AddDays(1);
            q = q.Where(e => e.CreateTime < toExclusive);
        }

        if (request.FavoriteVendorIds is { Count: > 0 } fav)
        {
            var favList = fav.ToList();
            q = q.Where(e => favList.Contains(e.Id));
        }

        q = await _dataPermission.ApplyVendorListDataScopeAsync(request.CurrentUserId, q, cancellationToken);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(e => e.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        await HydrateContactsAsync(items, cancellationToken);

        return new PagedResult<VendorInfo>
        {
            Items = items,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<PagedResult<VendorInfo>> GetDeletedVendorsPagedAsync(
        int page,
        int pageSize,
        string? keyword,
        string? currentUserId,
        CancellationToken cancellationToken = default)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);

        var q = _db.Vendors.AsNoTracking().IgnoreQueryFilters().Where(e => e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLowerInvariant();
            q = q.Where(e =>
                (e.Code != null && e.Code.ToLower().Contains(k)) ||
                (e.OfficialName != null && e.OfficialName.ToLower().Contains(k)) ||
                (e.NickName != null && e.NickName.ToLower().Contains(k)) ||
                (e.EnglishOfficialName != null && e.EnglishOfficialName.ToLower().Contains(k)));
        }

        q = await _dataPermission.ApplyVendorListDataScopeAsync(currentUserId, q, cancellationToken);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(e => e.DeleteTime ?? e.ModifyTime ?? e.CreateTime)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        await HydrateContactsAsync(items, cancellationToken);

        return new PagedResult<VendorInfo>
        {
            Items = items,
            TotalCount = total,
            PageIndex = p,
            PageSize = ps
        };
    }

    /// <inheritdoc />
    public async Task<PagedResult<VendorInfo>> GetBlacklistVendorsPagedAsync(
        int page,
        int pageSize,
        string? keyword,
        string? currentUserId,
        CancellationToken cancellationToken = default)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);

        var q = _db.Vendors.AsNoTracking().Where(e => e.BlackList && !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLowerInvariant();
            q = q.Where(e =>
                (e.Code != null && e.Code.ToLower().Contains(k)) ||
                (e.OfficialName != null && e.OfficialName.ToLower().Contains(k)) ||
                (e.NickName != null && e.NickName.ToLower().Contains(k)) ||
                (e.EnglishOfficialName != null && e.EnglishOfficialName.ToLower().Contains(k)));
        }

        q = await _dataPermission.ApplyVendorListDataScopeAsync(currentUserId, q, cancellationToken);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(e => e.ModifyTime ?? e.CreateTime)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        await HydrateContactsAsync(items, cancellationToken);

        return new PagedResult<VendorInfo>
        {
            Items = items,
            TotalCount = total,
            PageIndex = p,
            PageSize = ps
        };
    }

    /// <inheritdoc />
    public async Task<PagedResult<VendorInfo>> GetFrozenVendorsPagedAsync(
        int page,
        int pageSize,
        string? keyword,
        string? currentUserId,
        CancellationToken cancellationToken = default)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);

        var q = _db.Vendors.AsNoTracking().Where(e => e.IsDisenable && !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLowerInvariant();
            q = q.Where(e =>
                (e.Code != null && e.Code.ToLower().Contains(k)) ||
                (e.OfficialName != null && e.OfficialName.ToLower().Contains(k)) ||
                (e.NickName != null && e.NickName.ToLower().Contains(k)) ||
                (e.EnglishOfficialName != null && e.EnglishOfficialName.ToLower().Contains(k)));
        }

        q = await _dataPermission.ApplyVendorListDataScopeAsync(currentUserId, q, cancellationToken);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(e => e.ModifyTime ?? e.CreateTime)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        await HydrateContactsAsync(items, cancellationToken);

        return new PagedResult<VendorInfo>
        {
            Items = items,
            TotalCount = total,
            PageIndex = p,
            PageSize = ps
        };
    }

    private async Task HydrateContactsAsync(IReadOnlyList<VendorInfo> vendors, CancellationToken cancellationToken)
    {
        if (vendors.Count == 0)
            return;

        var ids = vendors.Select(v => v.Id).Distinct().ToList();
        var rows = await _db.VendorContacts.AsNoTracking()
            .Where(c => ids.Contains(c.VendorId))
            .ToListAsync(cancellationToken);

        var byVendor = rows
            .GroupBy(c => c.VendorId)
            .ToDictionary(
                g => g.Key,
                g => (ICollection<VendorContactInfo>)g.OrderByDescending(c => c.IsMain).ThenBy(c => c.CName).ToList());

        foreach (var v in vendors)
            v.Contacts = byVendor.TryGetValue(v.Id, out var list) ? list : new List<VendorContactInfo>();
    }
}
