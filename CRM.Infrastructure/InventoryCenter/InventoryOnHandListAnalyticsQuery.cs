using System.Globalization;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.InventoryCenter;

/// <summary>库存中心列表看板：时点存量趋势、原币 KPI、加权平均库龄。</summary>
public sealed class InventoryOnHandListAnalyticsQuery : IInventoryOnHandListAnalyticsQuery
{
    private const int TopN = 10;
    private const int TrendDefaultMonths = 12;
    private const int StagnantDays = 90;
    private static readonly short[] TrendChartCurrencies =
    [
        (short)CurrencyCode.RMB,
        (short)CurrencyCode.USD
    ];
    private const int IdChunkSize = 800;
    private const string UnsetSalesUser = "未分配业务员";
    private const string UnsetCustomer = "无客户 / 备货";
    private const string UnsetWarehouse = "无仓库";
    private const string UnsetBrand = "未分配品牌";
    private const string UnsetMaterial = "未分配物料";

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public InventoryOnHandListAnalyticsQuery(
        ApplicationDbContext db,
        IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    public async Task<InventoryOnHandListAnalyticsDashboardDto> GetDashboardAsync(
        InventoryOnHandSummaryQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var bundle = await LoadBundleAsync(request, cancellationToken);
        var layers = bundle.Layers;
        var today = DateTime.UtcNow.Date;
        var stagnantThreshold = today.AddDays(-StagnantDays);

        var currencyLines = maskAmounts
            ? (IReadOnlyList<InventoryOnHandListAnalyticsCurrencyLineDto>)Array.Empty<InventoryOnHandListAnalyticsCurrencyLineDto>()
            : BuildCurrencyLines(layers);

        var stagnantCount = layers.Count(l =>
            !l.StockInDate.HasValue || l.StockInDate.Value.Date <= stagnantThreshold);

        return new InventoryOnHandListAnalyticsDashboardDto
        {
            Context = new InventoryOnHandListAnalyticsContextDto { MaskAmounts = maskAmounts },
            Snapshot = new InventoryOnHandListAnalyticsSnapshotDto
            {
                OnHandQty = layers.Sum(l => l.QtyRepertory),
                CurrencyLines = currencyLines,
                WeightedAvgAgeDays = WeightedAvgAge(layers),
                StagnantLayerCount = stagnantCount
            }
        };
    }

    public async Task<IReadOnlyList<InventoryOnHandListAnalyticsTrendPointDto>> GetTrendsAsync(
        InventoryOnHandSummaryQueryRequest request,
        string groupBy,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var bundle = await LoadBundleAsync(request, cancellationToken);
        var layers = bundle.Layers;
        var outs = bundle.OutEvents;
        var today = DateTime.UtcNow.Date;
        var dateFrom = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(-(TrendDefaultMonths - 1));
        const string normalizedGroupBy = "month";
        var periods = BuildPeriodKeys(dateFrom, today, normalizedGroupBy);
        var currencies = TrendChartCurrencies;

        var result = new List<InventoryOnHandListAnalyticsTrendPointDto>(periods.Count);
        foreach (var period in periods)
        {
            var (_, endExclusive) = ParsePeriodRange(period, normalizedGroupBy);
            var asOf = endExclusive.AddDays(-1).Date;
            if (asOf > today) asOf = today;

            var qtyTotal = 0;
            var amountByCur = currencies.ToDictionary(c => c, _ => 0m);
            foreach (var layer in layers)
            {
                var qty = QtyAt(layer, asOf, outs);
                if (qty <= 0) continue;
                qtyTotal += qty;
                if (!maskAmounts)
                    amountByCur[layer.Currency] += qty * layer.PurchasePrice;
            }

            result.Add(new InventoryOnHandListAnalyticsTrendPointDto
            {
                Period = period,
                OnHandQty = qtyTotal,
                AmountsByCurrency = currencies.Select(c =>
                {
                    var (key, label) = FormatCurrency(c);
                    return new InventoryOnHandListAnalyticsTrendCurrencyAmountDto
                    {
                        CurrencyKey = key,
                        CurrencyLabel = label,
                        Amount = maskAmounts
                            ? null
                            : Math.Round(amountByCur[c], 2, MidpointRounding.AwayFromZero)
                    };
                }).ToList()
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<InventoryOnHandListAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        InventoryOnHandSummaryQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var layers = (await LoadBundleAsync(request, cancellationToken)).Layers;
        var currencies = InventoryOnHandCurrency.OrderPresent(layers.Select(l => l.Currency));
        var groups = new List<InventoryOnHandListAnalyticsBreakdownGroupDto>
        {
            BuildQtyBreakdown("stockType", "库存类型", layers, l => l.StockType.ToString(), FormatStockType),
            BuildQtyBreakdown("warehouse", "仓库", layers, l => string.IsNullOrWhiteSpace(l.WarehouseId) ? "_unset" : l.WarehouseId!, l => l.WarehouseLabel),
            BuildQtyBreakdown("salesUser", "业务员", layers,
                l => string.IsNullOrWhiteSpace(l.SalespersonId) ? "_unset" : l.SalespersonId!,
                l => string.IsNullOrWhiteSpace(l.SalespersonName) ? UnsetSalesUser : l.SalespersonName!),
            BuildAgeBucketQtyBreakdown(layers)
        };

        foreach (var ccy in currencies)
        {
            var (key, label) = FormatCurrency(ccy);
            var inCcy = layers.Where(l => l.Currency == ccy).ToList();
            groups.Add(BuildAmountBreakdown("stockType", "库存类型", key, label, inCcy,
                l => l.StockType.ToString(), FormatStockType, maskAmounts));
            groups.Add(BuildAmountBreakdown("warehouse", "仓库", key, label, inCcy,
                l => string.IsNullOrWhiteSpace(l.WarehouseId) ? "_unset" : l.WarehouseId!, l => l.WarehouseLabel, maskAmounts));
            groups.Add(BuildAmountBreakdown("salesUser", "业务员", key, label, inCcy,
                l => string.IsNullOrWhiteSpace(l.SalespersonId) ? "_unset" : l.SalespersonId!,
                l => string.IsNullOrWhiteSpace(l.SalespersonName) ? UnsetSalesUser : l.SalespersonName!, maskAmounts));
            groups.Add(BuildAgeBucketAmountBreakdown(inCcy, key, label, maskAmounts));
        }

        return groups;
    }

    public async Task<InventoryOnHandListAnalyticsRankingsDto> GetRankingsAsync(
        InventoryOnHandSummaryQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var layers = (await LoadBundleAsync(request, cancellationToken)).Layers;
        var currencies = InventoryOnHandCurrency.OrderPresent(layers.Select(l => l.Currency));

        return new InventoryOnHandListAnalyticsRankingsDto
        {
            CustomerByQty = RankQty(
                layers,
                l => string.IsNullOrWhiteSpace(l.CustomerId) ? "_unset" : l.CustomerId!,
                l => string.IsNullOrWhiteSpace(l.CustomerId) ? UnsetCustomer : (l.CustomerName ?? l.CustomerId!)),
            SalesUserByQty = RankQty(
                layers,
                l => string.IsNullOrWhiteSpace(l.SalespersonId) ? "_unset" : l.SalespersonId!,
                l => string.IsNullOrWhiteSpace(l.SalespersonName) ? UnsetSalesUser : l.SalespersonName!),
            MaterialByQty = RankQty(
                layers,
                MaterialKey,
                MaterialLabel),
            BrandByQty = RankQty(
                layers,
                l => string.IsNullOrWhiteSpace(l.BrandKey) ? "_unset" : l.BrandKey,
                l => string.IsNullOrWhiteSpace(l.PurchaseBrand) ? UnsetBrand : l.PurchaseBrand!.Trim()),
            CustomerByAmount = FacetRankAmount(
                layers, currencies, maskAmounts,
                l => string.IsNullOrWhiteSpace(l.CustomerId) ? "_unset" : l.CustomerId!,
                l => string.IsNullOrWhiteSpace(l.CustomerId) ? UnsetCustomer : (l.CustomerName ?? l.CustomerId!)),
            SalesUserByAmount = FacetRankAmount(
                layers, currencies, maskAmounts,
                l => string.IsNullOrWhiteSpace(l.SalespersonId) ? "_unset" : l.SalespersonId!,
                l => string.IsNullOrWhiteSpace(l.SalespersonName) ? UnsetSalesUser : l.SalespersonName!),
            MaterialByAmount = FacetRankAmount(
                layers, currencies, maskAmounts, MaterialKey, MaterialLabel),
            BrandByAmount = FacetRankAmount(
                layers, currencies, maskAmounts,
                l => string.IsNullOrWhiteSpace(l.BrandKey) ? "_unset" : l.BrandKey,
                l => string.IsNullOrWhiteSpace(l.PurchaseBrand) ? UnsetBrand : l.PurchaseBrand!.Trim())
        };
    }

    private async Task<AnalyticsBundle> LoadBundleAsync(
        InventoryOnHandSummaryQueryRequest request,
        CancellationToken cancellationToken)
    {
        var filtered = await BuildFilteredAsync(request, cancellationToken);
        var joined = from si in filtered
                     join sin in _db.StockIns.AsNoTracking() on si.StockInId equals sin.Id into sinJoin
                     from sin in sinJoin.DefaultIfEmpty()
                     select new
                     {
                         si.Id,
                         si.PurchasePn,
                         si.PurchaseBrand,
                         si.StockType,
                         si.WarehouseId,
                         si.CustomerId,
                         si.CustomerName,
                         si.SalespersonId,
                         si.SalespersonName,
                         si.QtyRepertory,
                         si.QtyInbound,
                         si.PurchasePrice,
                         si.PurchaseCurrency,
                         StockInDate = sin != null && !sin.IsDeleted ? (DateTime?)sin.StockInDate : null
                     };

        var raw = await joined.ToListAsync(cancellationToken);
        var warehouseIds = raw
            .Select(x => x.WarehouseId?.Trim())
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var warehouseById = warehouseIds.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : await _db.Warehouses.AsNoTracking()
                .Where(w => warehouseIds.Contains(w.Id))
                .ToDictionaryAsync(
                    w => w.Id.Trim(),
                    w => string.IsNullOrWhiteSpace(w.WarehouseName)
                        ? (string.IsNullOrWhiteSpace(w.WarehouseCode) ? w.Id.Trim() : w.WarehouseCode.Trim())
                        : w.WarehouseName.Trim(),
                    StringComparer.OrdinalIgnoreCase,
                    cancellationToken);

        var today = DateTime.UtcNow.Date;
        var layers = raw.Select(x =>
        {
            var whId = x.WarehouseId?.Trim() ?? "";
            warehouseById.TryGetValue(whId, out var whName);
            var stockInDate = x.StockInDate.HasValue && x.StockInDate.Value.Year >= 2000
                ? x.StockInDate.Value.Date
                : (DateTime?)null;
            return new LayerRow
            {
                Id = x.Id.Trim(),
                PurchasePn = x.PurchasePn,
                PurchaseBrand = x.PurchaseBrand,
                PnKey = x.PurchasePn == null ? "" : x.PurchasePn.Trim().ToLowerInvariant(),
                BrandKey = x.PurchaseBrand == null ? "" : x.PurchaseBrand.Trim().ToLowerInvariant(),
                StockType = x.StockType,
                WarehouseId = whId,
                WarehouseLabel = string.IsNullOrEmpty(whId) ? UnsetWarehouse : (whName ?? whId),
                CustomerId = x.CustomerId,
                CustomerName = x.CustomerName,
                SalespersonId = x.SalespersonId,
                SalespersonName = x.SalespersonName,
                QtyRepertory = x.QtyRepertory,
                QtyInbound = x.QtyInbound,
                PurchasePrice = x.PurchasePrice,
                Currency = InventoryOnHandCurrency.Normalize(x.PurchaseCurrency),
                StockInDate = stockInDate,
                AgeDays = stockInDate.HasValue ? Math.Max(0, (today - stockInDate.Value).Days) : 0
            };
        }).ToList();

        var outs = await LoadOutEventsAsync(layers.Select(l => l.Id).ToList(), cancellationToken);
        return new AnalyticsBundle(layers, outs);
    }

    private async Task<List<OutEventRow>> LoadOutEventsAsync(
        IReadOnlyList<string> stockItemIds,
        CancellationToken cancellationToken)
    {
        if (stockItemIds.Count == 0)
            return new List<OutEventRow>();

        var result = new List<OutEventRow>();
        foreach (var chunk in Chunk(stockItemIds, IdChunkSize))
        {
            var rows = await (
                from soi in _db.StockOutItems.AsNoTracking()
                join so in _db.StockOuts.AsNoTracking() on soi.StockOutId equals so.Id
                where !soi.IsDeleted && !so.IsDeleted
                where chunk.Contains(soi.StockItemId!)
                where so.Status == 2 || so.Status == 4
                select new
                {
                    StockItemId = soi.StockItemId!,
                    Qty = soi.ActualQty > 0 ? soi.ActualQty : soi.Quantity,
                    OutDate = so.StockOutDate ?? so.CreateTime
                }).ToListAsync(cancellationToken);

            foreach (var r in rows)
            {
                if (r.Qty <= 0) continue;
                result.Add(new OutEventRow
                {
                    StockItemId = r.StockItemId.Trim(),
                    Qty = r.Qty,
                    OutDate = r.OutDate.Date
                });
            }
        }

        return result;
    }

    private async Task<IQueryable<CRM.Core.Models.Inventory.StockItem>> BuildFilteredAsync(
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

    private static int QtyAt(LayerRow layer, DateTime asOfDate, IReadOnlyList<OutEventRow> outs)
    {
        if (!layer.StockInDate.HasValue || layer.StockInDate.Value.Date > asOfDate)
            return 0;
        var outQty = outs
            .Where(o => string.Equals(o.StockItemId, layer.Id, StringComparison.OrdinalIgnoreCase)
                        && o.OutDate <= asOfDate)
            .Sum(o => o.Qty);
        var qty = layer.QtyInbound - outQty;
        return qty > 0 ? qty : 0;
    }

    private static decimal? WeightedAvgAge(IReadOnlyList<LayerRow> layers)
    {
        var eligible = layers.Where(l => l.StockInDate.HasValue).ToList();
        var qty = eligible.Sum(l => l.QtyRepertory);
        if (qty <= 0) return null;
        return Math.Round((decimal)eligible.Sum(l => l.AgeDays * l.QtyRepertory) / qty, 1, MidpointRounding.AwayFromZero);
    }

    private static List<InventoryOnHandListAnalyticsCurrencyLineDto> BuildCurrencyLines(IReadOnlyList<LayerRow> layers)
    {
        return InventoryOnHandCurrency.OrderPresent(layers.Select(l => l.Currency))
            .Select(c =>
            {
                var (key, label) = FormatCurrency(c);
                var amount = layers.Where(l => l.Currency == c).Sum(l => l.QtyRepertory * l.PurchasePrice);
                return new InventoryOnHandListAnalyticsCurrencyLineDto
                {
                    CurrencyKey = key,
                    CurrencyLabel = label,
                    OriginalAmount = Math.Round(amount, 2, MidpointRounding.AwayFromZero)
                };
            })
            .ToList();
    }

    private static InventoryOnHandListAnalyticsBreakdownGroupDto BuildQtyBreakdown(
        string groupKey,
        string groupLabel,
        IReadOnlyList<LayerRow> layers,
        Func<LayerRow, string> keySelector,
        Func<LayerRow, string> labelSelector) =>
        new()
        {
            GroupKey = groupKey,
            GroupLabel = groupLabel,
            Items = BuildBreakdownItems(layers, keySelector, labelSelector, l => l.QtyRepertory)
        };

    private static InventoryOnHandListAnalyticsBreakdownGroupDto BuildAmountBreakdown(
        string groupKey,
        string groupLabel,
        string currencyKey,
        string currencyLabel,
        IReadOnlyList<LayerRow> layers,
        Func<LayerRow, string> keySelector,
        Func<LayerRow, string> labelSelector,
        bool maskAmounts) =>
        new()
        {
            GroupKey = groupKey,
            GroupLabel = groupLabel,
            CurrencyKey = currencyKey,
            CurrencyLabel = currencyLabel,
            Items = BuildBreakdownItems(
                layers,
                keySelector,
                labelSelector,
                l => maskAmounts ? 1m : l.QtyRepertory * l.PurchasePrice)
        };

    private static InventoryOnHandListAnalyticsBreakdownGroupDto BuildAgeBucketQtyBreakdown(IReadOnlyList<LayerRow> layers)
    {
        var buckets = CreateAgeBuckets();
        foreach (var layer in layers.Where(l => l.StockInDate.HasValue))
        {
            var key = ClassifyAgeBucket(layer.AgeDays);
            buckets[key] = (buckets[key].Label, buckets[key].Qty + layer.QtyRepertory);
        }

        return new InventoryOnHandListAnalyticsBreakdownGroupDto
        {
            GroupKey = "ageBucket",
            GroupLabel = "库龄分布",
            Items = ToBreakdownItems(buckets)
        };
    }

    private static InventoryOnHandListAnalyticsBreakdownGroupDto BuildAgeBucketAmountBreakdown(
        IReadOnlyList<LayerRow> layers,
        string currencyKey,
        string currencyLabel,
        bool maskAmounts)
    {
        var buckets = CreateAgeBuckets();
        foreach (var layer in layers.Where(l => l.StockInDate.HasValue))
        {
            var key = ClassifyAgeBucket(layer.AgeDays);
            var add = maskAmounts ? 1m : layer.QtyRepertory * layer.PurchasePrice;
            buckets[key] = (buckets[key].Label, buckets[key].Qty + add);
        }

        return new InventoryOnHandListAnalyticsBreakdownGroupDto
        {
            GroupKey = "ageBucket",
            GroupLabel = "库龄分布",
            CurrencyKey = currencyKey,
            CurrencyLabel = currencyLabel,
            Items = ToBreakdownItems(buckets)
        };
    }

    private static Dictionary<string, (string Label, decimal Qty)> CreateAgeBuckets() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["0_30"] = ("0–30天", 0m),
            ["31_90"] = ("31–90天", 0m),
            ["91_180"] = ("91–180天", 0m),
            ["181_365"] = ("181–365天", 0m),
            ["365_plus"] = ("365天以上", 0m)
        };

    private static List<SalesAnalyticsBreakdownItemDto> ToBreakdownItems(
        Dictionary<string, (string Label, decimal Qty)> buckets)
    {
        var items = buckets.Select(kv => new SalesAnalyticsBreakdownItemDto
        {
            Key = kv.Key,
            Label = kv.Value.Label,
            Value = kv.Value.Qty,
            Ratio = 0
        }).ToList();
        ApplyRatios(items);
        return items;
    }

    private static List<SalesAnalyticsBreakdownItemDto> BuildBreakdownItems(
        IReadOnlyList<LayerRow> layers,
        Func<LayerRow, string> keySelector,
        Func<LayerRow, string> labelSelector,
        Func<LayerRow, decimal> valueSelector)
    {
        var map = new Dictionary<string, (string Label, decimal Value)>(StringComparer.OrdinalIgnoreCase);
        foreach (var layer in layers)
        {
            var key = keySelector(layer);
            var label = labelSelector(layer);
            if (map.TryGetValue(key, out var existing))
                map[key] = (existing.Label, existing.Value + valueSelector(layer));
            else
                map[key] = (label, valueSelector(layer));
        }

        var items = map.Select(kv => new SalesAnalyticsBreakdownItemDto
        {
            Key = kv.Key,
            Label = kv.Value.Label,
            Value = kv.Value.Value,
            Ratio = 0
        }).ToList();
        ApplyRatios(items);
        return items;
    }

    private static List<SalesAnalyticsRankingRowDto> RankQty(
        IReadOnlyList<LayerRow> layers,
        Func<LayerRow, string> keySelector,
        Func<LayerRow, string> labelSelector) =>
        layers
            .GroupBy(keySelector, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = labelSelector(g.First()),
                OrderCount = g.Sum(x => x.QtyRepertory),
                Amount = null
            })
            .OrderByDescending(x => x.OrderCount)
            .Take(TopN)
            .ToList();

    private static List<InventoryOnHandListAnalyticsRankingFacetDto> FacetRankAmount(
        IReadOnlyList<LayerRow> layers,
        IReadOnlyList<short> currencies,
        bool maskAmounts,
        Func<LayerRow, string> keySelector,
        Func<LayerRow, string> labelSelector) =>
        currencies.Select(ccy =>
        {
            var (key, label) = FormatCurrency(ccy);
            var inCcy = layers.Where(l => l.Currency == ccy).ToList();
            return new InventoryOnHandListAnalyticsRankingFacetDto
            {
                CurrencyKey = key,
                CurrencyLabel = label,
                Rows = inCcy
                    .GroupBy(keySelector, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new SalesAnalyticsRankingRowDto
                    {
                        Id = g.Key,
                        Name = labelSelector(g.First()),
                        Amount = maskAmounts
                            ? null
                            : Math.Round(g.Sum(x => x.QtyRepertory * x.PurchasePrice), 2, MidpointRounding.AwayFromZero),
                        OrderCount = g.Sum(x => x.QtyRepertory)
                    })
                    .OrderByDescending(x => x.Amount ?? x.OrderCount)
                    .Take(TopN)
                    .ToList()
            };
        }).ToList();

    private static string MaterialKey(LayerRow l)
    {
        if (string.IsNullOrWhiteSpace(l.PnKey) && string.IsNullOrWhiteSpace(l.BrandKey))
            return "_unset";
        return $"{l.PnKey}|{l.BrandKey}";
    }

    private static string MaterialLabel(LayerRow l)
    {
        var pn = string.IsNullOrWhiteSpace(l.PurchasePn) ? "" : l.PurchasePn.Trim();
        var brand = string.IsNullOrWhiteSpace(l.PurchaseBrand) ? "" : l.PurchaseBrand.Trim();
        if (pn.Length == 0 && brand.Length == 0) return UnsetMaterial;
        if (brand.Length == 0) return pn;
        if (pn.Length == 0) return brand;
        return $"{pn} / {brand}";
    }

    private static string FormatStockType(LayerRow l) => l.StockType switch
    {
        1 => "客单",
        2 => "备货",
        3 => "样品",
        _ => l.StockType.ToString()
    };

    private static (string Key, string Label) FormatCurrency(short currency)
    {
        var normalized = InventoryOnHandCurrency.Normalize(currency);
        return (normalized.ToString(), ((CurrencyCode)normalized).ToIsoText());
    }

    private static string ClassifyAgeBucket(int ageDays)
    {
        if (ageDays <= 30) return "0_30";
        if (ageDays <= 90) return "31_90";
        if (ageDays <= 180) return "91_180";
        if (ageDays <= 365) return "181_365";
        return "365_plus";
    }

    private static void ApplyRatios(List<SalesAnalyticsBreakdownItemDto> items)
    {
        var total = items.Sum(x => x.Value);
        if (total <= 0)
        {
            foreach (var it in items) it.Ratio = 0;
            return;
        }

        foreach (var it in items)
            it.Ratio = Math.Round(it.Value / total * 100m, 2);
    }

    private static string NormalizeGroupBy(string? groupBy) =>
        (groupBy ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "day" => "day",
            "week" => "week",
            _ => "month"
        };

    private static List<string> BuildPeriodKeys(DateTime from, DateTime to, string groupBy)
    {
        var keys = new List<string>();
        var cursor = from.Date;
        var end = to.Date;
        while (cursor <= end)
        {
            keys.Add(FormatPeriodKey(cursor, groupBy));
            cursor = groupBy switch
            {
                "day" => cursor.AddDays(1),
                "week" => cursor.AddDays(7),
                _ => cursor.AddMonths(1)
            };
        }

        return keys.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string FormatPeriodKey(DateTime date, string groupBy) => groupBy switch
    {
        "day" => date.ToString("yyyy-MM-dd"),
        "week" => $"{date:yyyy}-W{ISOWeek.GetWeekOfYear(date):D2}",
        _ => date.ToString("yyyy-MM")
    };

    private static (DateTime Start, DateTime End) ParsePeriodRange(string period, string groupBy)
    {
        if (groupBy == "day" && DateTime.TryParse(period, out var day))
            return (day.Date, day.Date.AddDays(1));

        if (groupBy == "month" && DateTime.TryParse(period + "-01", out var month))
            return (month.Date, month.AddMonths(1));

        if (groupBy == "week" && period.Contains("-W", StringComparison.Ordinal))
        {
            var parts = period.Split("-W", StringSplitOptions.None);
            if (parts.Length == 2
                && int.TryParse(parts[0], out var year)
                && int.TryParse(parts[1], out var week))
            {
                var start = ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
                return (start, start.AddDays(7));
            }
        }

        return (DateTime.MinValue, DateTime.MinValue);
    }

    private static IEnumerable<List<string>> Chunk(IReadOnlyList<string> ids, int size)
    {
        for (var i = 0; i < ids.Count; i += size)
            yield return ids.Skip(i).Take(Math.Min(size, ids.Count - i)).ToList();
    }

    private sealed class AnalyticsBundle
    {
        public AnalyticsBundle(IReadOnlyList<LayerRow> layers, IReadOnlyList<OutEventRow> outEvents)
        {
            Layers = layers;
            OutEvents = outEvents;
        }

        public IReadOnlyList<LayerRow> Layers { get; }
        public IReadOnlyList<OutEventRow> OutEvents { get; }
    }

    private sealed class LayerRow
    {
        public string Id { get; set; } = string.Empty;
        public string? PurchasePn { get; set; }
        public string? PurchaseBrand { get; set; }
        public string PnKey { get; set; } = "";
        public string BrandKey { get; set; } = "";
        public short StockType { get; set; }
        public string WarehouseId { get; set; } = "";
        public string WarehouseLabel { get; set; } = "";
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? SalespersonId { get; set; }
        public string? SalespersonName { get; set; }
        public int QtyRepertory { get; set; }
        public int QtyInbound { get; set; }
        public decimal PurchasePrice { get; set; }
        public short Currency { get; set; }
        public DateTime? StockInDate { get; set; }
        public int AgeDays { get; set; }
    }

    private sealed class OutEventRow
    {
        public string StockItemId { get; set; } = string.Empty;
        public int Qty { get; set; }
        public DateTime OutDate { get; set; }
    }
}
