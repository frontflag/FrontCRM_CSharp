using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.InventoryCenter;

/// <summary>EF 实现的库存明细列表分页（避免与 Core 筛选 DTO 类名 <see cref="InventoryStockItemListQuery"/> 冲突）。</summary>
public sealed class InventoryStockItemEfListQuery : IInventoryStockItemListQuery
{
    private const int StagnantDays = 90;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public InventoryStockItemEfListQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    /// <inheritdoc />
    public async Task<InventoryStockItemListPagedResult> GetPagedAsync(
        InventoryStockItemListQuery? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        query ??= new InventoryStockItemListQuery();
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, IInventoryStockItemListQuery.MaxPageSize);

        var codeNeedle = query.StockInCode?.Trim().ToLowerInvariant();
        var stockItemCodeNeedle = query.StockItemCode?.Trim().ToLowerInvariant();
        var warehouseIdNeedle = query.WarehouseId?.Trim();
        var pnNeedle = query.PurchasePn?.Trim().ToLowerInvariant();
        var brandNeedle = query.PurchaseBrand?.Trim().ToLowerInvariant();
        var customerNeedle = query.CustomerName?.Trim().ToLowerInvariant();
        var vendorNeedle = query.VendorName?.Trim().ToLowerInvariant();
        var spNeedle = query.SalespersonName?.Trim().ToLowerInvariant();
        var puNeedle = query.PurchaserName?.Trim().ToLowerInvariant();
        var spUserId = query.SalespersonUserId?.Trim();
        var puUserId = query.PurchaserUserId?.Trim();
        var ffNeedle = query.FreightForwarderOrderNo?.Trim().ToLowerInvariant();
        var outboundFilter = query.OutboundStatus;
        DateTime? fromD = query.StockInDateFrom.HasValue ? query.StockInDateFrom.Value.Date : null;
        DateTime? toEx = query.StockInDateTo.HasValue ? query.StockInDateTo.Value.Date.AddDays(1) : null;

        var stockItems = _db.StockItems.AsNoTracking()
            .Where(si => si.TransferType == null || si.TransferType != StockItemTransferTypeCodes.ManualTransferSource);
        stockItems = await _dataPermission.ApplyStockItemListDataScopeAsync(
            query.CurrentUserId,
            stockItems,
            _db.SellOrders.AsNoTracking(),
            _db.SellOrderItems.AsNoTracking(),
            _db.Customers.AsNoTracking(),
            cancellationToken);

        var baseJoin =
            from si in stockItems
            join sin in _db.StockIns.AsNoTracking() on si.StockInId equals sin.Id into sinJoin
            from sin in sinJoin.DefaultIfEmpty()
            join w in _db.Warehouses.AsNoTracking() on si.WarehouseId equals w.Id into wj
            from w in wj.DefaultIfEmpty()
            join soi in _db.SellOrderItems.AsNoTracking() on si.SellOrderItemId equals soi.Id into soij
            from soi in soij.DefaultIfEmpty()
            select new { si, sin, w, soi };

        var filtered = baseJoin;
        if (!string.IsNullOrEmpty(codeNeedle))
            filtered = filtered.Where(x =>
                x.sin != null && x.sin.StockInCode.ToLower().Contains(codeNeedle));
        if (!string.IsNullOrEmpty(stockItemCodeNeedle))
            filtered = filtered.Where(x =>
                x.si.StockItemCode != null && x.si.StockItemCode.ToLower().Contains(stockItemCodeNeedle));
        if (fromD.HasValue)
            filtered = filtered.Where(x => x.sin != null && x.sin.StockInDate >= fromD.Value);
        if (toEx.HasValue)
            filtered = filtered.Where(x => x.sin != null && x.sin.StockInDate < toEx.Value);
        if (!string.IsNullOrEmpty(pnNeedle))
            filtered = filtered.Where(x =>
                x.si.PurchasePn != null && x.si.PurchasePn.ToLower().Contains(pnNeedle));
        if (!string.IsNullOrEmpty(brandNeedle))
            filtered = filtered.Where(x =>
                x.si.PurchaseBrand != null && x.si.PurchaseBrand.ToLower().Contains(brandNeedle));
        if (!string.IsNullOrEmpty(ffNeedle))
            filtered = filtered.Where(x =>
                x.si.PurchaseOrderItemId != null &&
                _db.PurchaseOrderItems.Any(poi =>
                    poi.Id == x.si.PurchaseOrderItemId &&
                    _db.PurchaseOrders.Any(po =>
                        po.Id == poi.PurchaseOrderId &&
                        po.FreightForwarderOrderNo != null &&
                        po.FreightForwarderOrderNo.ToLower().Contains(ffNeedle))));
        if (!string.IsNullOrEmpty(customerNeedle))
            filtered = filtered.Where(x =>
                x.si.CustomerName != null && x.si.CustomerName.ToLower().Contains(customerNeedle));
        if (!string.IsNullOrEmpty(vendorNeedle))
            filtered = filtered.Where(x =>
                x.si.VendorName != null && x.si.VendorName.ToLower().Contains(vendorNeedle));

        if (!string.IsNullOrEmpty(spUserId))
            filtered = filtered.Where(x => x.si.SalespersonId != null && x.si.SalespersonId == spUserId);
        else if (!string.IsNullOrEmpty(spNeedle))
            filtered = filtered.Where(x =>
                x.si.SalespersonName != null && x.si.SalespersonName.ToLower().Contains(spNeedle));

        if (!string.IsNullOrEmpty(puUserId))
            filtered = filtered.Where(x => x.si.PurchaserId != null && x.si.PurchaserId == puUserId);
        else if (!string.IsNullOrEmpty(puNeedle))
            filtered = filtered.Where(x =>
                x.si.PurchaserName != null && x.si.PurchaserName.ToLower().Contains(puNeedle));

        if (outboundFilter is >= 1 and <= 3)
            filtered = filtered.Where(x => x.si.StockOutStatus == outboundFilter.Value);

        if (!string.IsNullOrEmpty(warehouseIdNeedle))
            filtered = filtered.Where(x => x.si.WarehouseId == warehouseIdNeedle);

        if (query.StockType is >= 1 and <= 3)
            filtered = filtered.Where(x => x.si.StockType == query.StockType.Value);

        if (query.StockInType is { } requestedStockInType)
        {
            if (StockInTypeCode.IsPurchaseReceipt(requestedStockInType))
            {
                const short purchase = StockInTypeCode.Purchase;
                const short legacyPurchase = StockInTypeCode.LegacyPurchase;
                filtered = filtered.Where(x =>
                    x.sin != null
                    && (x.sin.StockInType == purchase || x.sin.StockInType == legacyPurchase));
            }
            else if (StockInTypeCode.IsBusinessType(requestedStockInType))
            {
                filtered = filtered.Where(x =>
                    x.sin != null && x.sin.StockInType == requestedStockInType);
            }
        }

        if (query.StagnantOnly == true)
        {
            var stagnantThreshold = DateTime.UtcNow.Date.AddDays(-StagnantDays);
            filtered = filtered.Where(x =>
                x.si.QtyRepertory > 0 &&
                (x.sin == null ||
                 x.sin.StockInDate.Year < 2000 ||
                 x.sin.StockInDate.Date <= stagnantThreshold));
        }

        var rankDim = query.RankDimension?.Trim().ToLowerInvariant();
        var rankKey = query.RankKey?.Trim();
        if (!string.IsNullOrEmpty(rankDim) && !string.IsNullOrEmpty(rankKey))
        {
            var isUnset = string.Equals(rankKey, "_unset", StringComparison.OrdinalIgnoreCase);
            var rankKeyLower = rankKey.ToLowerInvariant();
            filtered = rankDim switch
            {
                "customer" when isUnset => filtered.Where(x => string.IsNullOrWhiteSpace(x.si.CustomerId)),
                "customer" => filtered.Where(x => x.si.CustomerId != null && x.si.CustomerId == rankKey),
                "salesuser" when isUnset => filtered.Where(x => string.IsNullOrWhiteSpace(x.si.SalespersonId)),
                "salesuser" => filtered.Where(x => x.si.SalespersonId != null && x.si.SalespersonId == rankKey),
                "brand" when isUnset => filtered.Where(x => string.IsNullOrWhiteSpace(x.si.PurchaseBrand)),
                "brand" => filtered.Where(x =>
                    x.si.PurchaseBrand != null && x.si.PurchaseBrand.Trim().ToLower() == rankKeyLower),
                "material" when isUnset => filtered.Where(x =>
                    string.IsNullOrWhiteSpace(x.si.PurchasePn) && string.IsNullOrWhiteSpace(x.si.PurchaseBrand)),
                "material" => filtered.Where(x =>
                    ((x.si.PurchasePn ?? "").Trim().ToLower()) + "|" + ((x.si.PurchaseBrand ?? "").Trim().ToLower())
                    == rankKeyLower),
                _ => filtered
            };
            filtered = filtered.Where(x => x.si.QtyRepertory > 0);
            if (query.RankCurrency is >= (short)CurrencyCode.RMB)
            {
                var rankCcy = InventoryOnHandCurrency.Normalize(query.RankCurrency.Value);
                filtered = filtered.Where(x => x.si.PurchaseCurrency == rankCcy);
            }
        }

        if (query.RepertoryHasStock == true)
            filtered = filtered.Where(x => x.si.QtyRepertory > 0);
        else if (query.RepertoryHasStock == false)
            filtered = filtered.Where(x => x.si.QtyRepertory == 0);

        var ordered = filtered
            .OrderByDescending(x => x.sin != null ? x.sin.StockInDate : DateTime.MinValue)
            .ThenByDescending(x => x.si.CreateTime)
            .ThenBy(x => x.si.Id);

        var total = await ordered.Select(x => x.si.Id).CountAsync(cancellationToken);
        var qtyTotals = await filtered
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Inbound = g.Sum(x => x.si.QtyInbound),
                StockOut = g.Sum(x => x.si.QtyStockOut),
                Repertory = g.Sum(x => x.si.QtyRepertory)
            })
            .FirstOrDefaultAsync(cancellationToken);
        var pageRows = await ordered
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(x => new InventoryStockItemListRowDto
            {
                StockItemId = x.si.Id,
                StockItemCode = x.si.StockItemCode,
                StockInItemId = x.si.StockInItemId,
                StockInItemCode = x.si.StockInItemCode,
                StockInId = x.si.StockInId,
                StockInCode = x.sin != null ? x.sin.StockInCode : null,
                StockInDate = x.sin != null ? x.sin.StockInDate : null,
                MaterialId = x.si.MaterialId,
                LocationId = x.si.LocationId,
                BatchNo = x.si.BatchNo,
                ProductionDate = x.si.ProductionDate,
                PurchasePn = x.si.PurchasePn,
                PurchaseBrand = x.si.PurchaseBrand,
                PurchaseOrderItemCode = x.si.PurchaseOrderItemCode,
                SellOrderItemCode = x.si.SellOrderItemCode,
                QtyInbound = x.si.QtyInbound,
                QtyStockOut = x.si.QtyStockOut,
                QtyRepertory = x.si.QtyRepertory,
                QtyRepertoryAvailable = x.si.QtyRepertoryAvailable,
                QtyOccupy = x.si.QtyOccupy,
                QtySales = x.si.QtySales,
                PurchasePrice = x.si.PurchasePrice,
                PurchaseCurrency = x.si.PurchaseCurrency,
                PurchasePriceUsd = x.si.PurchasePriceUsd,
                SalesPrice = x.si.SalesPrice,
                SalesCurrency = x.si.SalesCurrency,
                SalesPriceUsd = x.si.SalesPriceUsd,
                VendorId = x.si.VendorId,
                VendorName = x.si.VendorName,
                CustomerId = x.si.CustomerId,
                CustomerName = x.si.CustomerName,
                RegionType = x.si.RegionType,
                StockType = x.si.StockType,
                StockInType = x.sin != null ? x.sin.StockInType : (short)0,
                CustomerPn = x.soi != null ? x.soi.CustomerPn : null,
                CustomerBrand = x.soi != null ? x.soi.CustomerBrand : null,
                PurchaserName = x.si.PurchaserName,
                SalespersonName = x.si.SalespersonName,
                CreateTime = x.si.CreateTime,
                StockAggregateId = x.si.StockAggregateId,
                WarehouseId = x.si.WarehouseId,
                WarehouseCode = x.w != null ? x.w.WarehouseCode : null,
                WarehouseName = x.w != null ? x.w.WarehouseName : null,
                OutboundStatus = x.si.StockOutStatus,
                ProfitOutBizUsd = x.si.ProfitOutBizUsd
            })
            .ToListAsync(cancellationToken);

        await EnrichStockItemListDisplayAsync(pageRows, cancellationToken);

        return new InventoryStockItemListPagedResult
        {
            Items = pageRows,
            TotalCount = total,
            PageIndex = p,
            PageSize = ps,
            TotalQtyInbound = qtyTotals?.Inbound ?? 0,
            TotalQtyStockOut = qtyTotals?.StockOut ?? 0,
            TotalQtyRepertory = qtyTotals?.Repertory ?? 0
        };
    }

    /// <inheritdoc />
    public async Task<List<InventoryStockItemListRowDto>> GetByIdsAsync(
        IReadOnlyList<string> orderedStockItemIds,
        string? currentUserId = null,
        bool applyDataScope = true,
        CancellationToken cancellationToken = default)
    {
        if (orderedStockItemIds == null || orderedStockItemIds.Count == 0)
            return new List<InventoryStockItemListRowDto>();

        var idList = orderedStockItemIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (idList.Count == 0)
            return new List<InventoryStockItemListRowDto>();

        var stockItems = _db.StockItems.AsNoTracking()
            .Where(si =>
                idList.Contains(si.Id) &&
                (si.TransferType == null || si.TransferType != StockItemTransferTypeCodes.ManualTransferSource));
        if (applyDataScope)
        {
            stockItems = await _dataPermission.ApplyStockItemListDataScopeAsync(
                currentUserId,
                stockItems,
                _db.SellOrders.AsNoTracking(),
                _db.SellOrderItems.AsNoTracking(),
                _db.Customers.AsNoTracking(),
                cancellationToken);
        }

        var rows = await (
            from si in stockItems
            join sin in _db.StockIns.AsNoTracking() on si.StockInId equals sin.Id
            join w in _db.Warehouses.AsNoTracking() on si.WarehouseId equals w.Id into wj
            from w in wj.DefaultIfEmpty()
            join soi in _db.SellOrderItems.AsNoTracking() on si.SellOrderItemId equals soi.Id into soij
            from soi in soij.DefaultIfEmpty()
            select new InventoryStockItemListRowDto
            {
                StockItemId = si.Id,
                StockItemCode = si.StockItemCode,
                StockInItemId = si.StockInItemId,
                StockInItemCode = si.StockInItemCode,
                StockInId = si.StockInId,
                StockInCode = sin.StockInCode,
                StockInDate = sin.StockInDate,
                MaterialId = si.MaterialId,
                LocationId = si.LocationId,
                BatchNo = si.BatchNo,
                ProductionDate = si.ProductionDate,
                PurchasePn = si.PurchasePn,
                PurchaseBrand = si.PurchaseBrand,
                PurchaseOrderItemCode = si.PurchaseOrderItemCode,
                SellOrderItemCode = si.SellOrderItemCode,
                QtyInbound = si.QtyInbound,
                QtyStockOut = si.QtyStockOut,
                QtyRepertory = si.QtyRepertory,
                QtyRepertoryAvailable = si.QtyRepertoryAvailable,
                QtyOccupy = si.QtyOccupy,
                QtySales = si.QtySales,
                PurchasePrice = si.PurchasePrice,
                PurchaseCurrency = si.PurchaseCurrency,
                PurchasePriceUsd = si.PurchasePriceUsd,
                SalesPrice = si.SalesPrice,
                SalesCurrency = si.SalesCurrency,
                SalesPriceUsd = si.SalesPriceUsd,
                VendorId = si.VendorId,
                VendorName = si.VendorName,
                CustomerId = si.CustomerId,
                CustomerName = si.CustomerName,
                RegionType = si.RegionType,
                StockType = si.StockType,
                StockInType = sin.StockInType,
                CustomerPn = soi != null ? soi.CustomerPn : null,
                CustomerBrand = soi != null ? soi.CustomerBrand : null,
                PurchaserName = si.PurchaserName,
                SalespersonName = si.SalespersonName,
                CreateTime = si.CreateTime,
                StockAggregateId = si.StockAggregateId,
                WarehouseId = si.WarehouseId,
                WarehouseCode = w != null ? w.WarehouseCode : null,
                WarehouseName = w != null ? w.WarehouseName : null,
                OutboundStatus = si.StockOutStatus,
                ProfitOutBizUsd = si.ProfitOutBizUsd
            }).ToListAsync(cancellationToken);

        await EnrichStockItemListDisplayAsync(rows, cancellationToken);

        var rowById = rows.ToDictionary(r => r.StockItemId, StringComparer.OrdinalIgnoreCase);
        var ordered = new List<InventoryStockItemListRowDto>(orderedStockItemIds.Count);
        foreach (var id in orderedStockItemIds)
        {
            var key = id?.Trim();
            if (string.IsNullOrEmpty(key) || !rowById.TryGetValue(key, out var row))
                continue;
            ordered.Add(row);
        }

        return ordered;
    }

    private async Task EnrichStockItemListDisplayAsync(
        List<InventoryStockItemListRowDto> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
            return;

        var vendorIds = rows
            .Select(r => r.VendorId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (vendorIds.Count > 0)
        {
            var venDict = await _db.Vendors.AsNoTracking()
                .Where(v => vendorIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => v, cancellationToken);
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.VendorId) ||
                    !venDict.TryGetValue(row.VendorId.Trim(), out var ven))
                    continue;
                if (!string.IsNullOrWhiteSpace(ven.OfficialName))
                    row.VendorChineseName = ven.OfficialName.Trim();
                else if (!string.IsNullOrWhiteSpace(ven.NickName))
                    row.VendorChineseName = ven.NickName.Trim();
                if (!string.IsNullOrWhiteSpace(ven.EnglishOfficialName))
                    row.VendorEnglishName = ven.EnglishOfficialName.Trim();
                if (!string.IsNullOrWhiteSpace(ven.Code))
                    row.VendorCode = ven.Code.Trim();
            }
        }

        var stockItemIds = rows.Select(r => r.StockItemId).ToList();
        var poRows = await (
            from si in _db.StockItems.AsNoTracking()
            where stockItemIds.Contains(si.Id) && si.PurchaseOrderItemId != null
            join poi in _db.PurchaseOrderItems.AsNoTracking() on si.PurchaseOrderItemId equals poi.Id
            join po in _db.PurchaseOrders.AsNoTracking() on poi.PurchaseOrderId equals po.Id
            select new { si.Id, po.FreightForwarderOrderNo, po.PurchaseOrderCode }
        ).ToListAsync(cancellationToken);
        var poByStockItemId = poRows
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (!poByStockItemId.TryGetValue(row.StockItemId, out var po))
                continue;
            row.FreightForwarderOrderNo = string.IsNullOrWhiteSpace(po.FreightForwarderOrderNo)
                ? null
                : po.FreightForwarderOrderNo.Trim();
            row.PurchaseOrderCode = string.IsNullOrWhiteSpace(po.PurchaseOrderCode)
                ? null
                : po.PurchaseOrderCode.Trim();
        }

        foreach (var row in rows)
        {
            row.RegionType = RegionTypeCode.Normalize(row.RegionType);
            row.StockItemCode = string.IsNullOrWhiteSpace(row.StockItemCode) ? null : row.StockItemCode.Trim();
            row.StockInItemCode = string.IsNullOrWhiteSpace(row.StockInItemCode) ? null : row.StockInItemCode.Trim();
            row.StockInCode = string.IsNullOrWhiteSpace(row.StockInCode) ? null : row.StockInCode.Trim();
            row.LocationId = string.IsNullOrWhiteSpace(row.LocationId) ? null : row.LocationId.Trim();
            row.BatchNo = string.IsNullOrWhiteSpace(row.BatchNo) ? null : row.BatchNo.Trim();
            row.PurchasePn = string.IsNullOrWhiteSpace(row.PurchasePn) ? null : row.PurchasePn.Trim();
            row.PurchaseBrand = string.IsNullOrWhiteSpace(row.PurchaseBrand) ? null : row.PurchaseBrand.Trim();
            row.PurchaseOrderItemCode = string.IsNullOrWhiteSpace(row.PurchaseOrderItemCode)
                ? null
                : row.PurchaseOrderItemCode.Trim();
            row.PurchaseOrderCode = string.IsNullOrWhiteSpace(row.PurchaseOrderCode) ? null : row.PurchaseOrderCode.Trim();
            row.SellOrderItemCode = string.IsNullOrWhiteSpace(row.SellOrderItemCode) ? null : row.SellOrderItemCode.Trim();
            row.VendorId = string.IsNullOrWhiteSpace(row.VendorId) ? null : row.VendorId.Trim();
            row.VendorName = string.IsNullOrWhiteSpace(row.VendorName) ? null : row.VendorName.Trim();
            row.VendorChineseName = string.IsNullOrWhiteSpace(row.VendorChineseName)
                ? row.VendorName
                : row.VendorChineseName.Trim();
            row.VendorEnglishName = string.IsNullOrWhiteSpace(row.VendorEnglishName) ? null : row.VendorEnglishName.Trim();
            row.VendorCode = string.IsNullOrWhiteSpace(row.VendorCode) ? null : row.VendorCode.Trim();
            row.CustomerId = string.IsNullOrWhiteSpace(row.CustomerId) ? null : row.CustomerId.Trim();
            row.CustomerName = string.IsNullOrWhiteSpace(row.CustomerName) ? null : row.CustomerName.Trim();
            row.CustomerPn = string.IsNullOrWhiteSpace(row.CustomerPn) ? null : row.CustomerPn.Trim();
            row.CustomerBrand = string.IsNullOrWhiteSpace(row.CustomerBrand) ? null : row.CustomerBrand.Trim();
            row.PurchaserName = string.IsNullOrWhiteSpace(row.PurchaserName) ? null : row.PurchaserName.Trim();
            row.SalespersonName = string.IsNullOrWhiteSpace(row.SalespersonName) ? null : row.SalespersonName.Trim();
            row.WarehouseId = row.WarehouseId?.Trim() ?? string.Empty;
            row.StockAggregateId = row.StockAggregateId?.Trim() ?? string.Empty;
            row.WarehouseCode = string.IsNullOrWhiteSpace(row.WarehouseCode) ? null : row.WarehouseCode.Trim();
            row.WarehouseName = string.IsNullOrWhiteSpace(row.WarehouseName) ? null : row.WarehouseName.Trim();
        }
    }
}
