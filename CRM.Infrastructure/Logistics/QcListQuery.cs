using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Infrastructure.Data;
using CRM.Infrastructure.PurchaseOrders;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Logistics;

public sealed class QcListQuery : IQcListQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public QcListQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
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

        qcQuery = await PurchaseOrderDataScopeQueryHelper.FilterQcInfosAsync(
            _dataPermission, _db, request?.CurrentUserId, qcQuery, cancellationToken);

        if (request != null)
        {
            if (!string.IsNullOrWhiteSpace(request.Preset) && QcListQuickFilterCodes.IsKnown(request.Preset))
            {
                qcQuery = QcListQuickFilter.Apply(_db, qcQuery, request.Preset.Trim());
            }

            if (!string.IsNullOrWhiteSpace(request.QcId))
            {
                var qid = request.QcId.Trim();
                qcQuery = qcQuery.Where(q => q.Id == qid);
            }

            if (!string.IsNullOrWhiteSpace(request.QcCode))
            {
                var k = request.QcCode.Trim().ToLowerInvariant();
                qcQuery = qcQuery.Where(q => q.QcCode != null && q.QcCode.ToLower().Contains(k));
            }

            if (request.StockInType.HasValue)
            {
                var type = StockInTypeCode.NormalizeForNotify(request.StockInType.Value);
                qcQuery = qcQuery.Where(q => q.StockInType == type);
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
                    ((n.PurchaseOrderCode != null && n.PurchaseOrderCode.ToLower().Contains(k))
                     || (n.NoticeCode != null && n.NoticeCode.ToLower().Contains(k)))));
            }

            if (!string.IsNullOrWhiteSpace(request.FreightForwarderOrderNo))
            {
                var k = request.FreightForwarderOrderNo.Trim().ToLowerInvariant();
                qcQuery = qcQuery.Where(q => _db.StockInNotifies.Any(n =>
                    n.Id == q.StockInNotifyId &&
                    _db.PurchaseOrders.Any(po =>
                        po.Id == n.PurchaseOrderId &&
                        po.FreightForwarderOrderNo != null &&
                        po.FreightForwarderOrderNo.ToLower().Contains(k))));
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

            if (request.SellOrderItemIds is { Count: > 0 })
            {
                var itemIds = request.SellOrderItemIds
                    .Select(id => id.Trim())
                    .Where(id => id.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (itemIds.Count > 0)
                {
                    qcQuery = qcQuery.Where(q =>
                        _db.StockInNotifies.Any(n =>
                            n.Id == q.StockInNotifyId &&
                            !n.IsDeleted &&
                            n.SellOrderItemId != null &&
                            itemIds.Contains(n.SellOrderItemId)) ||
                        _db.QCItems.Any(qi =>
                            qi.QcInfoId == q.Id &&
                            !qi.IsDeleted &&
                            _db.StockInNotifies.Any(n2 =>
                                n2.Id == qi.ArrivalStockInNotifyId &&
                                !n2.IsDeleted &&
                                n2.SellOrderItemId != null &&
                                itemIds.Contains(n2.SellOrderItemId))) ||
                        _db.StockInNotifies.Any(n =>
                            n.Id == q.StockInNotifyId &&
                            !n.IsDeleted &&
                            n.PurchaseOrderItemId != null &&
                            _db.PurchaseOrderItems.Any(poi =>
                                poi.Id == n.PurchaseOrderItemId &&
                                poi.SellOrderItemId != null &&
                                itemIds.Contains(poi.SellOrderItemId))));
                }
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

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, int>> GetQcImageCountsAsync(
        IReadOnlyCollection<string> qcIds,
        CancellationToken cancellationToken = default)
    {
        return await QcListQuickFilter.CountQcImagesByBizIdsAsync(_db, qcIds, cancellationToken);
    }
}
