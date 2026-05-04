using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Logistics;

public sealed class ArrivalNoticeListQuery : IArrivalNoticeListQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;

    public ArrivalNoticeListQuery(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<PagedResult<StockInNotify>> GetPagedAsync(
        short? status,
        string? purchaseOrderCode,
        DateTime? expectedArrivalDate,
        string? noticeId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);

        var q = _db.StockInNotifies.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(noticeId))
        {
            var nid = noticeId.Trim();
            q = q.Where(x => x.Id == nid);
        }

        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(purchaseOrderCode))
        {
            var k = purchaseOrderCode.Trim().ToLowerInvariant();
            q = q.Where(x => x.PurchaseOrderCode.ToLower().Contains(k));
        }

        if (expectedArrivalDate.HasValue)
        {
            var d = expectedArrivalDate.Value.Date;
            var next = d.AddDays(1);
            q = q.Where(x => x.ExpectedArrivalDate.HasValue
                             && x.ExpectedArrivalDate.Value >= d
                             && x.ExpectedArrivalDate.Value < next);
        }

        var total = await q.CountAsync(cancellationToken);
        var rows = await q
            .OrderByDescending(x => x.CreateTime)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return new PagedResult<StockInNotify>
            {
                Items = rows,
                TotalCount = total,
                PageIndex = p,
                PageSize = ps
            };
        }

        var poIds = rows.Select(x => x.PurchaseOrderId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var vendorCodes = await _db.PurchaseOrders.AsNoTracking()
            .Where(po => poIds.Contains(po.Id))
            .Select(po => new { po.Id, po.VendorCode })
            .ToDictionaryAsync(x => x.Id, x => x.VendorCode, cancellationToken);

        foreach (var row in rows)
        {
            if (vendorCodes.TryGetValue(row.PurchaseOrderId, out var vc))
                row.VendorCode = vc;
            AttachItemSnapshot(row);
        }

        return new PagedResult<StockInNotify>
        {
            Items = rows,
            TotalCount = total,
            PageIndex = p,
            PageSize = ps
        };
    }

    private static void AttachItemSnapshot(StockInNotify n)
    {
        n.Items = new List<StockInNotifyItemSnapshot>
        {
            new()
            {
                Id = n.Id,
                StockInNotifyId = n.Id,
                PurchaseOrderItemId = n.PurchaseOrderItemId,
                Pn = n.Pn,
                Brand = n.Brand,
                Qty = n.ExpectQty,
                ArrivedQty = n.ReceiveQty,
                PassedQty = n.PassedQty
            }
        };
    }
}
