using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Infrastructure.Data;
using CRM.Infrastructure.PurchaseOrders;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Logistics;

public sealed class ArrivalNoticeListQuery : IArrivalNoticeListQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public ArrivalNoticeListQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    /// <inheritdoc />
    public async Task<PagedResult<StockInNotify>> GetPagedAsync(
        short? status,
        string? purchaseOrderCode,
        string? freightForwarderOrderNo,
        DateTime? expectedArrivalDate,
        string? noticeId,
        short? stockInType,
        int page,
        int pageSize,
        string? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);

        var q = _db.StockInNotifies.AsNoTracking();
        q = await PurchaseOrderDataScopeQueryHelper.FilterArrivalNoticesAsync(
            _dataPermission, _db, currentUserId, q, cancellationToken);

        if (!string.IsNullOrWhiteSpace(noticeId))
        {
            var nid = noticeId.Trim();
            q = q.Where(x => x.Id == nid);
        }

        if (status.HasValue)
            q = q.Where(x => x.Status == status.Value);

        if (stockInType.HasValue)
        {
            var type = StockInTypeCode.NormalizeForNotify(stockInType.Value);
            q = q.Where(x => x.StockInType == type);
        }

        if (!string.IsNullOrWhiteSpace(purchaseOrderCode))
        {
            var k = purchaseOrderCode.Trim().ToLowerInvariant();
            q = q.Where(x =>
                (x.PurchaseOrderCode != null && x.PurchaseOrderCode.ToLower().Contains(k))
                || (x.NoticeCode != null && x.NoticeCode.ToLower().Contains(k)));
        }

        if (!string.IsNullOrWhiteSpace(freightForwarderOrderNo))
        {
            var k = freightForwarderOrderNo.Trim().ToLowerInvariant();
            q = q.Where(x => _db.PurchaseOrders.Any(po =>
                po.Id == x.PurchaseOrderId &&
                po.FreightForwarderOrderNo != null &&
                po.FreightForwarderOrderNo.ToLower().Contains(k)));
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

        if (rows.Count > 0)
            await EnrichRowsAsync(rows, cancellationToken);

        return new PagedResult<StockInNotify>
        {
            Items = rows,
            TotalCount = total,
            PageIndex = p,
            PageSize = ps
        };
    }

    /// <inheritdoc />
    public async Task<List<StockInNotify>> GetByIdsAsync(
        IReadOnlyList<string> ids,
        string? currentUserId = null,
        bool applyDataScope = true,
        CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Count == 0)
            return new List<StockInNotify>();

        var idList = ids
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        if (idList.Count == 0)
            return new List<StockInNotify>();

        var q = _db.StockInNotifies.AsNoTracking()
            .Where(x => !x.IsDeleted && idList.Contains(x.Id));
        if (applyDataScope)
        {
            q = await PurchaseOrderDataScopeQueryHelper.FilterArrivalNoticesAsync(
                _dataPermission, _db, currentUserId, q, cancellationToken);
        }

        var loaded = await q.ToListAsync(cancellationToken);
        if (loaded.Count == 0)
            return new List<StockInNotify>();

        await EnrichRowsAsync(loaded, cancellationToken);

        var byId = loaded.ToDictionary(x => x.Id.Trim(), x => x, StringComparer.OrdinalIgnoreCase);
        var result = new List<StockInNotify>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in ids)
        {
            var key = id.Trim();
            if (!seen.Add(key))
                continue;
            if (byId.TryGetValue(key, out var row))
                result.Add(row);
        }

        return result;
    }

    private async Task EnrichRowsAsync(IReadOnlyList<StockInNotify> rows, CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
            return;

        var poIds = rows.Select(x => x.PurchaseOrderId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var poMeta = poIds.Count == 0
            ? new Dictionary<string, (string? VendorCode, string? FreightForwarderOrderNo)>(StringComparer.OrdinalIgnoreCase)
            : await _db.PurchaseOrders.AsNoTracking()
                .Where(po => poIds.Contains(po.Id))
                .Select(po => new { po.Id, po.VendorCode, po.FreightForwarderOrderNo })
                .ToDictionaryAsync(
                    x => x.Id,
                    x => (x.VendorCode, x.FreightForwarderOrderNo),
                    cancellationToken);

        foreach (var row in rows)
        {
            if (poMeta.TryGetValue(row.PurchaseOrderId, out var meta))
            {
                row.VendorCode = meta.VendorCode;
                row.FreightForwarderOrderNo = meta.FreightForwarderOrderNo;
            }

            AttachItemSnapshot(row);
        }

        var vendorIds = rows
            .Select(x => x.VendorId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (vendorIds.Count == 0)
            return;

        var vendorEnglishMap = await _db.Vendors.AsNoTracking()
            .Where(v => vendorIds.Contains(v.Id) && v.EnglishOfficialName != null && v.EnglishOfficialName != "")
            .Select(v => new { v.Id, v.EnglishOfficialName })
            .ToDictionaryAsync(
                x => x.Id,
                x => x.EnglishOfficialName!.Trim(),
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);
        foreach (var row in rows)
        {
            var vid = row.VendorId?.Trim();
            if (!string.IsNullOrEmpty(vid) && vendorEnglishMap.TryGetValue(vid, out var english))
                row.VendorEnglishName = english;
        }
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
