using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Vendor;
using CRM.Infrastructure.Data;
using CRM.Infrastructure.PurchaseOrders;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.BatchReconciliation;

public sealed class BatchReconciliationListQuery : IBatchReconciliationListQuery
{
    public const int MaxPageSize = 200;
    public const int MaxExportRows = 50000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public BatchReconciliationListQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    public async Task<PagedResult<BatchReconciliationRowDto>> GetPagedAsync(
        BatchReconciliationQueryRequest? request,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, MaxPageSize);

        var q = await BuildReconciliationQueryAsync(request, cancellationToken);
        var total = await q.CountAsync(cancellationToken);

        var raw = await q
            .OrderByDescending(x => x.ib.CreateTime)
            .ThenBy(x => x.ib.GlobalBatchNo)
            .ThenBy(x => x.p != null ? x.p.Code : "")
            .ThenBy(x => x.ob != null ? x.ob.Id : "")
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        var rows = await MapRowsAsync(raw.Select(x => (
            x.ib,
            x.si,
            x.s,
            x.ob,
            x.p,
            x.po,
            x.vendor,
            x.customer,
            x.wh)).ToList(), cancellationToken);

        return new PagedResult<BatchReconciliationRowDto>
        {
            Items = rows,
            TotalCount = total,
            PageIndex = p,
            PageSize = ps
        };
    }

    public async Task<IReadOnlyList<BatchReconciliationConsumptionRowDto>> GetConsumptionByGlobalBatchNoAsync(
        string globalBatchNo,
        string? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        var key = (globalBatchNo ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(key))
            return Array.Empty<BatchReconciliationConsumptionRowDto>();

        var visible = await (await BuildInBatchOnlyQueryAsync(
                new BatchReconciliationQueryRequest { GlobalBatchNo = key, CurrentUserId = currentUserId },
                cancellationToken))
            .AnyAsync(cancellationToken);
        if (!visible)
            return Array.Empty<BatchReconciliationConsumptionRowDto>();

        var outs = await (
            from ob in _db.StockOutBatches.AsNoTracking()
            join p in _db.Packings.AsNoTracking() on ob.PackingId equals p.Id
            join c in _db.Customers.AsNoTracking() on p.CustomerId equals c.Id into cg
            from c in cg.DefaultIfEmpty()
            where ob.GlobalBatchNo == key
            orderby ob.CreateTime descending, p.Code
            select new { ob, p, c }
        ).ToListAsync(cancellationToken);

        var packingIds = outs.Select(x => x.p.Id).Distinct().ToList();
        var outDates = await ResolvePackingStockOutDatesAsync(packingIds, cancellationToken);

        return outs.Select(x => new BatchReconciliationConsumptionRowDto
        {
            StockOutBatchId = x.ob.Id,
            PackingCode = x.p.Code,
            OutQty = x.ob.OutQty,
            StockOutDate = outDates.TryGetValue(x.p.Id, out var d) ? d : null,
            CustomerId = x.p.CustomerId,
            CustomerName = FormatCustomerName(x.c)
        }).ToList();
    }

    public async Task<IReadOnlyList<BatchReconciliationRowDto>> ListForInBatchExportAsync(
        BatchReconciliationQueryRequest? request,
        int maxRows,
        CancellationToken cancellationToken = default)
    {
        var limit = maxRows < 1 ? MaxExportRows : Math.Min(maxRows, MaxExportRows);
        var q = await BuildInBatchOnlyQueryAsync(request, cancellationToken);
        var raw = await q
            .OrderByDescending(x => x.ib.CreateTime)
            .ThenBy(x => x.ib.GlobalBatchNo)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return await MapRowsAsync(raw.Select(x => (
            x.ib, x.si, x.s, (StockOutBatch?)null, (Packing?)null, x.po, x.vendor, (CustomerInfo?)null, x.wh)).ToList(), cancellationToken);
    }

    public async Task<IReadOnlyList<BatchOutExportRowDto>> ListForOutBatchExportAsync(
        BatchReconciliationQueryRequest? request,
        int maxRows,
        CancellationToken cancellationToken = default)
    {
        var limit = maxRows < 1 ? MaxExportRows : Math.Min(maxRows, MaxExportRows);
        var q = (await BuildReconciliationQueryAsync(request, cancellationToken))
            .Where(x => x.ob != null);

        var raw = await q
            .OrderByDescending(x => x.ob!.CreateTime)
            .ThenBy(x => x.ib.GlobalBatchNo)
            .Take(limit)
            .Select(x => new { x.ib, x.ob, x.p, x.si })
            .ToListAsync(cancellationToken);

        var packingIds = raw.Where(x => x.p != null).Select(x => x.p!.Id).Distinct().ToList();
        var outDates = await ResolvePackingStockOutDatesAsync(packingIds, cancellationToken);

        return raw
            .Where(x => x.ob != null && x.p != null)
            .Select(x => new BatchOutExportRowDto
            {
                GlobalBatchNo = x.ib.GlobalBatchNo,
                OutQty = x.ob!.OutQty,
                PackingCode = x.p!.Code,
                StockOutDate = outDates.TryGetValue(x.p.Id, out var d) ? d : null,
                MaterialModel = x.si.PurchasePn,
                Lot = x.ib.Lot
            })
            .ToList();
    }

    private async Task<IQueryable<ReconciliationRow>> BuildReconciliationQueryAsync(
        BatchReconciliationQueryRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new BatchReconciliationQueryRequest();

        var q =
            from ib in _db.StockInBatches.AsNoTracking()
            join si in _db.StockInItems.AsNoTracking() on ib.StockInItemId equals si.Id
            join s in _db.StockIns.AsNoTracking() on si.StockInId equals s.Id
            join ext in _db.StockInItemExtends.AsNoTracking() on si.Id equals ext.Id into extG
            from ext in extG.DefaultIfEmpty()
            join poi in _db.PurchaseOrderItems.AsNoTracking() on ext.PurchaseOrderItemId equals poi.Id into poiG
            from poi in poiG.DefaultIfEmpty()
            join po in _db.PurchaseOrders.AsNoTracking() on poi.PurchaseOrderId equals po.Id into poG
            from po in poG.DefaultIfEmpty()
            join ob in _db.StockOutBatches.AsNoTracking() on ib.GlobalBatchNo equals ob.GlobalBatchNo into obG
            from ob in obG.DefaultIfEmpty()
            join p in _db.Packings.AsNoTracking() on ob.PackingId equals p.Id into pG
            from p in pG.DefaultIfEmpty()
            join vendor in _db.Vendors.AsNoTracking() on s.VendorId equals vendor.Id into vG
            from vendor in vG.DefaultIfEmpty()
            join customer in _db.Customers.AsNoTracking() on p.CustomerId equals customer.Id into cG
            from customer in cG.DefaultIfEmpty()
            join wh in _db.Warehouses.AsNoTracking() on s.WarehouseId equals wh.Id into whG
            from wh in whG.DefaultIfEmpty()
            select new ReconciliationRow
            {
                ib = ib,
                si = si,
                s = s,
                ob = ob,
                p = p,
                po = po,
                vendor = vendor,
                customer = customer,
                wh = wh
            };

        if (!string.IsNullOrWhiteSpace(request.GlobalBatchNo))
        {
            var needle = request.GlobalBatchNo.Trim();
            q = q.Where(x => x.ib.GlobalBatchNo.Contains(needle));
        }

        if (!string.IsNullOrWhiteSpace(request.StockInCode))
        {
            var needle = request.StockInCode.Trim();
            q = q.Where(x => x.s.StockInCode.Contains(needle));
        }

        if (!string.IsNullOrWhiteSpace(request.PackingCode))
        {
            var needle = request.PackingCode.Trim();
            q = q.Where(x => x.p != null && x.p.Code.Contains(needle));
        }

        if (!string.IsNullOrWhiteSpace(request.PurchaseOrderCode))
        {
            var needle = request.PurchaseOrderCode.Trim();
            q = q.Where(x => x.po != null && x.po.PurchaseOrderCode != null && x.po.PurchaseOrderCode.Contains(needle));
        }

        if (!string.IsNullOrWhiteSpace(request.MaterialModel))
        {
            var needle = request.MaterialModel.Trim().ToLower();
            q = q.Where(x =>
                (x.si.PurchasePn != null && x.si.PurchasePn.ToLower().Contains(needle)) ||
                (x.si.PurchaseBrand != null && x.si.PurchaseBrand.ToLower().Contains(needle)) ||
                (x.si.MaterialId != null && x.si.MaterialId.ToLower().Contains(needle)));
        }

        if (!string.IsNullOrWhiteSpace(request.Lot))
        {
            var needle = request.Lot.Trim();
            q = q.Where(x => x.ib.Lot != null && x.ib.Lot.Contains(needle));
        }

        if (!string.IsNullOrWhiteSpace(request.SerialNumber))
        {
            var needle = request.SerialNumber.Trim();
            q = q.Where(x => x.ib.SerialNumber != null && x.ib.SerialNumber.Contains(needle));
        }

        if (!string.IsNullOrWhiteSpace(request.VendorName))
        {
            var needle = request.VendorName.Trim().ToLower();
            q = q.Where(x =>
                x.vendor != null &&
                ((x.vendor.OfficialName != null && x.vendor.OfficialName.ToLower().Contains(needle)) ||
                 (x.vendor.NickName != null && x.vendor.NickName.ToLower().Contains(needle)) ||
                 (x.vendor.Code != null && x.vendor.Code.ToLower().Contains(needle))));
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerName))
        {
            var needle = request.CustomerName.Trim().ToLower();
            q = q.Where(x =>
                x.customer != null &&
                ((x.customer.OfficialName != null && x.customer.OfficialName.ToLower().Contains(needle)) ||
                 (x.customer.NickName != null && x.customer.NickName.ToLower().Contains(needle)) ||
                 (x.customer.CustomerCode != null && x.customer.CustomerCode.ToLower().Contains(needle))));
        }

        if (!string.IsNullOrWhiteSpace(request.Remark))
        {
            var needle = request.Remark.Trim();
            q = q.Where(x =>
                (x.ib.Remark != null && x.ib.Remark.Contains(needle)) ||
                (x.s.Remark != null && x.s.Remark.Contains(needle)));
        }

        return await ApplyPurchaseScopeToReconciliationAsync(request.CurrentUserId, q, cancellationToken);
    }

    private async Task<IQueryable<InBatchRow>> BuildInBatchOnlyQueryAsync(
        BatchReconciliationQueryRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new BatchReconciliationQueryRequest();

        var q =
            from ib in _db.StockInBatches.AsNoTracking()
            join si in _db.StockInItems.AsNoTracking() on ib.StockInItemId equals si.Id
            join s in _db.StockIns.AsNoTracking() on si.StockInId equals s.Id
            join ext in _db.StockInItemExtends.AsNoTracking() on si.Id equals ext.Id into extG
            from ext in extG.DefaultIfEmpty()
            join poi in _db.PurchaseOrderItems.AsNoTracking() on ext.PurchaseOrderItemId equals poi.Id into poiG
            from poi in poiG.DefaultIfEmpty()
            join po in _db.PurchaseOrders.AsNoTracking() on poi.PurchaseOrderId equals po.Id into poG
            from po in poG.DefaultIfEmpty()
            join vendor in _db.Vendors.AsNoTracking() on s.VendorId equals vendor.Id into vG
            from vendor in vG.DefaultIfEmpty()
            join wh in _db.Warehouses.AsNoTracking() on s.WarehouseId equals wh.Id into whG
            from wh in whG.DefaultIfEmpty()
            select new InBatchRow
            {
                ib = ib,
                si = si,
                s = s,
                po = po,
                vendor = vendor,
                wh = wh
            };

        if (!string.IsNullOrWhiteSpace(request.GlobalBatchNo))
        {
            var needle = request.GlobalBatchNo.Trim();
            q = q.Where(x => x.ib.GlobalBatchNo.Contains(needle));
        }

        if (!string.IsNullOrWhiteSpace(request.StockInCode))
        {
            var needle = request.StockInCode.Trim();
            q = q.Where(x => x.s.StockInCode.Contains(needle));
        }

        if (!string.IsNullOrWhiteSpace(request.PackingCode))
        {
            var needle = request.PackingCode.Trim();
            q = q.Where(x =>
                _db.StockOutBatches.Any(ob =>
                    ob.GlobalBatchNo == x.ib.GlobalBatchNo &&
                    _db.Packings.Any(p => p.Id == ob.PackingId && p.Code.Contains(needle))));
        }

        if (!string.IsNullOrWhiteSpace(request.PurchaseOrderCode))
        {
            var needle = request.PurchaseOrderCode.Trim();
            q = q.Where(x => x.po != null && x.po.PurchaseOrderCode != null && x.po.PurchaseOrderCode.Contains(needle));
        }

        if (!string.IsNullOrWhiteSpace(request.MaterialModel))
        {
            var needle = request.MaterialModel.Trim().ToLower();
            q = q.Where(x =>
                (x.si.PurchasePn != null && x.si.PurchasePn.ToLower().Contains(needle)) ||
                (x.si.PurchaseBrand != null && x.si.PurchaseBrand.ToLower().Contains(needle)) ||
                (x.si.MaterialId != null && x.si.MaterialId.ToLower().Contains(needle)));
        }

        if (!string.IsNullOrWhiteSpace(request.Lot))
        {
            var needle = request.Lot.Trim();
            q = q.Where(x => x.ib.Lot != null && x.ib.Lot.Contains(needle));
        }

        if (!string.IsNullOrWhiteSpace(request.SerialNumber))
        {
            var needle = request.SerialNumber.Trim();
            q = q.Where(x => x.ib.SerialNumber != null && x.ib.SerialNumber.Contains(needle));
        }

        if (!string.IsNullOrWhiteSpace(request.VendorName))
        {
            var needle = request.VendorName.Trim().ToLower();
            q = q.Where(x =>
                x.vendor != null &&
                ((x.vendor.OfficialName != null && x.vendor.OfficialName.ToLower().Contains(needle)) ||
                 (x.vendor.NickName != null && x.vendor.NickName.ToLower().Contains(needle)) ||
                 (x.vendor.Code != null && x.vendor.Code.ToLower().Contains(needle))));
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerName))
        {
            var needle = request.CustomerName.Trim().ToLower();
            q = q.Where(x =>
                _db.StockOutBatches.Any(ob =>
                    ob.GlobalBatchNo == x.ib.GlobalBatchNo &&
                    _db.Packings.Any(p =>
                        p.Id == ob.PackingId &&
                        _db.Customers.Any(c =>
                            p.CustomerId == c.Id &&
                            ((c.OfficialName != null && c.OfficialName.ToLower().Contains(needle)) ||
                             (c.NickName != null && c.NickName.ToLower().Contains(needle)) ||
                             (c.CustomerCode != null && c.CustomerCode.ToLower().Contains(needle)))))));
        }

        if (!string.IsNullOrWhiteSpace(request.Remark))
        {
            var needle = request.Remark.Trim();
            q = q.Where(x =>
                (x.ib.Remark != null && x.ib.Remark.Contains(needle)) ||
                (x.s.Remark != null && x.s.Remark.Contains(needle)));
        }

        return await ApplyPurchaseScopeToInBatchAsync(request.CurrentUserId, q, cancellationToken);
    }

    private async Task<IQueryable<ReconciliationRow>> ApplyPurchaseScopeToReconciliationAsync(
        string? currentUserId,
        IQueryable<ReconciliationRow> query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentUserId))
            return query;

        var scopedPo = await PurchaseOrderDataScopeQueryHelper.GetScopedPurchaseOrdersAsync(
            _dataPermission, _db, currentUserId, cancellationToken);

        return query.Where(x => x.po != null && scopedPo.Any(po => po.Id == x.po!.Id));
    }

    private async Task<IQueryable<InBatchRow>> ApplyPurchaseScopeToInBatchAsync(
        string? currentUserId,
        IQueryable<InBatchRow> query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentUserId))
            return query;

        var scopedPo = await PurchaseOrderDataScopeQueryHelper.GetScopedPurchaseOrdersAsync(
            _dataPermission, _db, currentUserId, cancellationToken);

        return query.Where(x => x.po != null && scopedPo.Any(po => po.Id == x.po!.Id));
    }

    private async Task<List<BatchReconciliationRowDto>> MapRowsAsync(
        List<(StockInBatch ib, StockInItem si, StockIn s, StockOutBatch? ob, Packing? p, PurchaseOrder? po, VendorInfo? vendor, CustomerInfo? customer, WarehouseInfo? wh)> raw,
        CancellationToken cancellationToken)
    {
        if (raw.Count == 0)
            return new List<BatchReconciliationRowDto>();

        var globalNos = raw.Select(x => x.ib.GlobalBatchNo).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var outSums = await _db.StockOutBatches.AsNoTracking()
            .Where(ob => globalNos.Contains(ob.GlobalBatchNo))
            .GroupBy(ob => ob.GlobalBatchNo)
            .Select(g => new { GlobalBatchNo = g.Key, Total = g.Sum(x => x.OutQty) })
            .ToListAsync(cancellationToken);
        var outSumMap = outSums.ToDictionary(x => x.GlobalBatchNo, x => x.Total, StringComparer.OrdinalIgnoreCase);

        var packingIds = raw.Where(x => x.p != null).Select(x => x.p!.Id).Distinct().ToList();
        var outDates = await ResolvePackingStockOutDatesAsync(packingIds, cancellationToken);

        return raw.Select(x =>
        {
            outSumMap.TryGetValue(x.ib.GlobalBatchNo, out var totalOut);
            var remaining = x.ib.BatchQty - totalOut;
            return new BatchReconciliationRowDto
            {
                StockInBatchId = x.ib.Id,
                StockOutBatchId = x.ob?.Id,
                GlobalBatchNo = x.ib.GlobalBatchNo,
                WarehouseName = x.wh?.WarehouseName,
                StockInDate = x.s.StockInDate,
                StockInCode = x.s.StockInCode,
                PurchaseOrderCode = x.po?.PurchaseOrderCode,
                FreightForwarderOrderNo = x.po?.FreightForwarderOrderNo,
                VendorId = x.s.VendorId,
                VendorName = FormatVendorName(x.vendor),
                MaterialModel = x.si.PurchasePn,
                MaterialBrand = x.si.PurchaseBrand,
                StockInItemQuantity = x.si.Quantity,
                BatchDimension = x.ib.BatchDimension,
                BatchUnit = x.ib.BatchUnit,
                UnitNo = x.ib.UnitNo,
                BatchQty = x.ib.BatchQty,
                Dc = x.ib.Dc,
                PackageOrigin = x.ib.PackageOrigin,
                WaferOrigin = x.ib.WaferOrigin,
                Lot = x.ib.Lot,
                SerialNumber = x.ib.SerialNumber,
                FirmwareVersion = x.ib.FirmwareVersion,
                PartCode = x.ib.PartCode,
                BatchRemark = x.ib.Remark,
                PackingCode = x.p?.Code,
                CustomerId = x.p?.CustomerId,
                CustomerName = FormatCustomerName(x.customer),
                StockOutDate = x.p != null && outDates.TryGetValue(x.p.Id, out var d) ? d : null,
                OutQty = x.ob?.OutQty,
                TotalOutQty = totalOut,
                RemainingQty = remaining
            };
        }).ToList();
    }

    private async Task<Dictionary<string, DateTime?>> ResolvePackingStockOutDatesAsync(
        List<string> packingIds,
        CancellationToken cancellationToken)
    {
        if (packingIds.Count == 0)
            return new Dictionary<string, DateTime?>(StringComparer.OrdinalIgnoreCase);

        var links = await (
            from soi in _db.StockOutItems.AsNoTracking()
            join so in _db.StockOuts.AsNoTracking() on soi.StockOutId equals so.Id
            where soi.PackingId != null && packingIds.Contains(soi.PackingId)
            select new
            {
                PackingId = soi.PackingId!,
                so.StockOutDate,
                so.CreateTime,
                so.Id
            }
        ).ToListAsync(cancellationToken);

        return links
            .GroupBy(x => x.PackingId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderByDescending(x => x.CreateTime)
                    .ThenByDescending(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.StockOutDate)
                    .FirstOrDefault(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static string? FormatVendorName(VendorInfo? v)
    {
        if (v == null) return null;
        if (!string.IsNullOrWhiteSpace(v.OfficialName)) return v.OfficialName.Trim();
        if (!string.IsNullOrWhiteSpace(v.NickName)) return v.NickName.Trim();
        return string.IsNullOrWhiteSpace(v.Code) ? null : v.Code.Trim();
    }

    private static string? FormatCustomerName(CustomerInfo? c)
    {
        if (c == null) return null;
        if (!string.IsNullOrWhiteSpace(c.OfficialName)) return c.OfficialName.Trim();
        if (!string.IsNullOrWhiteSpace(c.NickName)) return c.NickName.Trim();
        return string.IsNullOrWhiteSpace(c.CustomerCode) ? null : c.CustomerCode.Trim();
    }

    private sealed class ReconciliationRow
    {
        public StockInBatch ib { get; set; } = null!;
        public StockInItem si { get; set; } = null!;
        public StockIn s { get; set; } = null!;
        public StockOutBatch? ob { get; set; }
        public Packing? p { get; set; }
        public PurchaseOrder? po { get; set; }
        public VendorInfo? vendor { get; set; }
        public CustomerInfo? customer { get; set; }
        public WarehouseInfo? wh { get; set; }
    }

    private sealed class InBatchRow
    {
        public StockInBatch ib { get; set; } = null!;
        public StockInItem si { get; set; } = null!;
        public StockIn s { get; set; } = null!;
        public PurchaseOrder? po { get; set; }
        public VendorInfo? vendor { get; set; }
        public WarehouseInfo? wh { get; set; }
    }
}
