using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Logistics;

public sealed class QcListQuery : IQcListQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;

    public QcListQuery(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<PagedResult<string>> GetPagedQcIdsAsync(
        QcQueryRequest? request,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);

        var qcQuery = _db.QCInfos.AsNoTracking()
            .Where(q => !q.IsDeleted)
            .Where(q => _db.StockInNotifies.Any(n => n.Id == q.StockInNotifyId && !n.IsDeleted));

        if (request != null)
        {
            if (!string.IsNullOrWhiteSpace(request.QcId))
            {
                var qid = request.QcId.Trim();
                qcQuery = qcQuery.Where(q => q.Id == qid);
            }

            if (!string.IsNullOrWhiteSpace(request.VendorName))
            {
                var k = request.VendorName.Trim().ToLowerInvariant();
                qcQuery = qcQuery.Where(q => _db.StockInNotifies.Any(n =>
                    n.Id == q.StockInNotifyId &&
                    n.VendorName != null &&
                    n.VendorName.ToLower().Contains(k)));
            }

            if (!string.IsNullOrWhiteSpace(request.PurchaseOrderCode))
            {
                var k = request.PurchaseOrderCode.Trim().ToLowerInvariant();
                qcQuery = qcQuery.Where(q => _db.StockInNotifies.Any(n =>
                    n.Id == q.StockInNotifyId &&
                    n.PurchaseOrderCode.ToLower().Contains(k)));
            }

            if (!string.IsNullOrWhiteSpace(request.SalesOrderCode))
            {
                var k = request.SalesOrderCode.Trim().ToLowerInvariant();
                qcQuery = qcQuery.Where(q =>
                    _db.StockInNotifies.Any(n =>
                        n.Id == q.StockInNotifyId &&
                        n.PurchaseOrderItemId != null &&
                        _db.PurchaseOrderItems.Any(poi =>
                            poi.Id == n.PurchaseOrderItemId &&
                            poi.SellOrderItemId != null &&
                            _db.SellOrderItems.Any(soi =>
                                soi.Id == poi.SellOrderItemId &&
                                _db.SellOrders.Any(so =>
                                    so.Id == soi.SellOrderId &&
                                    so.SellOrderCode != null &&
                                    so.SellOrderCode.ToLower().Contains(k))))));
            }

            if (!string.IsNullOrWhiteSpace(request.Model))
            {
                var k = request.Model.Trim().ToLowerInvariant();
                qcQuery = qcQuery.Where(q =>
                    _db.StockInNotifies.Any(n =>
                        n.Id == q.StockInNotifyId &&
                        !n.IsDeleted &&
                        ((n.Pn != null && n.Pn.ToLower().Contains(k)) ||
                         (n.Brand != null && n.Brand.ToLower().Contains(k)) ||
                         (n.PurchaseOrderItemId != null &&
                          _db.PurchaseOrderItems.Any(poi =>
                              poi.Id == n.PurchaseOrderItemId &&
                              ((poi.PN != null && poi.PN.ToLower().Contains(k)) ||
                               (poi.Brand != null && poi.Brand.ToLower().Contains(k))))) ||
                         _db.PurchaseOrderItems.Any(poi =>
                             poi.PurchaseOrderId == n.PurchaseOrderId &&
                             ((poi.PN != null && poi.PN.ToLower().Contains(k)) ||
                              (poi.Brand != null && poi.Brand.ToLower().Contains(k)))))) ||
                    _db.QCItems.Any(qi =>
                        qi.QcInfoId == q.Id &&
                        !qi.IsDeleted &&
                        _db.StockInNotifies.Any(n2 =>
                            n2.Id == qi.ArrivalStockInNotifyId &&
                            !n2.IsDeleted &&
                            ((n2.Pn != null && n2.Pn.ToLower().Contains(k)) ||
                             (n2.Brand != null && n2.Brand.ToLower().Contains(k)) ||
                             (n2.PurchaseOrderItemId != null &&
                              _db.PurchaseOrderItems.Any(poi =>
                                  poi.Id == n2.PurchaseOrderItemId &&
                                  ((poi.PN != null && poi.PN.ToLower().Contains(k)) ||
                                   (poi.Brand != null && poi.Brand.ToLower().Contains(k))))) ||
                             _db.PurchaseOrderItems.Any(poi =>
                                 poi.PurchaseOrderId == n2.PurchaseOrderId &&
                                 ((poi.PN != null && poi.PN.ToLower().Contains(k)) ||
                                  (poi.Brand != null && poi.Brand.ToLower().Contains(k))))))));
            }
        }

        var total = await qcQuery.CountAsync(cancellationToken);
        var ids = await qcQuery
            .OrderByDescending(q => q.CreateTime)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(q => q.Id)
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
