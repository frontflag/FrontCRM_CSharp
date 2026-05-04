using CRM.Core.Interfaces;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.StockOuts;

public sealed class StockOutListQuery : IStockOutListQuery
{
    public const int MaxPageSize = 2000;
    private const short TransferStockOutType = 3;

    private readonly ApplicationDbContext _db;

    public StockOutListQuery(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<PagedResult<string>> GetPagedStockOutIdsAsync(
        string? keyword,
        string? sourceCode,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);

        var q = _db.StockOuts.AsNoTracking()
            .Where(so => so.StockOutType != TransferStockOutType);

        if (!string.IsNullOrWhiteSpace(sourceCode))
        {
            var c = sourceCode.Trim().ToLowerInvariant();
            q = q.Where(so => so.SourceCode != null && so.SourceCode.ToLower() == c);
        }
        else if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLowerInvariant();
            q = q.Where(so =>
                so.StockOutCode.ToLower().Contains(k) ||
                (so.SourceCode != null && so.SourceCode.ToLower().Contains(k)) ||
                (so.ShipmentMethod != null && so.ShipmentMethod.ToLower().Contains(k)) ||
                (so.CourierTrackingNo != null && so.CourierTrackingNo.ToLower().Contains(k)) ||
                (so.SellOrderItemId != null &&
                 _db.SellOrderItems.Any(sol =>
                     sol.Id == so.SellOrderItemId &&
                     sol.SellOrderItemCode != null &&
                     sol.SellOrderItemCode.ToLower().Contains(k))));
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
