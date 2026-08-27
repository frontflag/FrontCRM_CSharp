using System.Globalization;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.StockOuts;

/// <summary>出库明细列表看板聚合（筛选与 <see cref="StockOutItemEfListQuery"/> 共用）。</summary>
public sealed class StockOutItemListAnalyticsQuery : IStockOutItemListAnalyticsQuery
{
    private const string ExchangeRateHint =
        "折算美金优先出库过账快照；无快照则按出库数量×订单行折算单价；再无则查询日财务参数汇率";

    private const int TopN = 10;
    private const int IdChunkSize = 800;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;
    private readonly IFinanceExchangeRateService _exchangeRateService;

    public StockOutItemListAnalyticsQuery(
        ApplicationDbContext db,
        IDataPermissionService dataPermission,
        IFinanceExchangeRateService exchangeRateService)
    {
        _db = db;
        _dataPermission = dataPermission;
        _exchangeRateService = exchangeRateService;
    }

    /// <inheritdoc />
    public async Task<StockOutItemListAnalyticsDashboardDto> GetDashboardAsync(
        StockOutItemListQuery query,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var rows = await LoadAnalyticsRowsAsync(query, maskAmounts, cancellationToken);
        return new StockOutItemListAnalyticsDashboardDto
        {
            Context = new StockOutItemListAnalyticsContextDto
            {
                MaskAmounts = maskAmounts,
                ExchangeRateHint = maskAmounts ? null : ExchangeRateHint
            },
            Snapshot = new StockOutItemListAnalyticsSnapshotDto
            {
                CustomerCount = CountDistinctCustomers(rows),
                LineCount = rows.Count,
                AmountUsd = maskAmounts
                    ? null
                    : Math.Round(rows.Sum(r => r.UsdAmount), 2, MidpointRounding.AwayFromZero),
                CurrencyLines = maskAmounts ? Array.Empty<StockOutItemListAnalyticsCurrencyLineDto>() : BuildCurrencyLines(rows)
            }
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StockOutItemListAnalyticsTrendPointDto>> GetTrendsAsync(
        StockOutItemListQuery query,
        string groupBy,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var rows = await LoadAnalyticsRowsAsync(query, maskAmounts, cancellationToken);
        var dated = rows.Where(r => r.BucketDate.HasValue).ToList();
        if (dated.Count == 0 && !query.StockOutDateFrom.HasValue && !query.StockOutDateTo.HasValue)
            return Array.Empty<StockOutItemListAnalyticsTrendPointDto>();

        DateTime dateFrom;
        DateTime dateToInclusive;
        if (dated.Count > 0)
        {
            dateFrom = dated.Min(r => r.BucketDate!.Value).Date;
            dateToInclusive = dated.Max(r => r.BucketDate!.Value).Date;
        }
        else
        {
            dateFrom = query.StockOutDateFrom?.Date ?? DateTime.UtcNow.Date;
            dateToInclusive = query.StockOutDateTo?.Date ?? dateFrom;
        }

        if (query.StockOutDateFrom.HasValue && query.StockOutDateFrom.Value.Date < dateFrom)
            dateFrom = query.StockOutDateFrom.Value.Date;
        if (query.StockOutDateTo.HasValue && query.StockOutDateTo.Value.Date > dateToInclusive)
            dateToInclusive = query.StockOutDateTo.Value.Date;

        var normalizedGroupBy = NormalizeGroupBy(groupBy);
        var periods = BuildPeriodKeys(dateFrom, dateToInclusive, normalizedGroupBy);
        var result = new List<StockOutItemListAnalyticsTrendPointDto>(periods.Count);
        foreach (var period in periods)
        {
            var (start, end) = ParsePeriodRange(period, normalizedGroupBy);
            var inBucket = dated.Where(r => r.BucketDate!.Value >= start && r.BucketDate.Value < end).ToList();
            result.Add(new StockOutItemListAnalyticsTrendPointDto
            {
                Period = period,
                LineCount = inBucket.Count,
                AmountUsd = maskAmounts
                    ? null
                    : Math.Round(inBucket.Sum(r => r.UsdAmount), 2, MidpointRounding.AwayFromZero)
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        StockOutItemListQuery query,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var rows = await LoadAnalyticsRowsAsync(query, maskAmounts, cancellationToken);

        var typeItems = BuildBreakdown(
            rows,
            r => NormalizeTypeKey(r.StockOutType),
            r => FormatStockOutType(r.StockOutType),
            r => maskAmounts ? 1m : r.UsdAmount);

        var salesUserItems = BuildBreakdown(
            rows,
            r => string.IsNullOrWhiteSpace(r.SalesUserId) ? "_unset" : r.SalesUserId!,
            r => string.IsNullOrWhiteSpace(r.SalesUserName) ? "未分配业务员" : r.SalesUserName!,
            r => maskAmounts ? 1m : r.UsdAmount);

        return new List<SalesAnalyticsBreakdownGroupDto>
        {
            new() { GroupKey = "stockOutType", GroupLabel = "出库类型", Items = typeItems },
            new() { GroupKey = "salesUser", GroupLabel = "业务员", Items = salesUserItems }
        };
    }

    /// <inheritdoc />
    public async Task<StockOutItemListAnalyticsRankingsDto> GetRankingsAsync(
        StockOutItemListQuery query,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var rows = await LoadAnalyticsRowsAsync(query, maskAmounts, cancellationToken);

        var customerByAmount = rows
            .GroupBy(r => string.IsNullOrWhiteSpace(r.CustomerId) ? "_unset" : r.CustomerId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.Key == "_unset"
                    ? "未关联客户"
                    : (g.Select(x => x.CustomerName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? g.Key),
                Amount = maskAmounts ? null : Math.Round(g.Sum(x => x.UsdAmount), 2, MidpointRounding.AwayFromZero),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.Amount ?? x.OrderCount)
            .Take(TopN)
            .ToList();

        var salesUserByAmount = rows
            .GroupBy(r => string.IsNullOrWhiteSpace(r.SalesUserId) ? "_unset" : r.SalesUserId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.Key == "_unset"
                    ? "未分配业务员"
                    : (g.Select(x => x.SalesUserName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? g.Key),
                Amount = maskAmounts ? null : Math.Round(g.Sum(x => x.UsdAmount), 2, MidpointRounding.AwayFromZero),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.Amount ?? x.OrderCount)
            .Take(TopN)
            .ToList();

        return new StockOutItemListAnalyticsRankingsDto
        {
            CustomerByAmount = customerByAmount,
            SalesUserByAmount = salesUserByAmount
        };
    }

    private async Task<List<AnalyticsRow>> LoadAnalyticsRowsAsync(
        StockOutItemListQuery query,
        bool maskAmounts,
        CancellationToken cancellationToken)
    {
        var filtered = await StockOutItemListFilter.BuildFilteredJoinQueryAsync(
            _db, _dataPermission, query, cancellationToken);

        var raw = await filtered
            .Select(x => new RawJoinRow
            {
                ItemId = x.Item.Id,
                ActualQty = x.Item.ActualQty,
                Quantity = x.Item.Quantity,
                StockOutDate = x.Header.StockOutDate,
                StockOutType = x.Header.StockOutType,
                CustomerId = x.Header.CustomerId,
                CustomerOfficial = x.HeaderCustomer != null ? x.HeaderCustomer.OfficialName : null,
                CustomerNick = x.HeaderCustomer != null ? x.HeaderCustomer.NickName : null,
                SoCustomerName = x.Order != null ? x.Order.CustomerName : null,
                HeaderSellOrderItemId = x.Header.SellOrderItemId,
                HeaderLineId = x.SoLine != null ? x.SoLine.Id : null,
                HeaderLinePrice = x.SoLine != null ? x.SoLine.Price : 0m,
                HeaderLineCurrency = x.SoLine != null ? x.SoLine.Currency : (short)0,
                HeaderLineConvertPrice = x.SoLine != null ? x.SoLine.ConvertPrice : 0m,
                SoSalesUserId = x.Order != null ? x.Order.SalesUserId : null,
                SoSalesUserName = x.Order != null ? x.Order.SalesUserName : null,
                UserLogin = x.SalesUser != null ? x.SalesUser.UserName : null
            })
            .ToListAsync(cancellationToken);

        if (raw.Count == 0)
            return new List<AnalyticsRow>();

        var itemIds = raw.Select(x => x.ItemId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var extends = await LoadExtendsAsync(itemIds, cancellationToken);

        var extraLineIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in raw)
        {
            if (!extends.TryGetValue(row.ItemId, out var ext))
                continue;
            var sellId = FirstNonEmpty(ext.SellOrderItemId, row.HeaderSellOrderItemId);
            if (string.IsNullOrWhiteSpace(sellId))
                continue;
            if (!string.Equals(sellId, row.HeaderLineId, StringComparison.OrdinalIgnoreCase))
                extraLineIds.Add(sellId);
        }

        var extraLines = await LoadSellOrderLinesAsync(extraLineIds, cancellationToken);
        var rates = maskAmounts
            ? new FinanceExchangeRateDto()
            : await _exchangeRateService.GetCurrentAsync(cancellationToken);

        var result = new List<AnalyticsRow>(raw.Count);
        foreach (var x in raw)
        {
            extends.TryGetValue(x.ItemId, out var ext);
            var sellLineId = FirstNonEmpty(ext?.SellOrderItemId, x.HeaderSellOrderItemId);
            decimal soPrice = x.HeaderLinePrice;
            short soCurrency = x.HeaderLineCurrency;
            decimal soConvert = x.HeaderLineConvertPrice;
            if (!string.IsNullOrWhiteSpace(sellLineId)
                && extraLines.TryGetValue(sellLineId, out var extra))
            {
                soPrice = extra.Price;
                soCurrency = extra.Currency;
                soConvert = extra.ConvertPrice;
            }

            var outQty = x.ActualQty > 0 ? x.ActualQty : x.Quantity;
            decimal? unitPrice = ext?.SalesPrice is > 0 ? ext.SalesPrice : (soPrice > 0 ? soPrice : null);
            var currency = ext?.SalesCurrency is > 0
                ? ext.SalesCurrency.Value
                : (soCurrency > 0 ? soCurrency : (short)CurrencyCode.RMB);
            var localAmount = outQty * (unitPrice ?? 0m);

            decimal usdAmount;
            if (ext?.SalesPriceUsd != null)
            {
                usdAmount = outQty * ext.SalesPriceUsd.Value;
            }
            else
            {
                usdAmount = FinanceAnalyticsMoneyBuilder.ExtendAmountToUsd(
                    localAmount,
                    soPrice,
                    soConvert,
                    currency,
                    rates.UsdToCny,
                    rates.UsdToHkd,
                    rates.UsdToEur);
            }

            string? customerName = null;
            if (!string.IsNullOrWhiteSpace(x.CustomerOfficial))
                customerName = x.CustomerOfficial.Trim();
            else if (!string.IsNullOrWhiteSpace(x.CustomerNick))
                customerName = x.CustomerNick.Trim();
            else if (!string.IsNullOrWhiteSpace(x.SoCustomerName))
                customerName = x.SoCustomerName.Trim();

            var salesUserId = string.IsNullOrWhiteSpace(x.SoSalesUserId) ? null : x.SoSalesUserId.Trim();
            var salesUserName = !string.IsNullOrWhiteSpace(x.UserLogin)
                ? x.UserLogin.Trim()
                : (string.IsNullOrWhiteSpace(x.SoSalesUserName) ? null : x.SoSalesUserName.Trim());

            result.Add(new AnalyticsRow
            {
                CustomerId = string.IsNullOrWhiteSpace(x.CustomerId) ? null : x.CustomerId.Trim(),
                CustomerName = customerName,
                SalesUserId = salesUserId,
                SalesUserName = salesUserName,
                StockOutType = x.StockOutType,
                Currency = currency,
                LocalAmount = localAmount,
                UsdAmount = usdAmount,
                BucketDate = x.StockOutDate?.Date
            });
        }

        return result;
    }

    private async Task<Dictionary<string, ExtendSnap>> LoadExtendsAsync(
        List<string> itemIds,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, ExtendSnap>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in Chunk(itemIds, IdChunkSize))
        {
            var rows = await _db.StockOutItemExtends.AsNoTracking()
                .Where(e => chunk.Contains(e.Id))
                .Select(e => new ExtendSnap
                {
                    Id = e.Id,
                    SellOrderItemId = e.SellOrderItemId,
                    SalesPrice = e.SalesPrice,
                    SalesCurrency = e.SalesCurrency,
                    SalesPriceUsd = e.SalesPriceUsd
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

    private async Task<Dictionary<string, LineSnap>> LoadSellOrderLinesAsync(
        HashSet<string> ids,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, LineSnap>(StringComparer.OrdinalIgnoreCase);
        if (ids.Count == 0)
            return map;

        foreach (var chunk in Chunk(ids.ToList(), IdChunkSize))
        {
            var rows = await _db.SellOrderItems.AsNoTracking()
                .Where(i => chunk.Contains(i.Id))
                .Select(i => new LineSnap
                {
                    Id = i.Id,
                    Price = i.Price,
                    Currency = i.Currency,
                    ConvertPrice = i.ConvertPrice
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

    private static IReadOnlyList<StockOutItemListAnalyticsCurrencyLineDto> BuildCurrencyLines(List<AnalyticsRow> rows)
    {
        return rows
            .GroupBy(r => r.Currency)
            .Select(g => new StockOutItemListAnalyticsCurrencyLineDto
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
        List<AnalyticsRow> rows,
        Func<AnalyticsRow, string> keySelector,
        Func<AnalyticsRow, string> labelSelector,
        Func<AnalyticsRow, decimal> valueSelector)
    {
        var items = rows
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

    private static int CountDistinctCustomers(List<AnalyticsRow> rows) =>
        rows
            .Where(r => !string.IsNullOrWhiteSpace(r.CustomerId))
            .Select(r => r.CustomerId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

    private static string NormalizeTypeKey(short type) =>
        StockOutTypeCode.IsSalesStockOut(type) ? StockOutTypeCode.Sales.ToString() : type.ToString();

    private static string FormatStockOutType(short type) =>
        StockOutTypeCode.IsSalesStockOut(type)
            ? "销售出库"
            : type switch
            {
                StockOutTypeCode.Customs => "报关出库",
                StockOutTypeCode.Return => "退货出库",
                StockOutTypeCode.Scrap => "报废出库",
                StockOutTypeCode.Transfer => "移库",
                _ => $"类型{type}"
            };

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }

        return null;
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

    private sealed class RawJoinRow
    {
        public string ItemId { get; set; } = string.Empty;
        public int ActualQty { get; set; }
        public int Quantity { get; set; }
        public DateTime? StockOutDate { get; set; }
        public short StockOutType { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerOfficial { get; set; }
        public string? CustomerNick { get; set; }
        public string? SoCustomerName { get; set; }
        public string? HeaderSellOrderItemId { get; set; }
        public string? HeaderLineId { get; set; }
        public decimal HeaderLinePrice { get; set; }
        public short HeaderLineCurrency { get; set; }
        public decimal HeaderLineConvertPrice { get; set; }
        public string? SoSalesUserId { get; set; }
        public string? SoSalesUserName { get; set; }
        public string? UserLogin { get; set; }
    }

    private sealed class ExtendSnap
    {
        public string Id { get; set; } = string.Empty;
        public string? SellOrderItemId { get; set; }
        public decimal? SalesPrice { get; set; }
        public short? SalesCurrency { get; set; }
        public decimal? SalesPriceUsd { get; set; }
    }

    private sealed class LineSnap
    {
        public string Id { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public short Currency { get; set; }
        public decimal ConvertPrice { get; set; }
    }

    private sealed class AnalyticsRow
    {
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? SalesUserId { get; set; }
        public string? SalesUserName { get; set; }
        public short StockOutType { get; set; }
        public short Currency { get; set; }
        public decimal LocalAmount { get; set; }
        public decimal UsdAmount { get; set; }
        public DateTime? BucketDate { get; set; }
    }
}
