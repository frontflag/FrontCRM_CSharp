using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.InventoryCenter;

/// <summary>库存明细列表筛选（分页与看板共用，含数据范围）。</summary>
internal static class InventoryStockItemListFilter
{
    private const int StagnantDays = 90;

    internal sealed class JoinRow
    {
        public StockItem Si { get; set; } = null!;
        public StockIn? Sin { get; set; }
        public WarehouseInfo? W { get; set; }
        public SellOrderItem? Soi { get; set; }
    }

    public static async Task<IQueryable<JoinRow>> BuildFilteredJoinAsync(
        ApplicationDbContext db,
        IDataPermissionService dataPermission,
        InventoryStockItemListQuery query,
        CancellationToken cancellationToken)
    {
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

        var stockItems = db.StockItems.AsNoTracking()
            .Where(si => si.TransferType == null || si.TransferType != StockItemTransferTypeCodes.ManualTransferSource);
        stockItems = await dataPermission.ApplyStockItemListDataScopeAsync(
            query.CurrentUserId,
            stockItems,
            db.SellOrders.AsNoTracking(),
            db.SellOrderItems.AsNoTracking(),
            db.Customers.AsNoTracking(),
            cancellationToken);

        var filtered =
            from si in stockItems
            join sin in db.StockIns.AsNoTracking() on si.StockInId equals sin.Id into sinJoin
            from sin in sinJoin.DefaultIfEmpty()
            join w in db.Warehouses.AsNoTracking() on si.WarehouseId equals w.Id into wj
            from w in wj.DefaultIfEmpty()
            join soi in db.SellOrderItems.AsNoTracking() on si.SellOrderItemId equals soi.Id into soij
            from soi in soij.DefaultIfEmpty()
            select new JoinRow { Si = si, Sin = sin, W = w, Soi = soi };

        if (!string.IsNullOrEmpty(codeNeedle))
            filtered = filtered.Where(x =>
                x.Sin != null && x.Sin.StockInCode.ToLower().Contains(codeNeedle));
        if (!string.IsNullOrEmpty(stockItemCodeNeedle))
            filtered = filtered.Where(x =>
                x.Si.StockItemCode != null && x.Si.StockItemCode.ToLower().Contains(stockItemCodeNeedle));
        if (fromD.HasValue)
            filtered = filtered.Where(x => x.Sin != null && x.Sin.StockInDate >= fromD.Value);
        if (toEx.HasValue)
            filtered = filtered.Where(x => x.Sin != null && x.Sin.StockInDate < toEx.Value);
        if (!string.IsNullOrEmpty(pnNeedle))
            filtered = filtered.Where(x =>
                x.Si.PurchasePn != null && x.Si.PurchasePn.ToLower().Contains(pnNeedle));
        if (!string.IsNullOrEmpty(brandNeedle))
            filtered = filtered.Where(x =>
                x.Si.PurchaseBrand != null && x.Si.PurchaseBrand.ToLower().Contains(brandNeedle));
        if (!string.IsNullOrEmpty(ffNeedle))
            filtered = filtered.Where(x =>
                x.Si.PurchaseOrderItemId != null &&
                db.PurchaseOrderItems.Any(poi =>
                    poi.Id == x.Si.PurchaseOrderItemId &&
                    db.PurchaseOrders.Any(po =>
                        po.Id == poi.PurchaseOrderId &&
                        po.FreightForwarderOrderNo != null &&
                        po.FreightForwarderOrderNo.ToLower().Contains(ffNeedle))));
        if (!string.IsNullOrEmpty(customerNeedle))
            filtered = filtered.Where(x =>
                x.Si.CustomerName != null && x.Si.CustomerName.ToLower().Contains(customerNeedle));
        if (!string.IsNullOrEmpty(vendorNeedle))
            filtered = filtered.Where(x =>
                x.Si.VendorName != null && x.Si.VendorName.ToLower().Contains(vendorNeedle));

        if (!string.IsNullOrEmpty(spUserId))
            filtered = filtered.Where(x => x.Si.SalespersonId != null && x.Si.SalespersonId == spUserId);
        else if (!string.IsNullOrEmpty(spNeedle))
            filtered = filtered.Where(x =>
                x.Si.SalespersonName != null && x.Si.SalespersonName.ToLower().Contains(spNeedle));

        if (!string.IsNullOrEmpty(puUserId))
            filtered = filtered.Where(x => x.Si.PurchaserId != null && x.Si.PurchaserId == puUserId);
        else if (!string.IsNullOrEmpty(puNeedle))
            filtered = filtered.Where(x =>
                x.Si.PurchaserName != null && x.Si.PurchaserName.ToLower().Contains(puNeedle));

        if (outboundFilter is >= 1 and <= 3)
            filtered = filtered.Where(x => x.Si.StockOutStatus == outboundFilter.Value);

        if (!string.IsNullOrEmpty(warehouseIdNeedle))
            filtered = filtered.Where(x => x.Si.WarehouseId == warehouseIdNeedle);

        if (query.StockType is >= 1 and <= 3)
            filtered = filtered.Where(x => x.Si.StockType == query.StockType.Value);

        if (query.StockInType is { } requestedStockInType)
        {
            if (StockInTypeCode.IsPurchaseReceipt(requestedStockInType))
            {
                const short purchase = StockInTypeCode.Purchase;
                const short legacyPurchase = StockInTypeCode.LegacyPurchase;
                filtered = filtered.Where(x =>
                    x.Sin != null
                    && (x.Sin.StockInType == purchase || x.Sin.StockInType == legacyPurchase));
            }
            else if (StockInTypeCode.IsBusinessType(requestedStockInType))
            {
                filtered = filtered.Where(x =>
                    x.Sin != null && x.Sin.StockInType == requestedStockInType);
            }
        }

        if (query.StagnantOnly == true)
        {
            var stagnantThreshold = DateTime.UtcNow.Date.AddDays(-StagnantDays);
            filtered = filtered.Where(x =>
                x.Si.QtyRepertory > 0 &&
                (x.Sin == null ||
                 x.Sin.StockInDate.Year < 2000 ||
                 x.Sin.StockInDate.Date <= stagnantThreshold));
        }

        var rankDim = query.RankDimension?.Trim().ToLowerInvariant();
        var rankKey = query.RankKey?.Trim();
        if (!string.IsNullOrEmpty(rankDim) && !string.IsNullOrEmpty(rankKey))
        {
            var isUnset = string.Equals(rankKey, "_unset", StringComparison.OrdinalIgnoreCase);
            var rankKeyLower = rankKey.ToLowerInvariant();
            filtered = rankDim switch
            {
                "customer" when isUnset => filtered.Where(x => string.IsNullOrWhiteSpace(x.Si.CustomerId)),
                "customer" => filtered.Where(x => x.Si.CustomerId != null && x.Si.CustomerId == rankKey),
                "salesuser" when isUnset => filtered.Where(x => string.IsNullOrWhiteSpace(x.Si.SalespersonId)),
                "salesuser" => filtered.Where(x => x.Si.SalespersonId != null && x.Si.SalespersonId == rankKey),
                "brand" when isUnset => filtered.Where(x => string.IsNullOrWhiteSpace(x.Si.PurchaseBrand)),
                "brand" => filtered.Where(x =>
                    x.Si.PurchaseBrand != null && x.Si.PurchaseBrand.Trim().ToLower() == rankKeyLower),
                "material" when isUnset => filtered.Where(x =>
                    string.IsNullOrWhiteSpace(x.Si.PurchasePn) && string.IsNullOrWhiteSpace(x.Si.PurchaseBrand)),
                "material" => filtered.Where(x =>
                    ((x.Si.PurchasePn ?? "").Trim().ToLower()) + "|" + ((x.Si.PurchaseBrand ?? "").Trim().ToLower())
                    == rankKeyLower),
                _ => filtered
            };
            filtered = filtered.Where(x => x.Si.QtyRepertory > 0);
            if (query.RankCurrency is >= (short)CurrencyCode.RMB)
            {
                var rankCcy = InventoryOnHandCurrency.Normalize(query.RankCurrency.Value);
                filtered = filtered.Where(x => x.Si.PurchaseCurrency == rankCcy);
            }
        }

        if (query.RepertoryHasStock == true)
            filtered = filtered.Where(x => x.Si.QtyRepertory > 0);
        else if (query.RepertoryHasStock == false)
            filtered = filtered.Where(x => x.Si.QtyRepertory == 0);

        return filtered;
    }
}
