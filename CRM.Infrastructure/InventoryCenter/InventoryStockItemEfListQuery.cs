using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.InventoryCenter;

/// <summary>EF 实现的库存明细列表分页（避免与 Core 筛选 DTO 类名 <see cref="InventoryStockItemListQuery"/> 冲突）。</summary>
public sealed class InventoryStockItemEfListQuery : IInventoryStockItemListQuery
{
    private readonly ApplicationDbContext _db;

    public InventoryStockItemEfListQuery(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<PagedResult<InventoryStockItemListRowDto>> GetPagedAsync(
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
        var outboundFilter = query.OutboundStatus;
        DateTime? fromD = query.StockInDateFrom.HasValue ? query.StockInDateFrom.Value.Date : null;
        DateTime? toEx = query.StockInDateTo.HasValue ? query.StockInDateTo.Value.Date.AddDays(1) : null;

        var baseJoin =
            from si in _db.StockItems.AsNoTracking()
            join sin in _db.StockIns.AsNoTracking() on si.StockInId equals sin.Id
            join w in _db.Warehouses.AsNoTracking() on si.WarehouseId equals w.Id into wj
            from w in wj.DefaultIfEmpty()
            where si.TransferType == null || si.TransferType != StockItemTransferTypeCodes.ManualTransferSource
            select new { si, sin, w };

        var filtered = baseJoin;
        if (!string.IsNullOrEmpty(codeNeedle))
            filtered = filtered.Where(x => x.sin.StockInCode.ToLower().Contains(codeNeedle));
        if (!string.IsNullOrEmpty(stockItemCodeNeedle))
            filtered = filtered.Where(x =>
                x.si.StockItemCode != null && x.si.StockItemCode.ToLower().Contains(stockItemCodeNeedle));
        if (fromD.HasValue)
            filtered = filtered.Where(x => x.sin.StockInDate >= fromD.Value);
        if (toEx.HasValue)
            filtered = filtered.Where(x => x.sin.StockInDate < toEx.Value);
        if (!string.IsNullOrEmpty(pnNeedle))
            filtered = filtered.Where(x =>
                x.si.PurchasePn != null && x.si.PurchasePn.ToLower().Contains(pnNeedle));
        if (!string.IsNullOrEmpty(brandNeedle))
            filtered = filtered.Where(x =>
                x.si.PurchaseBrand != null && x.si.PurchaseBrand.ToLower().Contains(brandNeedle));
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

        var ordered = filtered
            .OrderByDescending(x => x.sin.StockInDate)
            .ThenByDescending(x => x.si.CreateTime)
            .ThenBy(x => x.si.Id);

        var total = await ordered.Select(x => x.si.Id).CountAsync(cancellationToken);
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
                StockInCode = x.sin.StockInCode,
                StockInDate = x.sin.StockInDate,
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
                VendorName = x.si.VendorName,
                CustomerName = x.si.CustomerName,
                RegionType = x.si.RegionType,
                StockType = x.si.StockType,
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

        foreach (var row in pageRows)
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
            row.SellOrderItemCode = string.IsNullOrWhiteSpace(row.SellOrderItemCode) ? null : row.SellOrderItemCode.Trim();
            row.VendorName = string.IsNullOrWhiteSpace(row.VendorName) ? null : row.VendorName.Trim();
            row.CustomerName = string.IsNullOrWhiteSpace(row.CustomerName) ? null : row.CustomerName.Trim();
            row.PurchaserName = string.IsNullOrWhiteSpace(row.PurchaserName) ? null : row.PurchaserName.Trim();
            row.SalespersonName = string.IsNullOrWhiteSpace(row.SalespersonName) ? null : row.SalespersonName.Trim();
            row.WarehouseId = row.WarehouseId?.Trim() ?? string.Empty;
            row.StockAggregateId = row.StockAggregateId?.Trim() ?? string.Empty;
            row.WarehouseCode = string.IsNullOrWhiteSpace(row.WarehouseCode) ? null : row.WarehouseCode.Trim();
            row.WarehouseName = string.IsNullOrWhiteSpace(row.WarehouseName) ? null : row.WarehouseName.Trim();
        }

        return new PagedResult<InventoryStockItemListRowDto>
        {
            Items = pageRows,
            TotalCount = total,
            PageIndex = p,
            PageSize = ps
        };
    }
}
