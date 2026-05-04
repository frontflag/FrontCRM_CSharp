using CRM.Core.Interfaces;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.StockIns;

/// <summary>入库单列表：在库侧收窄主键后再由服务层组装展示字段（与 <see cref="StockInQueryRequest"/> 语义对齐）。</summary>
public sealed class StockInListQuery : IStockInListQuery
{
    public const int MaxPageSize = 2000;
    private const short TransferStockInType = 3;

    private readonly ApplicationDbContext _db;

    public StockInListQuery(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<PagedResult<string>> GetPagedStockInIdsAsync(
        StockInQueryRequest? request,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);

        var q = _db.StockIns.AsNoTracking()
            .Where(s => s.StockInType != TransferStockInType);

        if (request != null)
        {
            if (!string.IsNullOrWhiteSpace(request.StockInCode))
            {
                var k = request.StockInCode.Trim().ToLowerInvariant();
                q = q.Where(s => s.StockInCode.ToLower().Contains(k));
            }

            if (!string.IsNullOrWhiteSpace(request.WarehouseId))
            {
                var wid = request.WarehouseId.Trim();
                q = q.Where(s => s.WarehouseId == wid);
            }

            if (!string.IsNullOrWhiteSpace(request.Remark))
            {
                var k = request.Remark.Trim().ToLowerInvariant();
                q = q.Where(s => s.Remark != null && s.Remark.ToLower().Contains(k));
            }

            if (request.StockInDateStart.HasValue)
            {
                var d = request.StockInDateStart.Value.Date;
                q = q.Where(s => s.StockInDate >= d);
            }

            if (request.StockInDateEnd.HasValue)
            {
                var endEx = request.StockInDateEnd.Value.Date.AddDays(1);
                q = q.Where(s => s.StockInDate < endEx);
            }

            if (!string.IsNullOrWhiteSpace(request.VendorName))
            {
                var k = request.VendorName.Trim().ToLowerInvariant();
                q = q.Where(s =>
                    s.VendorId != null &&
                    _db.Vendors.Any(v =>
                        v.Id == s.VendorId &&
                        ((v.OfficialName != null && v.OfficialName.ToLower().Contains(k)) ||
                         (v.NickName != null && v.NickName.ToLower().Contains(k)) ||
                         (v.Code != null && v.Code.ToLower().Contains(k)))));
            }

            if (!string.IsNullOrWhiteSpace(request.Model))
            {
                var k = request.Model.Trim().ToLowerInvariant();
                q = q.Where(s => _db.StockInItems.Any(i =>
                    i.StockInId == s.Id &&
                    (i.MaterialId.ToLower().Contains(k) ||
                     (i.PurchasePn != null && i.PurchasePn.ToLower().Contains(k)) ||
                     (i.PurchaseBrand != null && i.PurchaseBrand.ToLower().Contains(k)) ||
                     (i.Remark != null && i.Remark.ToLower().Contains(k)))));
            }

            if (!string.IsNullOrWhiteSpace(request.PurchaseOrderCode))
            {
                var k = request.PurchaseOrderCode.Trim().ToLowerInvariant();
                q = q.Where(s =>
                    (s.SourceCode != null && s.SourceCode.ToLower().Contains(k)) ||
                    _db.StockInItemExtends.Any(e =>
                        e.StockInId == s.Id &&
                        e.PurchaseOrderItemCode != null &&
                        e.PurchaseOrderItemCode.ToLower().Contains(k)) ||
                    _db.StockInItemExtends.Any(e =>
                        e.StockInId == s.Id &&
                        e.PurchaseOrderItemId != null &&
                        _db.PurchaseOrderItems.Any(poi =>
                            poi.Id == e.PurchaseOrderItemId &&
                            _db.PurchaseOrders.Any(po =>
                                po.Id == poi.PurchaseOrderId &&
                                po.PurchaseOrderCode.ToLower().Contains(k)))));
            }

            if (!string.IsNullOrWhiteSpace(request.SourceDisplayNo))
            {
                var k = request.SourceDisplayNo.Trim().ToLowerInvariant();
                q = q.Where(s =>
                    (s.SourceCode != null && s.SourceCode.ToLower().Contains(k)) ||
                    (s.QcCode != null && s.QcCode.ToLower().Contains(k)) ||
                    _db.StockInItemExtends.Any(e =>
                        e.StockInId == s.Id &&
                        e.PurchaseOrderItemCode != null &&
                        e.PurchaseOrderItemCode.ToLower().Contains(k)) ||
                    _db.StockInItemExtends.Any(e =>
                        e.StockInId == s.Id &&
                        e.PurchaseOrderItemId != null &&
                        _db.PurchaseOrderItems.Any(poi =>
                            poi.Id == e.PurchaseOrderItemId &&
                            _db.PurchaseOrders.Any(po =>
                                po.Id == poi.PurchaseOrderId &&
                                po.PurchaseOrderCode.ToLower().Contains(k)))));
            }

            if (!string.IsNullOrWhiteSpace(request.SalesOrderCode))
            {
                var k = request.SalesOrderCode.Trim().ToLowerInvariant();
                q = q.Where(s =>
                    _db.StockInItemExtends.Any(e =>
                        e.StockInId == s.Id &&
                        e.PurchaseOrderItemId != null &&
                        _db.PurchaseOrderItems.Any(poi =>
                            poi.Id == e.PurchaseOrderItemId &&
                            poi.PurchaseOrderId != null &&
                            _db.PurchaseOrderItems.Any(poi2 =>
                                poi2.PurchaseOrderId == poi.PurchaseOrderId &&
                                poi2.SellOrderItemId != null &&
                                _db.SellOrderItems.Any(soi =>
                                    soi.Id == poi2.SellOrderItemId &&
                                    _db.SellOrders.Any(so =>
                                        so.Id == soi.SellOrderId &&
                                        so.SellOrderCode != null &&
                                        so.SellOrderCode.ToLower().Contains(k)))))));
            }
        }

        var total = await q.CountAsync(cancellationToken);
        var ids = await q
            .OrderByDescending(s => s.CreateTime)
            .ThenByDescending(s => s.Id)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(s => s.Id)
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
