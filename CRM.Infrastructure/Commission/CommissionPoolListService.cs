using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Commission;

public sealed class CommissionPoolListService : ICommissionPoolListService
{
    readonly ApplicationDbContext _db;

    public CommissionPoolListService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<CommissionPaged<CommissionPoolListRowDto>> ListAsync(
        CommissionPoolListQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!CommissionPoolListRules.IsSalesStatus(query.SalesCommissionStatus))
            throw new ArgumentOutOfRangeException(nameof(query.SalesCommissionStatus), "销售提成状态无效");
        if (!CommissionPoolListRules.IsPurchaseStatus(query.PurchaseCommissionStatus))
            throw new ArgumentOutOfRangeException(nameof(query.PurchaseCommissionStatus), "采购提成状态无效");
        if (!CommissionPoolListRules.IsReceiptStatus(query.ReceiptProgressStatus))
            throw new ArgumentOutOfRangeException(nameof(query.ReceiptProgressStatus), "收款核销状态无效");
        if (query.StockOutDateFrom is { } from && query.StockOutDateTo is { } to && from > to)
            throw new ArgumentOutOfRangeException(nameof(query.StockOutDateFrom), "出库日起不能晚于止日");

        var (page, pageSize) = CommissionPoolListRules.NormalizePage(query.Page, query.PageSize);
        var q = _db.CommissionPools.AsNoTracking().AsQueryable();

        if (query.SalesCommissionStatus is { } salesStatus)
            q = q.Where(x => x.SalesCommissionStatus == salesStatus);
        if (query.PurchaseCommissionStatus is { } purchaseStatus)
            q = q.Where(x => x.PurchaseCommissionStatus == purchaseStatus);
        if (query.ReceiptProgressStatus is { } receiptStatus)
            q = q.Where(x => x.ReceiptProgressStatus == receiptStatus);
        if (query.StockOutDateFrom is { } dateFrom)
            q = q.Where(x => x.StockOutDate >= dateFrom);
        if (query.StockOutDateTo is { } dateTo)
            q = q.Where(x => x.StockOutDate <= dateTo);

        var salesUserId = CommissionPoolListRules.NormalizeUserId(query.SalesUserId);
        if (salesUserId != null)
            q = ApplySalesOwner(q, salesUserId);

        var purchaseUserId = CommissionPoolListRules.NormalizeUserId(query.PurchaseUserId);
        if (purchaseUserId != null)
            q = ApplyPurchaseOwner(q, purchaseUserId);

        var restrictSales = CommissionPoolListRules.NormalizeUserId(query.RestrictSalesUserId);
        var restrictPurchase = CommissionPoolListRules.NormalizeUserId(query.RestrictPurchaseUserId);
        if (query.RestrictEither && restrictSales != null && restrictPurchase != null)
        {
            q = q.Where(x =>
                x.SalesUserId == restrictSales
                || _db.SellOrders.IgnoreQueryFilters().Any(o => o.Id == x.SellOrderId && o.SalesUserId == restrictSales)
                || x.PurchaseUserId == restrictPurchase
                || _db.PurchaseOrders.IgnoreQueryFilters().Any(o =>
                    o.Id == x.PurchaseOrderId && o.PurchaseUserId == restrictPurchase));
        }
        else
        {
            if (restrictSales != null)
                q = ApplySalesOwner(q, restrictSales);
            if (restrictPurchase != null)
                q = ApplyPurchaseOwner(q, restrictPurchase);
        }

        var purchasePn = CommissionPoolListRules.NormalizeKeyword(query.PurchasePn);
        if (purchasePn != null)
        {
            var pn = purchasePn.ToLower();
            q = q.Where(x =>
                _db.StockOutItems.IgnoreQueryFilters().Any(i =>
                    i.Id == x.StockOutItemId
                    && i.PurchasePn != null
                    && i.PurchasePn.ToLower().Contains(pn))
                || _db.SellOrderItems.IgnoreQueryFilters().Any(i =>
                    i.Id == x.SellOrderItemId
                    && i.PN != null
                    && i.PN.ToLower().Contains(pn)));
        }

        var keyword = CommissionPoolListRules.NormalizeKeyword(query.Keyword);
        if (keyword != null)
        {
            var nameIds = await _db.Users.AsNoTracking()
                .Where(u => u.UserName.Contains(keyword)
                            || (u.RealName != null && u.RealName.Contains(keyword)))
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);
            var sellOrderIdsByName = await _db.SellOrders.AsNoTracking()
                .Where(o =>
                    (o.SalesUserName != null && o.SalesUserName.Contains(keyword))
                    || (o.SalesUserId != null && nameIds.Contains(o.SalesUserId)))
                .Select(o => o.Id)
                .ToListAsync(cancellationToken);
            var purchaseOrderIdsByName = await _db.PurchaseOrders.AsNoTracking()
                .Where(o =>
                    (o.PurchaseUserName != null && o.PurchaseUserName.Contains(keyword))
                    || (o.PurchaseUserId != null && nameIds.Contains(o.PurchaseUserId)))
                .Select(o => o.Id)
                .ToListAsync(cancellationToken);
            q = q.Where(x =>
                x.StockOutCode.Contains(keyword)
                || (x.SellOrderCode != null && x.SellOrderCode.Contains(keyword))
                || (x.SellOrderItemCode != null && x.SellOrderItemCode.Contains(keyword))
                || (x.PurchaseOrderCode != null && x.PurchaseOrderCode.Contains(keyword))
                || (x.PurchaseOrderItemCode != null && x.PurchaseOrderItemCode.Contains(keyword))
                || _db.StockOutItems.IgnoreQueryFilters().Any(i =>
                    i.Id == x.StockOutItemId
                    && i.StockOutItemCode != null
                    && i.StockOutItemCode.Contains(keyword))
                || (x.SalesUserId != null && nameIds.Contains(x.SalesUserId))
                || (x.PurchaseUserId != null && nameIds.Contains(x.PurchaseUserId))
                || (x.SellOrderId != null && sellOrderIdsByName.Contains(x.SellOrderId))
                || (x.PurchaseOrderId != null && purchaseOrderIdsByName.Contains(x.PurchaseOrderId)));
        }

        var total = await q.CountAsync(cancellationToken);
        var rows = await q
            .OrderByDescending(x => x.StockOutDate)
            .ThenByDescending(x => x.StockOutCode)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var sellOrderIds = DistinctIds(rows.Select(x => x.SellOrderId));
        var purchaseOrderIds = DistinctIds(rows.Select(x => x.PurchaseOrderId));
        var sellOrders = new Dictionary<string, (string? SalesUserId, string? SalesUserName)>(StringComparer.OrdinalIgnoreCase);
        if (sellOrderIds.Count > 0)
        {
            var list = await _db.SellOrders.AsNoTracking().IgnoreQueryFilters()
                .Where(o => sellOrderIds.Contains(o.Id))
                .Select(o => new { o.Id, o.SalesUserId, o.SalesUserName })
                .ToListAsync(cancellationToken);
            foreach (var o in list)
                sellOrders[o.Id] = (o.SalesUserId, o.SalesUserName);
        }

        var purchaseOrders = new Dictionary<string, (string? PurchaseUserId, string? PurchaseUserName)>(StringComparer.OrdinalIgnoreCase);
        if (purchaseOrderIds.Count > 0)
        {
            var list = await _db.PurchaseOrders.AsNoTracking().IgnoreQueryFilters()
                .Where(o => purchaseOrderIds.Contains(o.Id))
                .Select(o => new { o.Id, o.PurchaseUserId, o.PurchaseUserName })
                .ToListAsync(cancellationToken);
            foreach (var o in list)
                purchaseOrders[o.Id] = (o.PurchaseUserId, o.PurchaseUserName);
        }

        var userIds = DistinctIds(
            rows.Select(x => x.SalesUserId)
                .Concat(rows.Select(x => x.PurchaseUserId))
                .Concat(sellOrders.Values.Select(v => v.SalesUserId))
                .Concat(purchaseOrders.Values.Select(v => v.PurchaseUserId)));
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (userIds.Count > 0)
        {
            var users = await _db.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync(cancellationToken);
            foreach (var u in users)
                names[u.Id] = CommissionPoolListRules.DisplayAccount(u.UserName, u.Id);
        }

        string? NameOf(string? userId) =>
            string.IsNullOrWhiteSpace(userId)
                ? null
                : names.TryGetValue(userId, out var n) ? n : null;

        var stockOutItemIds = DistinctIds(rows.Select(x => x.StockOutItemId));
        var stockItems = new Dictionary<string, (string? Pn, string? Brand, int Qty, string? ItemCode)>(
            StringComparer.OrdinalIgnoreCase);
        if (stockOutItemIds.Count > 0)
        {
            var list = await _db.StockOutItems.AsNoTracking().IgnoreQueryFilters()
                .Where(i => stockOutItemIds.Contains(i.Id))
                .Select(i => new { i.Id, i.PurchasePn, i.PurchaseBrand, i.Quantity, i.StockOutItemCode })
                .ToListAsync(cancellationToken);
            foreach (var i in list)
                stockItems[i.Id] = (i.PurchasePn, i.PurchaseBrand, i.Quantity, i.StockOutItemCode);
        }

        var extends = new Dictionary<string, (int Qty, decimal PurchasePrice, short PurchaseCurrency, decimal PurchasePriceUsd, decimal? SalesPrice, short? SalesCurrency, decimal? SalesPriceUsd)>(
            StringComparer.OrdinalIgnoreCase);
        if (stockOutItemIds.Count > 0)
        {
            var list = await _db.StockOutItemExtends.AsNoTracking().IgnoreQueryFilters()
                .Where(e => stockOutItemIds.Contains(e.Id))
                .Select(e => new
                {
                    e.Id,
                    e.QtyStockOut,
                    e.PurchasePrice,
                    e.PurchaseCurrency,
                    e.PurchasePriceUsd,
                    e.SalesPrice,
                    e.SalesCurrency,
                    e.SalesPriceUsd
                })
                .ToListAsync(cancellationToken);
            foreach (var e in list)
                extends[e.Id] = (
                    e.QtyStockOut,
                    e.PurchasePrice,
                    e.PurchaseCurrency,
                    e.PurchasePriceUsd,
                    e.SalesPrice,
                    e.SalesCurrency,
                    e.SalesPriceUsd);
        }

        var sellItemIds = DistinctIds(rows.Select(x => x.SellOrderItemId));
        var sellItems = new Dictionary<string, (string? Pn, string? Brand, decimal Price, short Currency, decimal ConvertPrice)>(
            StringComparer.OrdinalIgnoreCase);
        if (sellItemIds.Count > 0)
        {
            var list = await _db.SellOrderItems.AsNoTracking().IgnoreQueryFilters()
                .Where(i => sellItemIds.Contains(i.Id))
                .Select(i => new { i.Id, i.PN, i.Brand, i.Price, i.Currency, i.ConvertPrice })
                .ToListAsync(cancellationToken);
            foreach (var i in list)
                sellItems[i.Id] = (i.PN, i.Brand, i.Price, i.Currency, i.ConvertPrice);
        }

        var items = rows.Select(x =>
        {
            sellOrders.TryGetValue(x.SellOrderId ?? "", out var so);
            purchaseOrders.TryGetValue(x.PurchaseOrderId ?? "", out var po);
            var hasItem = stockItems.TryGetValue(x.StockOutItemId, out var soItem);
            var hasExtend = extends.TryGetValue(x.StockOutItemId, out var ext);
            var hasSell = sellItems.TryGetValue(x.SellOrderItemId ?? "", out var sell);
            var facts = CommissionPoolListRules.ResolveLineFacts(
                hasItem ? soItem.Pn : null,
                hasItem ? soItem.Brand : null,
                hasItem,
                hasItem ? soItem.Qty : 0,
                hasExtend,
                hasExtend ? ext.Qty : 0,
                hasExtend ? ext.PurchasePrice : 0m,
                hasExtend ? ext.PurchaseCurrency : (short)0,
                hasExtend ? ext.PurchasePriceUsd : 0m,
                hasExtend ? ext.SalesPrice : null,
                hasExtend ? ext.SalesCurrency : null,
                hasExtend ? ext.SalesPriceUsd : null,
                hasSell ? sell.Pn : null,
                hasSell ? sell.Brand : null,
                hasSell ? sell.Price : null,
                hasSell ? sell.Currency : null,
                hasSell ? sell.ConvertPrice : null);
            var salesUserId = CommissionPoolListRules.FirstNonEmpty(x.SalesUserId, so.SalesUserId);
            var purchaseUserId = CommissionPoolListRules.FirstNonEmpty(x.PurchaseUserId, po.PurchaseUserId);
            return new CommissionPoolListRowDto
            {
                Id = x.Id,
                StockOutItemId = x.StockOutItemId,
                StockOutId = x.StockOutId,
                StockOutCode = x.StockOutCode,
                StockOutItemCode = CommissionPoolListRules.FirstNonEmpty(
                    hasItem ? soItem.ItemCode : null,
                    x.StockOutCode),
                StockOutDate = x.StockOutDate,
                SalesUserId = salesUserId,
                SalesUserName = CommissionPoolListRules.FirstNonEmpty(NameOf(salesUserId), so.SalesUserName),
                PurchaseUserId = purchaseUserId,
                PurchaseUserName = CommissionPoolListRules.FirstNonEmpty(NameOf(purchaseUserId), po.PurchaseUserName),
                PurchasePn = facts.PurchasePn,
                PurchaseBrand = facts.PurchaseBrand,
                PurchasePrice = facts.PurchasePrice,
                PurchaseCurrency = facts.PurchaseCurrency,
                PurchasePriceUsd = facts.PurchasePriceUsd,
                SalesPrice = facts.SalesPrice,
                SalesCurrency = facts.SalesCurrency,
                SalesPriceUsd = facts.SalesPriceUsd,
                QtyStockOut = facts.QtyStockOut,
                GpUsd = x.GpUsd,
                ReceiptProgressStatus = x.ReceiptProgressStatus,
                ReceiptDate = x.ReceiptDate,
                SellOrderId = x.SellOrderId,
                SellOrderCode = x.SellOrderCode,
                SellOrderItemCode = x.SellOrderItemCode,
                PurchaseOrderId = x.PurchaseOrderId,
                PurchaseOrderCode = x.PurchaseOrderCode,
                PurchaseOrderItemCode = x.PurchaseOrderItemCode,
                SalesCommissionStatus = x.SalesCommissionStatus,
                PurchaseCommissionStatus = x.PurchaseCommissionStatus
            };
        }).ToList();

        return new CommissionPaged<CommissionPoolListRowDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    IQueryable<CommissionPool> ApplySalesOwner(IQueryable<CommissionPool> q, string userId) =>
        q.Where(x =>
            x.SalesUserId == userId
            || _db.SellOrders.IgnoreQueryFilters().Any(o => o.Id == x.SellOrderId && o.SalesUserId == userId));

    IQueryable<CommissionPool> ApplyPurchaseOwner(IQueryable<CommissionPool> q, string userId) =>
        q.Where(x =>
            x.PurchaseUserId == userId
            || _db.PurchaseOrders.IgnoreQueryFilters().Any(o =>
                o.Id == x.PurchaseOrderId && o.PurchaseUserId == userId));

    static List<string> DistinctIds(IEnumerable<string?> ids) =>
        ids.Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
