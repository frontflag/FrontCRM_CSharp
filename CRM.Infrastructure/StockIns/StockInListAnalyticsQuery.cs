using System.Globalization;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.StockIns;

/// <summary>入库单列表看板聚合（筛选与 <see cref="StockInListQuery"/> 共用；计数按单头）。</summary>
public sealed class StockInListAnalyticsQuery : IStockInListAnalyticsQuery
{
    private const string ExchangeRateHint =
        "折算美金优先入库过账快照；无快照则按入库数量×采购订单行折算单价；再无则查询日财务参数汇率";

    private const int TopN = 10;
    private const int IdChunkSize = 800;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;
    private readonly IFinanceExchangeRateService _exchangeRateService;

    public StockInListAnalyticsQuery(
        ApplicationDbContext db,
        IDataPermissionService dataPermission,
        IFinanceExchangeRateService exchangeRateService)
    {
        _db = db;
        _dataPermission = dataPermission;
        _exchangeRateService = exchangeRateService;
    }

    /// <inheritdoc />
    public async Task<StockInListAnalyticsDashboardDto> GetDashboardAsync(
        StockInQueryRequest query,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var (headers, lines) = await LoadAnalyticsAsync(query, maskAmounts, cancellationToken);
        return new StockInListAnalyticsDashboardDto
        {
            Context = new StockInListAnalyticsContextDto
            {
                MaskAmounts = maskAmounts,
                ExchangeRateHint = maskAmounts ? null : ExchangeRateHint
            },
            Snapshot = new StockInListAnalyticsSnapshotDto
            {
                VendorCount = CountDistinctVendors(headers),
                HeaderCount = headers.Count,
                AmountUsd = maskAmounts
                    ? null
                    : Math.Round(headers.Sum(h => h.UsdAmount), 2, MidpointRounding.AwayFromZero),
                CurrencyLines = maskAmounts
                    ? Array.Empty<StockInListAnalyticsCurrencyLineDto>()
                    : BuildCurrencyLines(lines)
            }
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StockInListAnalyticsTrendPointDto>> GetTrendsAsync(
        StockInQueryRequest query,
        string groupBy,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var (headers, _) = await LoadAnalyticsAsync(query, maskAmounts, cancellationToken);
        var dated = headers.Where(h => h.BucketDate.HasValue).ToList();
        if (dated.Count == 0 && !query.StockInDateStart.HasValue && !query.StockInDateEnd.HasValue)
            return Array.Empty<StockInListAnalyticsTrendPointDto>();

        DateTime dateFrom;
        DateTime dateToInclusive;
        if (dated.Count > 0)
        {
            dateFrom = dated.Min(h => h.BucketDate!.Value).Date;
            dateToInclusive = dated.Max(h => h.BucketDate!.Value).Date;
        }
        else
        {
            dateFrom = query.StockInDateStart?.Date ?? DateTime.UtcNow.Date;
            dateToInclusive = query.StockInDateEnd?.Date ?? dateFrom;
        }

        if (query.StockInDateStart.HasValue && query.StockInDateStart.Value.Date < dateFrom)
            dateFrom = query.StockInDateStart.Value.Date;
        if (query.StockInDateEnd.HasValue && query.StockInDateEnd.Value.Date > dateToInclusive)
            dateToInclusive = query.StockInDateEnd.Value.Date;

        var normalizedGroupBy = NormalizeGroupBy(groupBy);
        var periods = BuildPeriodKeys(dateFrom, dateToInclusive, normalizedGroupBy);
        var result = new List<StockInListAnalyticsTrendPointDto>(periods.Count);
        foreach (var period in periods)
        {
            var (start, end) = ParsePeriodRange(period, normalizedGroupBy);
            var inBucket = dated.Where(h => h.BucketDate!.Value >= start && h.BucketDate.Value < end).ToList();
            result.Add(new StockInListAnalyticsTrendPointDto
            {
                Period = period,
                HeaderCount = inBucket.Count,
                AmountUsd = maskAmounts
                    ? null
                    : Math.Round(inBucket.Sum(h => h.UsdAmount), 2, MidpointRounding.AwayFromZero)
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        StockInQueryRequest query,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var (headers, _) = await LoadAnalyticsAsync(query, maskAmounts, cancellationToken);

        var typeItems = BuildBreakdown(
            headers,
            h => NormalizeTypeKey(h.StockInType),
            h => FormatStockInType(h.StockInType),
            h => maskAmounts ? 1m : h.UsdAmount);

        var purchaseUserItems = BuildBreakdown(
            headers,
            h => string.IsNullOrWhiteSpace(h.PurchaseUserId) ? "_unset" : h.PurchaseUserId!,
            h => string.IsNullOrWhiteSpace(h.PurchaseUserName) ? "未分配采购员" : h.PurchaseUserName!,
            h => maskAmounts ? 1m : h.UsdAmount);

        return new List<SalesAnalyticsBreakdownGroupDto>
        {
            new() { GroupKey = "stockInType", GroupLabel = "入库类型", Items = typeItems },
            new() { GroupKey = "purchaseUser", GroupLabel = "采购员", Items = purchaseUserItems }
        };
    }

    /// <inheritdoc />
    public async Task<StockInListAnalyticsRankingsDto> GetRankingsAsync(
        StockInQueryRequest query,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var (headers, _) = await LoadAnalyticsAsync(query, maskAmounts, cancellationToken);

        var vendorByAmount = headers
            .GroupBy(h => string.IsNullOrWhiteSpace(h.VendorId) ? "_unset" : h.VendorId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.Key == "_unset"
                    ? "未关联供应商"
                    : (g.Select(x => x.VendorName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? g.Key),
                Amount = maskAmounts ? null : Math.Round(g.Sum(x => x.UsdAmount), 2, MidpointRounding.AwayFromZero),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.Amount ?? x.OrderCount)
            .Take(TopN)
            .ToList();

        var purchaseUserByAmount = headers
            .GroupBy(h => string.IsNullOrWhiteSpace(h.PurchaseUserId) ? "_unset" : h.PurchaseUserId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.Key == "_unset"
                    ? "未分配采购员"
                    : (g.Select(x => x.PurchaseUserName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? g.Key),
                Amount = maskAmounts ? null : Math.Round(g.Sum(x => x.UsdAmount), 2, MidpointRounding.AwayFromZero),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.Amount ?? x.OrderCount)
            .Take(TopN)
            .ToList();

        return new StockInListAnalyticsRankingsDto
        {
            VendorByAmount = vendorByAmount,
            PurchaseUserByAmount = purchaseUserByAmount
        };
    }

    private async Task<(List<HeaderRow> Headers, List<LineMoney> Lines)> LoadAnalyticsAsync(
        StockInQueryRequest query,
        bool maskAmounts,
        CancellationToken cancellationToken)
    {
        var filtered = await StockInListFilter.BuildFilteredQueryAsync(
            _db, _dataPermission, query, cancellationToken);

        var headersRaw = await filtered
            .Select(s => new HeaderSnap
            {
                Id = s.Id,
                StockInDate = s.StockInDate,
                StockInType = s.StockInType,
                VendorId = s.VendorId
            })
            .ToListAsync(cancellationToken);

        if (headersRaw.Count == 0)
            return (new List<HeaderRow>(), new List<LineMoney>());

        var headerIds = headersRaw.Select(h => h.Id).ToList();
        var items = await LoadItemsAsync(headerIds, cancellationToken);
        var itemIds = items.Select(i => i.Id).ToList();
        var stockItems = await LoadStockItemsAsync(itemIds, cancellationToken);
        var extends = await LoadExtendsAsync(itemIds, cancellationToken);

        var poItemIds = extends.Values
            .Select(e => e.PurchaseOrderItemId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var poItems = await LoadPoItemsAsync(poItemIds, cancellationToken);
        var poIds = poItems.Values
            .Select(p => p.PurchaseOrderId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var purchaseOrders = await LoadPurchaseOrdersAsync(poIds, cancellationToken);

        var vendorIds = headersRaw
            .Select(h => h.VendorId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var vendors = await LoadVendorsAsync(vendorIds, cancellationToken);

        var userIds = purchaseOrders.Values
            .Select(p => p.PurchaseUserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var users = await LoadUsersAsync(userIds, cancellationToken);

        var rates = maskAmounts
            ? new FinanceExchangeRateDto()
            : await _exchangeRateService.GetCurrentAsync(cancellationToken);

        var linesByHeader = new Dictionary<string, List<LineMoney>>(StringComparer.OrdinalIgnoreCase);
        var purchaserByHeader = new Dictionary<string, (string? Id, string? Name)>(StringComparer.OrdinalIgnoreCase);
        var allLines = new List<LineMoney>(items.Count);

        foreach (var item in items.OrderBy(i => i.Id, StringComparer.OrdinalIgnoreCase))
        {
            stockItems.TryGetValue(item.Id, out var stock);
            extends.TryGetValue(item.Id, out var ext);
            PoItemSnap? poItem = null;
            PoSnap? po = null;
            if (!string.IsNullOrWhiteSpace(ext?.PurchaseOrderItemId)
                && poItems.TryGetValue(ext.PurchaseOrderItemId, out poItem)
                && !string.IsNullOrWhiteSpace(poItem.PurchaseOrderId))
            {
                purchaseOrders.TryGetValue(poItem.PurchaseOrderId, out po);
            }

            var qty = item.Quantity;
            var unitPrice = item.Price;
            var localAmount = qty * unitPrice;
            var currency = ResolveCurrency(item.Currency, stock?.PurchaseCurrency, poItem?.Currency);

            decimal usdAmount;
            if (stock != null)
            {
                usdAmount = qty * stock.PurchasePriceUsd;
            }
            else if (poItem is { ConvertPrice: > 0 })
            {
                usdAmount = qty * poItem.ConvertPrice;
            }
            else
            {
                usdAmount = FinanceAnalyticsMoneyBuilder.ExtendAmountToUsd(
                    localAmount,
                    poItem?.Cost ?? unitPrice,
                    poItem?.ConvertPrice ?? 0m,
                    currency,
                    rates.UsdToCny,
                    rates.UsdToHkd,
                    rates.UsdToEur);
            }

            var line = new LineMoney
            {
                Currency = currency,
                LocalAmount = localAmount,
                UsdAmount = usdAmount
            };
            allLines.Add(line);
            if (!linesByHeader.TryGetValue(item.StockInId, out var list))
            {
                list = new List<LineMoney>();
                linesByHeader[item.StockInId] = list;
            }
            list.Add(line);

            if (!purchaserByHeader.ContainsKey(item.StockInId)
                && !string.IsNullOrWhiteSpace(po?.PurchaseUserId))
            {
                var name = !string.IsNullOrWhiteSpace(po.PurchaseUserName)
                    ? po.PurchaseUserName.Trim()
                    : (users.TryGetValue(po.PurchaseUserId, out var login) ? login : null);
                purchaserByHeader[item.StockInId] = (po.PurchaseUserId.Trim(), name);
            }
        }

        var headers = new List<HeaderRow>(headersRaw.Count);
        foreach (var raw in headersRaw)
        {
            linesByHeader.TryGetValue(raw.Id, out var headerLines);
            purchaserByHeader.TryGetValue(raw.Id, out var purchaser);
            string? vendorName = null;
            if (!string.IsNullOrWhiteSpace(raw.VendorId) && vendors.TryGetValue(raw.VendorId, out var v))
            {
                vendorName = !string.IsNullOrWhiteSpace(v.OfficialName)
                    ? v.OfficialName.Trim()
                    : (string.IsNullOrWhiteSpace(v.NickName) ? null : v.NickName.Trim());
            }

            headers.Add(new HeaderRow
            {
                VendorId = string.IsNullOrWhiteSpace(raw.VendorId) ? null : raw.VendorId.Trim(),
                VendorName = vendorName,
                PurchaseUserId = purchaser.Id,
                PurchaseUserName = purchaser.Name,
                StockInType = raw.StockInType,
                UsdAmount = headerLines == null
                    ? 0m
                    : headerLines.Sum(l => l.UsdAmount),
                BucketDate = ToBucketDate(raw.StockInDate)
            });
        }

        return (headers, allLines);
    }

    private async Task<List<ItemSnap>> LoadItemsAsync(List<string> headerIds, CancellationToken cancellationToken)
    {
        var result = new List<ItemSnap>();
        foreach (var chunk in Chunk(headerIds, IdChunkSize))
        {
            var rows = await _db.StockInItems.AsNoTracking()
                .Where(i => chunk.Contains(i.StockInId))
                .Select(i => new ItemSnap
                {
                    Id = i.Id,
                    StockInId = i.StockInId,
                    Quantity = i.Quantity,
                    Price = i.Price,
                    Currency = i.Currency
                })
                .ToListAsync(cancellationToken);
            result.AddRange(rows);
        }

        return result;
    }

    private async Task<Dictionary<string, StockSnap>> LoadStockItemsAsync(
        List<string> itemIds,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, StockSnap>(StringComparer.OrdinalIgnoreCase);
        if (itemIds.Count == 0) return map;
        foreach (var chunk in Chunk(itemIds, IdChunkSize))
        {
            var rows = await _db.StockItems.AsNoTracking()
                .Where(s => chunk.Contains(s.StockInItemId))
                .Select(s => new StockSnap
                {
                    StockInItemId = s.StockInItemId,
                    PurchasePriceUsd = s.PurchasePriceUsd,
                    PurchaseCurrency = s.PurchaseCurrency
                })
                .ToListAsync(cancellationToken);
            foreach (var row in rows)
            {
                if (!string.IsNullOrWhiteSpace(row.StockInItemId) && !map.ContainsKey(row.StockInItemId))
                    map[row.StockInItemId] = row;
            }
        }

        return map;
    }

    private async Task<Dictionary<string, ExtendSnap>> LoadExtendsAsync(
        List<string> itemIds,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, ExtendSnap>(StringComparer.OrdinalIgnoreCase);
        if (itemIds.Count == 0) return map;
        foreach (var chunk in Chunk(itemIds, IdChunkSize))
        {
            var rows = await _db.StockInItemExtends.AsNoTracking()
                .Where(e => chunk.Contains(e.Id))
                .Select(e => new ExtendSnap
                {
                    Id = e.Id,
                    PurchaseOrderItemId = e.PurchaseOrderItemId
                })
                .ToListAsync(cancellationToken);
            foreach (var row in rows)
            {
                if (!string.IsNullOrWhiteSpace(row.Id))
                    map[row.Id] = row;
            }
        }

        return map;
    }

    private async Task<Dictionary<string, PoItemSnap>> LoadPoItemsAsync(
        List<string> ids,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, PoItemSnap>(StringComparer.OrdinalIgnoreCase);
        if (ids.Count == 0) return map;
        foreach (var chunk in Chunk(ids, IdChunkSize))
        {
            var rows = await _db.PurchaseOrderItems.AsNoTracking()
                .Where(i => chunk.Contains(i.Id))
                .Select(i => new PoItemSnap
                {
                    Id = i.Id,
                    PurchaseOrderId = i.PurchaseOrderId,
                    Cost = i.Cost,
                    ConvertPrice = i.ConvertPrice,
                    Currency = i.Currency
                })
                .ToListAsync(cancellationToken);
            foreach (var row in rows)
            {
                if (!string.IsNullOrWhiteSpace(row.Id))
                    map[row.Id] = row;
            }
        }

        return map;
    }

    private async Task<Dictionary<string, PoSnap>> LoadPurchaseOrdersAsync(
        List<string> ids,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, PoSnap>(StringComparer.OrdinalIgnoreCase);
        if (ids.Count == 0) return map;
        foreach (var chunk in Chunk(ids, IdChunkSize))
        {
            var rows = await _db.PurchaseOrders.AsNoTracking()
                .Where(o => chunk.Contains(o.Id))
                .Select(o => new PoSnap
                {
                    Id = o.Id,
                    PurchaseUserId = o.PurchaseUserId,
                    PurchaseUserName = o.PurchaseUserName
                })
                .ToListAsync(cancellationToken);
            foreach (var row in rows)
            {
                if (!string.IsNullOrWhiteSpace(row.Id))
                    map[row.Id] = row;
            }
        }

        return map;
    }

    private async Task<Dictionary<string, VendorSnap>> LoadVendorsAsync(
        List<string> ids,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, VendorSnap>(StringComparer.OrdinalIgnoreCase);
        if (ids.Count == 0) return map;
        foreach (var chunk in Chunk(ids, IdChunkSize))
        {
            var rows = await _db.Vendors.AsNoTracking()
                .Where(v => chunk.Contains(v.Id))
                .Select(v => new VendorSnap
                {
                    Id = v.Id,
                    OfficialName = v.OfficialName,
                    NickName = v.NickName
                })
                .ToListAsync(cancellationToken);
            foreach (var row in rows)
            {
                if (!string.IsNullOrWhiteSpace(row.Id))
                    map[row.Id] = row;
            }
        }

        return map;
    }

    private async Task<Dictionary<string, string>> LoadUsersAsync(
        List<string> ids,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (ids.Count == 0) return map;
        foreach (var chunk in Chunk(ids, IdChunkSize))
        {
            var rows = await _db.Users.AsNoTracking()
                .Where(u => chunk.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync(cancellationToken);
            foreach (var row in rows)
            {
                if (!string.IsNullOrWhiteSpace(row.Id) && !string.IsNullOrWhiteSpace(row.UserName))
                    map[row.Id] = row.UserName.Trim();
            }
        }

        return map;
    }

    private static IReadOnlyList<StockInListAnalyticsCurrencyLineDto> BuildCurrencyLines(List<LineMoney> lines)
    {
        return lines
            .GroupBy(r => r.Currency)
            .Select(g => new StockInListAnalyticsCurrencyLineDto
            {
                CurrencyKey = g.Key.ToString(),
                CurrencyLabel = ((CurrencyCode)g.Key).ToIsoText(),
                OriginalAmount = Math.Round(g.Sum(x => x.LocalAmount), 2, MidpointRounding.AwayFromZero),
                UsdAmount = Math.Round(g.Sum(x => x.UsdAmount), 2, MidpointRounding.AwayFromZero)
            })
            .Where(x => (x.OriginalAmount ?? 0m) != 0m || (x.UsdAmount ?? 0m) != 0m)
            .OrderBy(x => x.CurrencyKey)
            .ToList();
    }

    private static List<SalesAnalyticsBreakdownItemDto> BuildBreakdown(
        List<HeaderRow> headers,
        Func<HeaderRow, string> keySelector,
        Func<HeaderRow, string> labelSelector,
        Func<HeaderRow, decimal> valueSelector)
    {
        var items = headers
            .GroupBy(keySelector)
            .Select(g => new SalesAnalyticsBreakdownItemDto
            {
                Key = g.Key,
                Label = labelSelector(g.First()),
                Value = Math.Round(g.Sum(valueSelector), 2, MidpointRounding.AwayFromZero),
                Ratio = 0
            })
            .OrderByDescending(x => x.Value)
            .ToList();
        ApplyRatios(items);
        return items;
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
            it.Ratio = Math.Round(it.Value / total * 100m, 2, MidpointRounding.AwayFromZero);
    }

    private static int CountDistinctVendors(List<HeaderRow> headers) =>
        headers
            .Where(h => !string.IsNullOrWhiteSpace(h.VendorId))
            .Select(h => h.VendorId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

    private static short ResolveCurrency(short? itemCurrency, short? stockCurrency, short? poCurrency)
    {
        if (itemCurrency is > 0) return itemCurrency.Value;
        if (stockCurrency is > 0) return stockCurrency.Value;
        if (poCurrency is > 0) return poCurrency.Value;
        return (short)CurrencyCode.RMB;
    }

    private static string NormalizeTypeKey(short type) =>
        StockInTypeCode.IsPurchaseReceipt(type) ? StockInTypeCode.Purchase.ToString() : type.ToString();

    private static string FormatStockInType(short type) =>
        StockInTypeCode.IsPurchaseReceipt(type)
            ? "采购入库"
            : type switch
            {
                StockInTypeCode.Customs => "报关入库",
                StockInTypeCode.Return => "退货入库",
                StockInTypeCode.Scrap => "报废入库",
                StockInTypeCode.Transfer => "移库",
                _ => $"类型{type}"
            };

    private static DateTime? ToBucketDate(DateTime stockInDate) =>
        stockInDate.Year < 2000 ? null : stockInDate.Date;

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

        return keys.Distinct().ToList();
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

    private static IEnumerable<List<string>> Chunk(List<string> ids, int size)
    {
        for (var i = 0; i < ids.Count; i += size)
            yield return ids.GetRange(i, Math.Min(size, ids.Count - i));
    }

    private sealed class HeaderSnap
    {
        public string Id { get; set; } = string.Empty;
        public DateTime StockInDate { get; set; }
        public short StockInType { get; set; }
        public string? VendorId { get; set; }
    }

    private sealed class ItemSnap
    {
        public string Id { get; set; } = string.Empty;
        public string StockInId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public short? Currency { get; set; }
    }

    private sealed class StockSnap
    {
        public string StockInItemId { get; set; } = string.Empty;
        public decimal PurchasePriceUsd { get; set; }
        public short PurchaseCurrency { get; set; }
    }

    private sealed class ExtendSnap
    {
        public string Id { get; set; } = string.Empty;
        public string? PurchaseOrderItemId { get; set; }
    }

    private sealed class PoItemSnap
    {
        public string Id { get; set; } = string.Empty;
        public string PurchaseOrderId { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public decimal ConvertPrice { get; set; }
        public short Currency { get; set; }
    }

    private sealed class PoSnap
    {
        public string Id { get; set; } = string.Empty;
        public string? PurchaseUserId { get; set; }
        public string? PurchaseUserName { get; set; }
    }

    private sealed class VendorSnap
    {
        public string Id { get; set; } = string.Empty;
        public string? OfficialName { get; set; }
        public string? NickName { get; set; }
    }

    private sealed class LineMoney
    {
        public short Currency { get; set; }
        public decimal LocalAmount { get; set; }
        public decimal UsdAmount { get; set; }
    }

    private sealed class HeaderRow
    {
        public string? VendorId { get; set; }
        public string? VendorName { get; set; }
        public string? PurchaseUserId { get; set; }
        public string? PurchaseUserName { get; set; }
        public short StockInType { get; set; }
        public decimal UsdAmount { get; set; }
        public DateTime? BucketDate { get; set; }
    }
}
