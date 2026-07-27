using System.Globalization;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Finance;
using CRM.Core.Utilities;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Finance;

public sealed partial class FinanceReceivableListQuery
{
    private const string ExchangeRateHint = "美元折算按查询日财务参数汇率";

    /// <inheritdoc />
    public async Task<FinanceReceivableListAnalyticsDashboardDto> GetListAnalyticsDashboardAsync(
        FinanceReceivableQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var (rows, _) = await LoadAnalyticsRowsAsync(request, cancellationToken);
        var currencyPending = BuildCurrencyLines(rows, r => r.PendingUsd, r => r.VerifiedToBe);
        var currencyTotal = BuildCurrencyLines(rows, r => r.TotalUsd, r => r.Amount);

        int? maxAge = null;
        if (rows.Count > 0)
            maxAge = rows.Max(r => r.AgeDays);

        return new FinanceReceivableListAnalyticsDashboardDto
        {
            Context = new FinanceReceivableListAnalyticsContextDto
            {
                MaskAmounts = false,
                ExchangeRateHint = ExchangeRateHint
            },
            Snapshot = new FinanceReceivableListAnalyticsSnapshotDto
            {
                CustomerCount = rows
                    .Where(r => !string.IsNullOrWhiteSpace(r.CustomerId))
                    .Select(r => r.CustomerId!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                LineCount = rows.Count,
                PendingAmountUsd = Math.Round(rows.Sum(r => r.PendingUsd), 2, MidpointRounding.AwayFromZero),
                PendingCurrencyLines = currencyPending,
                TotalAmountUsd = Math.Round(rows.Sum(r => r.TotalUsd), 2, MidpointRounding.AwayFromZero),
                TotalCurrencyLines = currencyTotal,
                MaxReceivableAgeDays = rows.Count > 0 ? maxAge : null
            }
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FinanceReceivableListAnalyticsTrendPointDto>> GetListAnalyticsTrendsAsync(
        FinanceReceivableQueryRequest request,
        string groupBy,
        CancellationToken cancellationToken = default)
    {
        var (rows, _) = await LoadAnalyticsRowsAsync(request, cancellationToken);
        if (rows.Count == 0)
            return Array.Empty<FinanceReceivableListAnalyticsTrendPointDto>();

        var dateFrom = rows.Min(r => r.BucketDate).Date;
        var dateToInclusive = rows.Max(r => r.BucketDate).Date;
        if (request.StockOutDateFrom.HasValue && request.StockOutDateFrom.Value.Date < dateFrom)
            dateFrom = request.StockOutDateFrom.Value.Date;
        if (request.StockOutDateTo.HasValue && request.StockOutDateTo.Value.Date > dateToInclusive)
            dateToInclusive = request.StockOutDateTo.Value.Date;

        var normalizedGroupBy = NormalizeGroupBy(groupBy);
        var periods = BuildPeriodKeys(dateFrom, dateToInclusive, normalizedGroupBy);
        var result = new List<FinanceReceivableListAnalyticsTrendPointDto>();

        foreach (var period in periods)
        {
            var (start, end) = ParsePeriodRange(period, normalizedGroupBy);
            var inBucket = rows.Where(r => r.BucketDate >= start && r.BucketDate < end).ToList();
            result.Add(new FinanceReceivableListAnalyticsTrendPointDto
            {
                Period = period,
                CustomerCount = inBucket
                    .Where(r => !string.IsNullOrWhiteSpace(r.CustomerId))
                    .Select(r => r.CustomerId!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                LineCount = inBucket.Count,
                PendingAmountUsd = Math.Round(inBucket.Sum(r => r.PendingUsd), 2, MidpointRounding.AwayFromZero),
                TotalAmountUsd = Math.Round(inBucket.Sum(r => r.TotalUsd), 2, MidpointRounding.AwayFromZero)
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FinanceReceivableListAnalyticsBreakdownGroupDto>> GetListAnalyticsBreakdownsAsync(
        FinanceReceivableQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var (rows, _) = await LoadAnalyticsRowsAsync(request, cancellationToken);

        var statusItems = BuildDualBreakdown(
            rows,
            r => r.VerificationStatus.ToString(),
            r => FormatVerificationStatus(r.VerificationStatus),
            r => r.PendingUsd,
            r => r.TotalUsd);

        var currencyItems = BuildDualBreakdown(
            rows,
            r => r.Currency.ToString(),
            r => ((CurrencyCode)r.Currency).ToIsoText(),
            r => r.PendingUsd,
            r => r.TotalUsd);

        var agingItems = BuildDualBreakdown(
            rows,
            r => AgingBucketKey(r.AgeDays),
            r => AgingBucketLabel(r.AgeDays),
            r => r.PendingUsd,
            r => r.TotalUsd);

        var salesUserItems = BuildDualBreakdown(
            rows,
            r => string.IsNullOrWhiteSpace(r.SalesUserId) ? "_unset" : r.SalesUserId!,
            r => string.IsNullOrWhiteSpace(r.SalesUserName) ? "未分配业务员" : r.SalesUserName!,
            r => r.PendingUsd,
            r => r.TotalUsd);

        OrderAgingItems(agingItems);

        return new List<FinanceReceivableListAnalyticsBreakdownGroupDto>
        {
            new() { GroupKey = "verificationStatus", GroupLabel = "核销状态", Items = statusItems },
            new() { GroupKey = "currency", GroupLabel = "币别", Items = currencyItems },
            new() { GroupKey = "aging", GroupLabel = "账期", Items = agingItems },
            new() { GroupKey = "salesUser", GroupLabel = "业务员", Items = salesUserItems }
        };
    }

    /// <inheritdoc />
    public async Task<FinanceReceivableListAnalyticsRankingsDto> GetListAnalyticsRankingsAsync(
        FinanceReceivableQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var (rows, _) = await LoadAnalyticsRowsAsync(request, cancellationToken);

        var receivableByTotal = rows
            .OrderByDescending(r => r.TotalUsd)
            .ThenByDescending(r => r.PendingUsd)
            .Take(10)
            .Select(r => new FinanceReceivableListAnalyticsRankingRowDto
            {
                Id = r.Id,
                Name = !string.IsNullOrWhiteSpace(r.ReceivableCode)
                    ? r.ReceivableCode!
                    : (r.StockOutCode ?? r.Id),
                PendingAmountUsd = Math.Round(r.PendingUsd, 2, MidpointRounding.AwayFromZero),
                TotalAmountUsd = Math.Round(r.TotalUsd, 2, MidpointRounding.AwayFromZero),
                OrderCount = 1,
                VerificationStatus = r.VerificationStatus
            })
            .ToList();

        var customerByAmount = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.CustomerId))
            .GroupBy(r => r.CustomerId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new FinanceReceivableListAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.Select(x => x.CustomerName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? g.Key,
                PendingAmountUsd = Math.Round(g.Sum(x => x.PendingUsd), 2, MidpointRounding.AwayFromZero),
                TotalAmountUsd = Math.Round(g.Sum(x => x.TotalUsd), 2, MidpointRounding.AwayFromZero),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.PendingAmountUsd)
            .ThenByDescending(x => x.TotalAmountUsd)
            .Take(10)
            .ToList();

        var salesUserByAmount = rows
            .GroupBy(r => string.IsNullOrWhiteSpace(r.SalesUserId) ? "_unset" : r.SalesUserId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new FinanceReceivableListAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.Key == "_unset"
                    ? "未分配业务员"
                    : (g.Select(x => x.SalesUserName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? g.Key),
                PendingAmountUsd = Math.Round(g.Sum(x => x.PendingUsd), 2, MidpointRounding.AwayFromZero),
                TotalAmountUsd = Math.Round(g.Sum(x => x.TotalUsd), 2, MidpointRounding.AwayFromZero),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.PendingAmountUsd)
            .ThenByDescending(x => x.TotalAmountUsd)
            .Take(10)
            .ToList();

        return new FinanceReceivableListAnalyticsRankingsDto
        {
            ReceivableByTotalAmount = receivableByTotal,
            CustomerByAmount = customerByAmount,
            SalesUserByAmount = salesUserByAmount
        };
    }

    private async Task<(List<AnalyticsRow> Rows, FinanceExchangeRateDto Rates)> LoadAnalyticsRowsAsync(
        FinanceReceivableQueryRequest request,
        CancellationToken cancellationToken)
    {
        var rates = await _exchangeRateService.GetCurrentAsync(cancellationToken);
        var q = await BuildFilteredQueryAsync(request, cancellationToken);

        var raw = await (
            from r in q
            join so in _db.SellOrders.AsNoTracking() on r.SellOrderId equals so.Id into soJoin
            from so in soJoin.DefaultIfEmpty()
            select new
            {
                r.Id,
                r.ReceivableCode,
                r.StockOutCode,
                r.CustomerId,
                r.CustomerName,
                r.SalesUserId,
                SoSalesUserId = so != null ? so.SalesUserId : null,
                r.Currency,
                r.Amount,
                r.VerifiedToBe,
                r.VerificationStatus,
                r.StockOutDate,
                r.CreateTime
            }).ToListAsync(cancellationToken);

        var ids = raw.Select(x => x.Id).ToList();
        var lastWriteOffByReceivable = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        if (ids.Count > 0)
        {
            var writeOffs = await _db.FinanceReceivableWriteOffs.AsNoTracking()
                .Where(w => ids.Contains(w.FinanceReceivableId))
                .GroupBy(w => w.FinanceReceivableId)
                .Select(g => new { ReceivableId = g.Key, LastTime = g.Max(x => x.CreateTime) })
                .ToListAsync(cancellationToken);
            foreach (var w in writeOffs)
                lastWriteOffByReceivable[w.ReceivableId] = w.LastTime;
        }

        // 业务员展示与列表一致：仅登录账号 User.UserName（不用 RealName / 订单上的显示名）
        var salesUserIds = raw
            .Select(x => FirstNonEmpty(x.SalesUserId, x.SoSalesUserId))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var loginByUserId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (salesUserIds.Count > 0)
        {
            var users = await _db.Users.AsNoTracking()
                .Where(u => salesUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync(cancellationToken);
            foreach (var u in users)
            {
                if (string.IsNullOrWhiteSpace(u.Id)) continue;
                var login = string.IsNullOrWhiteSpace(u.UserName) ? null : u.UserName.Trim();
                if (!string.IsNullOrWhiteSpace(login))
                    loginByUserId[u.Id.Trim()] = login;
            }
        }

        var today = DateTime.UtcNow.Date;
        var rows = new List<AnalyticsRow>(raw.Count);
        foreach (var x in raw)
        {
            var stockOut = (x.StockOutDate ?? x.CreateTime).Date;
            var ageEnd = x.VerificationStatus == 2
                && lastWriteOffByReceivable.TryGetValue(x.Id, out var lastWo)
                    ? lastWo.Date
                    : today;
            var ageDays = Math.Max(0, (int)(ageEnd - stockOut).TotalDays);

            var salesUserId = FirstNonEmpty(x.SalesUserId, x.SoSalesUserId);
            string? salesLogin = null;
            if (!string.IsNullOrWhiteSpace(salesUserId)
                && loginByUserId.TryGetValue(salesUserId.Trim(), out var login))
                salesLogin = login;

            rows.Add(new AnalyticsRow
            {
                Id = x.Id,
                ReceivableCode = x.ReceivableCode,
                StockOutCode = x.StockOutCode,
                CustomerId = x.CustomerId,
                CustomerName = x.CustomerName,
                SalesUserId = salesUserId,
                SalesUserName = salesLogin,
                Currency = x.Currency,
                Amount = x.Amount,
                VerifiedToBe = x.VerifiedToBe,
                VerificationStatus = x.VerificationStatus,
                BucketDate = stockOut,
                AgeDays = ageDays,
                PendingUsd = ToUsd(x.VerifiedToBe, x.Currency, rates),
                TotalUsd = ToUsd(x.Amount, x.Currency, rates)
            });
        }

        return (rows, rates);
    }

    private static decimal ToUsd(decimal local, short currency, FinanceExchangeRateDto rates) =>
        Math.Round(
            ExchangeRateToUsdConverter.UnitLocalToUsd(local, currency, rates.UsdToCny, rates.UsdToHkd, rates.UsdToEur),
            2,
            MidpointRounding.AwayFromZero);

    private static IReadOnlyList<FinanceReceivableListAnalyticsCurrencyLineDto> BuildCurrencyLines(
        List<AnalyticsRow> rows,
        Func<AnalyticsRow, decimal> usdSelector,
        Func<AnalyticsRow, decimal> localSelector)
    {
        return rows
            .GroupBy(r => r.Currency)
            .Select(g => new FinanceReceivableListAnalyticsCurrencyLineDto
            {
                CurrencyKey = g.Key.ToString(),
                CurrencyLabel = ((CurrencyCode)g.Key).ToIsoText(),
                OriginalAmount = Math.Round(g.Sum(localSelector), 2, MidpointRounding.AwayFromZero),
                UsdAmount = Math.Round(g.Sum(usdSelector), 2, MidpointRounding.AwayFromZero)
            })
            .Where(x => (x.OriginalAmount ?? 0m) != 0m || (x.UsdAmount ?? 0m) != 0m)
            .OrderBy(x => x.CurrencyKey)
            .ToList();
    }

    private static List<FinanceReceivableListAnalyticsBreakdownItemDto> BuildDualBreakdown(
        List<AnalyticsRow> rows,
        Func<AnalyticsRow, string> keySelector,
        Func<AnalyticsRow, string> labelSelector,
        Func<AnalyticsRow, decimal> pendingSelector,
        Func<AnalyticsRow, decimal> totalSelector)
    {
        var items = rows
            .GroupBy(keySelector)
            .Select(g =>
            {
                var first = g.First();
                return new FinanceReceivableListAnalyticsBreakdownItemDto
                {
                    Key = g.Key,
                    Label = labelSelector(first),
                    PendingValue = Math.Round(g.Sum(pendingSelector), 2, MidpointRounding.AwayFromZero),
                    TotalValue = Math.Round(g.Sum(totalSelector), 2, MidpointRounding.AwayFromZero)
                };
            })
            .OrderByDescending(x => x.PendingValue)
            .ThenByDescending(x => x.TotalValue)
            .ToList();

        ApplyDualRatios(items);
        return items;
    }

    private static void ApplyDualRatios(List<FinanceReceivableListAnalyticsBreakdownItemDto> items)
    {
        var pendingSum = items.Sum(x => x.PendingValue);
        var totalSum = items.Sum(x => x.TotalValue);
        foreach (var item in items)
        {
            item.PendingRatio = pendingSum > 0m
                ? Math.Round(item.PendingValue / pendingSum * 100m, 2, MidpointRounding.AwayFromZero)
                : 0m;
            item.TotalRatio = totalSum > 0m
                ? Math.Round(item.TotalValue / totalSum * 100m, 2, MidpointRounding.AwayFromZero)
                : 0m;
        }
    }

    private static void OrderAgingItems(List<FinanceReceivableListAnalyticsBreakdownItemDto> items)
    {
        var order = new[] { "0-10", "10-30", "30-60", "60-90", "90-180", "180+" };
        items.Sort((a, b) =>
        {
            var ia = Array.IndexOf(order, a.Key);
            var ib = Array.IndexOf(order, b.Key);
            if (ia < 0) ia = 99;
            if (ib < 0) ib = 99;
            return ia.CompareTo(ib);
        });
    }

    private static string AgingBucketKey(int days) => days switch
    {
        < 10 => "0-10",
        < 30 => "10-30",
        < 60 => "30-60",
        < 90 => "60-90",
        < 180 => "90-180",
        _ => "180+"
    };

    private static string AgingBucketLabel(int days) => days switch
    {
        < 10 => "0-10天",
        < 30 => "10-30天",
        < 60 => "30-60天",
        < 90 => "60-90天",
        < 180 => "90天-半年",
        _ => "更长"
    };

    private static string FormatVerificationStatus(short status) => status switch
    {
        2 => "已核销",
        1 => "部分核销",
        _ => "未核销"
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

    private sealed class AnalyticsRow
    {
        public string Id { get; set; } = string.Empty;
        public string? ReceivableCode { get; set; }
        public string? StockOutCode { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? SalesUserId { get; set; }
        public string? SalesUserName { get; set; }
        public short Currency { get; set; }
        public decimal Amount { get; set; }
        public decimal VerifiedToBe { get; set; }
        public short VerificationStatus { get; set; }
        public DateTime BucketDate { get; set; }
        public int AgeDays { get; set; }
        public decimal PendingUsd { get; set; }
        public decimal TotalUsd { get; set; }
    }
}
