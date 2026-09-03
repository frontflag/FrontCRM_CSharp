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

        var filtered = await InventoryStockItemListFilter.BuildFilteredJoinAsync(
            _db, _dataPermission, query, cancellationToken);

        var ordered = filtered
            .OrderByDescending(x => x.Sin != null ? x.Sin.StockInDate : DateTime.MinValue)
            .ThenByDescending(x => x.Si.CreateTime)
            .ThenBy(x => x.Si.Id);

        var total = await ordered.Select(x => x.Si.Id).CountAsync(cancellationToken);
        var qtyTotals = await filtered
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Inbound = g.Sum(x => x.Si.QtyInbound),
                StockOut = g.Sum(x => x.Si.QtyStockOut),
                Repertory = g.Sum(x => x.Si.QtyRepertory)
            })
            .FirstOrDefaultAsync(cancellationToken);
        var pageRows = await ordered
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(x => new InventoryStockItemListRowDto
            {
                StockItemId = x.Si.Id,
                StockItemCode = x.Si.StockItemCode,
                StockInItemId = x.Si.StockInItemId,
                StockInItemCode = x.Si.StockInItemCode,
                StockInId = x.Si.StockInId,
                StockInCode = x.Sin != null ? x.Sin.StockInCode : null,
                StockInDate = x.Sin != null ? x.Sin.StockInDate : null,
                MaterialId = x.Si.MaterialId,
                LocationId = x.Si.LocationId,
                BatchNo = x.Si.BatchNo,
                ProductionDate = x.Si.ProductionDate,
                PurchasePn = x.Si.PurchasePn,
                PurchaseBrand = x.Si.PurchaseBrand,
                PurchaseOrderItemCode = x.Si.PurchaseOrderItemCode,
                SellOrderItemCode = x.Si.SellOrderItemCode,
                QtyInbound = x.Si.QtyInbound,
                QtyStockOut = x.Si.QtyStockOut,
                QtyRepertory = x.Si.QtyRepertory,
                QtyRepertoryAvailable = x.Si.QtyRepertoryAvailable,
                QtyOccupy = x.Si.QtyOccupy,
                QtySales = x.Si.QtySales,
                PurchasePrice = x.Si.PurchasePrice,
                PurchaseCurrency = x.Si.PurchaseCurrency,
                PurchasePriceUsd = x.Si.PurchasePriceUsd,
                SalesPrice = x.Si.SalesPrice,
                SalesCurrency = x.Si.SalesCurrency,
                SalesPriceUsd = x.Si.SalesPriceUsd,
                VendorId = x.Si.VendorId,
                VendorName = x.Si.VendorName,
                CustomerId = x.Si.CustomerId,
                CustomerName = x.Si.CustomerName,
                RegionType = x.Si.RegionType,
                StockType = x.Si.StockType,
                StockInType = x.Sin != null ? x.Sin.StockInType : (short)0,
                CustomerPn = x.Soi != null ? x.Soi.CustomerPn : null,
                CustomerBrand = x.Soi != null ? x.Soi.CustomerBrand : null,
                PurchaserName = x.Si.PurchaserName,
                SalespersonName = x.Si.SalespersonName,
                CreateTime = x.Si.CreateTime,
                StockAggregateId = x.Si.StockAggregateId,
                WarehouseId = x.Si.WarehouseId,
                WarehouseCode = x.W != null ? x.W.WarehouseCode : null,
                WarehouseName = x.W != null ? x.W.WarehouseName : null,
                OutboundStatus = x.Si.StockOutStatus,
                ProfitOutBizUsd = x.Si.ProfitOutBizUsd
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
