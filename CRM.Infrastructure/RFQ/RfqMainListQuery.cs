using CRM.Core.Interfaces;
using CRM.Core.Models.RFQ;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.RfqListQueries;

/// <summary>需求主表列表：EF 数据库分页（与内存全表方案行为对齐）。</summary>
public sealed class RfqMainListQuery : IRfqMainListQuery
{
    /// <summary>单页上限；与采购主表列表对齐，便于大批量导出等场景。</summary>
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public RfqMainListQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
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

        if (request.StartDate.HasValue)
            q = q.Where(r => r.CreateTime >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            q = q.Where(r => r.CreateTime <= request.EndDate.Value.AddDays(1));

        return q;
    }
}
