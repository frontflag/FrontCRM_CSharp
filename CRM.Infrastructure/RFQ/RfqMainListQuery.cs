using CRM.Core.Interfaces;
using CRM.Core.Models.RFQ;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.RfqListQueries;

/// <summary>需求主表列表：EF 数据库分页（与内存全表方案行为对齐）。</summary>
public sealed partial class RfqMainListQuery : IRfqMainListQuery
{
    /// <summary>单页上限；与采购主表列表对齐，便于大批量导出等场景。</summary>
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;
    private readonly ITagFilterService _tagFilterService;

    public RfqMainListQuery(
        ApplicationDbContext db,
        IDataPermissionService dataPermission,
        ITagFilterService tagFilterService)
    {
        _db = db;
        _dataPermission = dataPermission;
        _tagFilterService = tagFilterService;
    }

    /// <inheritdoc />
    public async Task<RfqMainListQueryPage> GetPagedWithAggregatesAsync(
        RFQQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var filtered = await BuildFilteredQueryAsync(request, cancellationToken);

        var total = await filtered.CountAsync(cancellationToken);
        var pending = await filtered.CountAsync(r => r.Status == 0, cancellationToken);
        var processing = await filtered.CountAsync(r => r.Status == 1 || r.Status == 2, cancellationToken);
        var quoted = await filtered.CountAsync(
            r => r.Status == 3 || r.Status == 4 || r.Status == 5,
            cancellationToken);

        var items = await filtered
            .OrderByDescending(r => r.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new RfqMainListQueryPage
        {
            Items = items,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize,
            Aggregates = new RfqMainListAggregates
            {
                Total = total,
                Pending = pending,
                Processing = processing,
                Quoted = quoted
            }
        };
    }

    private async Task<IQueryable<RFQ>> BuildFilteredQueryAsync(
        RFQQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = _db.RFQs.AsNoTracking();
        q = await _dataPermission.ApplyRfqMainListDataScopeAsync(request.CurrentUserId, q, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim().ToLowerInvariant();
            q = q.Where(r =>
                r.RfqCode.ToLower().Contains(kw) ||
                (r.Industry != null && r.Industry.ToLower().Contains(kw)) ||
                (r.Product != null && r.Product.ToLower().Contains(kw)) ||
                (r.Remark != null && r.Remark.ToLower().Contains(kw)));
        }

        if (request.Status.HasValue)
            q = q.Where(r => r.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.CustomerId))
            q = q.Where(r => r.CustomerId == request.CustomerId);

        if (!string.IsNullOrWhiteSpace(request.SalesUserName))
        {
            var acc = request.SalesUserName.Trim().ToLowerInvariant();
            var matchedUserIds = await _db.Users.AsNoTracking()
                .Where(u => u.UserName.ToLower().Contains(acc))
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);
            if (matchedUserIds.Count == 0)
                q = q.Where(r => false);
            else
                q = q.Where(r => r.SalesUserId != null && matchedUserIds.Contains(r.SalesUserId));
        }

        if (!string.IsNullOrWhiteSpace(request.CreateUserName))
        {
            var acc = request.CreateUserName.Trim().ToLowerInvariant();
            var matchedUserIds = await _db.Users.AsNoTracking()
                .Where(u => u.UserName.ToLower().Contains(acc))
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);
            if (matchedUserIds.Count == 0)
                q = q.Where(r => false);
            else
                q = q.Where(r => r.CreateByUserId != null && matchedUserIds.Contains(r.CreateByUserId));
        }

        if (request.StartDate.HasValue)
        {
            var start = SalesAnalyticsDateFilter.ToUtcDateStart(request.StartDate.Value);
            q = q.Where(r => r.CreateTime >= start);
        }

        if (request.EndDate.HasValue)
        {
            var endExclusive = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(request.EndDate.Value);
            q = q.Where(r => r.CreateTime < endExclusive);
        }

        var tagIds = request.TagIds?
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (tagIds is { Count: > 0 })
        {
            var matchedIds = await _tagFilterService.QueryEntityIdsByTagsAsync(new TagFilterRequest
            {
                EntityType = CRM.Core.Constants.RfqTagConstants.EntityType,
                IncludeTagIds = tagIds,
                IncludeLogic = "OR"
            });
            if (matchedIds.Count == 0)
                q = q.Where(r => false);
            else
                q = q.Where(r => matchedIds.Contains(r.Id));
        }

        return q;
    }
}
