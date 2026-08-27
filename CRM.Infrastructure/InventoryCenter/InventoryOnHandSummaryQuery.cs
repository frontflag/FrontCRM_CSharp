using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.InventoryCenter;

public sealed class InventoryOnHandSummaryQuery : IInventoryOnHandSummaryQuery
{
    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public InventoryOnHandSummaryQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    public async Task<InventoryOnHandSummaryPagedResult> GetPagedAsync(
        InventoryOnHandSummaryQueryRequest request,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        request ??= new InventoryOnHandSummaryQueryRequest();
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, IInventoryOnHandSummaryQuery.MaxPageSize);
        var groupByType = request.GroupByStockType;
        var groupByWh = request.GroupByWarehouse;

        var filtered = await BuildFilteredAsync(request, cancellationToken);

        var amountGroups = await filtered
            .Select(si => new
            {
                Cur = si.PurchaseCurrency < 1 || si.PurchaseCurrency > 6
                    ? (short)CurrencyCode.RMB
                    : si.PurchaseCurrency,
                Line = (decimal)si.QtyRepertory * si.PurchasePrice
            })
            .GroupBy(x => x.Cur)
            .Select(g => new { Currency = g.Key, Amount = g.Sum(x => x.Line) })
            .ToListAsync(cancellationToken);
        var currencies = InventoryOnHandCurrency.OrderPresent(amountGroups.Select(a => a.Currency));
        var totalOnHandQty = await filtered.SumAsync(si => (int?)si.QtyRepertory, cancellationToken) ?? 0;
        var totalAmounts = currencies
            .Select(c => new InventoryOnHandAmountDto
            {
                Currency = c,
                Amount = amountGroups.FirstOrDefault(a => a.Currency == c)?.Amount ?? 0m
            })
            .ToList();

        var keyed = filtered.Select(si => new
        {
            PnKey = si.PurchasePn == null ? "" : si.PurchasePn.Trim().ToLower(),
            BrandKey = si.PurchaseBrand == null ? "" : si.PurchaseBrand.Trim().ToLower(),
            DisplayPn = si.PurchasePn,
            DisplayBrand = si.PurchaseBrand,
            StockType = groupByType ? si.StockType : (short)0,
            WarehouseId = groupByWh ? (si.WarehouseId ?? "") : "",
            Qty = si.QtyRepertory,
            Cur = si.PurchaseCurrency < 1 || si.PurchaseCurrency > 6
                ? (short)CurrencyCode.RMB
                : si.PurchaseCurrency,
            Price = si.PurchasePrice
        });

        var grouped = keyed.GroupBy(x => new { x.PnKey, x.BrandKey, x.StockType, x.WarehouseId });
        var projected = grouped.Select(g => new OnHandAggRow
        {
            PnKey = g.Key.PnKey,
            BrandKey = g.Key.BrandKey,
            StockType = g.Key.StockType,
            WarehouseId = g.Key.WarehouseId,
            MaterialModel = g.Max(x => x.DisplayPn),
            PurchaseBrand = g.Max(x => x.DisplayBrand),
            OnHandQty = g.Sum(x => x.Qty),
            AmountRmb = g.Sum(x => x.Cur == 1 ? (decimal)x.Qty * x.Price : 0m),
            AmountUsd = g.Sum(x => x.Cur == 2 ? (decimal)x.Qty * x.Price : 0m),
            AmountEur = g.Sum(x => x.Cur == 3 ? (decimal)x.Qty * x.Price : 0m),
            AmountHkd = g.Sum(x => x.Cur == 4 ? (decimal)x.Qty * x.Price : 0m),
            AmountJpy = g.Sum(x => x.Cur == 5 ? (decimal)x.Qty * x.Price : 0m),
            AmountGbp = g.Sum(x => x.Cur == 6 ? (decimal)x.Qty * x.Price : 0m)
        });

        var ordered = projected
            .OrderBy(x => x.PnKey)
            .ThenBy(x => x.BrandKey)
            .ThenBy(x => x.StockType)
            .ThenBy(x => x.WarehouseId);

        var total = await ordered.CountAsync(cancellationToken);
        var pageRows = await ordered
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        Dictionary<string, WarehouseInfo> warehouseById = new(StringComparer.OrdinalIgnoreCase);
        if (groupByWh && pageRows.Count > 0)
        {
            var ids = pageRows
                .Select(x => x.WarehouseId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (ids.Count > 0)
            {
                var whs = await _db.Warehouses.AsNoTracking()
                    .Where(w => ids.Contains(w.Id))
                    .ToListAsync(cancellationToken);
                foreach (var w in whs)
                {
                    var id = w.Id?.Trim();
                    if (!string.IsNullOrEmpty(id))
                        warehouseById[id] = w;
                }
            }
        }

        var items = pageRows.Select(row =>
        {
            warehouseById.TryGetValue(row.WarehouseId ?? "", out var wh);
            return new InventoryOnHandSummaryRowDto
            {
                MaterialModel = string.IsNullOrWhiteSpace(row.MaterialModel) ? null : row.MaterialModel.Trim(),
                PurchaseBrand = string.IsNullOrWhiteSpace(row.PurchaseBrand) ? null : row.PurchaseBrand.Trim(),
                StockType = groupByType ? row.StockType : null,
                WarehouseId = groupByWh && !string.IsNullOrWhiteSpace(row.WarehouseId)
                    ? row.WarehouseId.Trim()
                    : null,
                WarehouseCode = groupByWh
                    ? (string.IsNullOrWhiteSpace(wh?.WarehouseCode) ? null : wh!.WarehouseCode.Trim())
                    : null,
                WarehouseName = groupByWh
                    ? (string.IsNullOrWhiteSpace(wh?.WarehouseName) ? null : wh!.WarehouseName.Trim())
                    : null,
                OnHandQty = row.OnHandQty,
                Amounts = BuildAmounts(row, currencies)
            };
        }).ToList();

        return new InventoryOnHandSummaryPagedResult
        {
            Items = items,
            TotalCount = total,
            PageIndex = p,
            PageSize = ps,
            Currencies = currencies,
            TotalOnHandQty = totalOnHandQty,
            TotalAmounts = totalAmounts
        };
    }

    private async Task<IQueryable<StockItem>> BuildFilteredAsync(
        InventoryOnHandSummaryQueryRequest request,
        CancellationToken cancellationToken)
    {
        var modelK = request.MaterialModel?.Trim().ToLowerInvariant();
        var brandK = request.PurchaseBrand?.Trim().ToLowerInvariant();
        var wh = request.WarehouseId?.Trim();

        var stockItems = _db.StockItems.AsNoTracking()
            .Where(si => si.QtyRepertory > 0)
            .Where(si => si.TransferType == null || si.TransferType != StockItemTransferTypeCodes.ManualTransferSource);
        stockItems = await _dataPermission.ApplyStockItemListDataScopeAsync(
            request.CurrentUserId,
            stockItems,
            _db.SellOrders.AsNoTracking(),
            _db.SellOrderItems.AsNoTracking(),
            _db.Customers.AsNoTracking(),
            cancellationToken);

        if (!string.IsNullOrEmpty(modelK))
            stockItems = stockItems.Where(si => si.PurchasePn != null && si.PurchasePn.ToLower().Contains(modelK));
        if (!string.IsNullOrEmpty(brandK))
            stockItems = stockItems.Where(si =>
                si.PurchaseBrand != null && si.PurchaseBrand.ToLower().Contains(brandK));
        if (request.StockType is >= 1 and <= 3)
            stockItems = stockItems.Where(si => si.StockType == request.StockType.Value);
        if (!string.IsNullOrEmpty(wh))
            stockItems = stockItems.Where(si => si.WarehouseId == wh);

        return stockItems;
    }

    private static List<InventoryOnHandAmountDto> BuildAmounts(OnHandAggRow row, IReadOnlyList<short> currencies)
    {
        var list = new List<InventoryOnHandAmountDto>(currencies.Count);
        foreach (var c in currencies)
        {
            var amount = c switch
            {
                1 => row.AmountRmb,
                2 => row.AmountUsd,
                3 => row.AmountEur,
                4 => row.AmountHkd,
                5 => row.AmountJpy,
                6 => row.AmountGbp,
                _ => 0m
            };
            list.Add(new InventoryOnHandAmountDto { Currency = c, Amount = amount });
        }

        return list;
    }

    private sealed class OnHandAggRow
    {
        public string PnKey { get; set; } = "";
        public string BrandKey { get; set; } = "";
        public short StockType { get; set; }
        public string WarehouseId { get; set; } = "";
        public string? MaterialModel { get; set; }
        public string? PurchaseBrand { get; set; }
        public int OnHandQty { get; set; }
        public decimal AmountRmb { get; set; }
        public decimal AmountUsd { get; set; }
        public decimal AmountEur { get; set; }
        public decimal AmountHkd { get; set; }
        public decimal AmountJpy { get; set; }
        public decimal AmountGbp { get; set; }
    }
}
