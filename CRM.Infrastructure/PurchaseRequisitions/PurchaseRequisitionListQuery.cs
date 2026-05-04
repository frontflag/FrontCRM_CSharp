using CRM.Core.Interfaces;
using CRM.Core.Models.Purchase;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.PurchaseRequisitions;

/// <summary>采购申请列表：数据库侧分页（替代控制器内全表 <c>GetAllAsync</c> 再 <c>Skip</c>/<c>Take</c>）。</summary>
public sealed class PurchaseRequisitionListQuery : IPurchaseRequisitionListQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;

    public PurchaseRequisitionListQuery(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<PagedResult<PurchaseRequisitionListPageRow>> GetPagedAsync(
        PurchaseRequisitionListQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var q =
            from pr in _db.PurchaseRequisitions.AsNoTracking()
            join so in _db.SellOrders.AsNoTracking() on pr.SellOrderId equals so.Id into soGroup
            from so in soGroup.DefaultIfEmpty()
            select new { pr, SellOrderCode = so != null ? so.SellOrderCode : (string?)null };

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
            .Select(x => new { x.pr, x.SellOrderCode })
            .ToListAsync(cancellationToken);

        var userIds = slice
            .SelectMany(x => new[] { x.pr.PurchaseUserId, x.pr.CreateByUserId })
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        IReadOnlyDictionary<string, string> userNames = new Dictionary<string, string>(StringComparer.Ordinal);
        if (userIds.Count > 0)
        {
            var idSet = userIds.ToHashSet(StringComparer.Ordinal);
            userNames = await _db.Users.AsNoTracking()
                .Where(u => idSet.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName.Trim(), StringComparer.Ordinal, cancellationToken);
        }

        string? AccountFor(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;
            var key = userId.Trim();
            return userNames.TryGetValue(key, out var name) ? name : null;
        }

        var items = slice.Select(x => new PurchaseRequisitionListPageRow
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
            PurchaseUserId = x.pr.PurchaseUserId,
            PurchaseUserAccount = AccountFor(x.pr.PurchaseUserId),
            QuoteVendorId = x.pr.QuoteVendorId,
            QuoteCost = x.pr.QuoteCost,
            Remark = x.pr.Remark,
            CreateTime = x.pr.CreateTime,
            CreateUserAccount = AccountFor(x.pr.CreateByUserId)
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
