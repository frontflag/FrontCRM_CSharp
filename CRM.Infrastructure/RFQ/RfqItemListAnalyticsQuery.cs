using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Quote;
using CRM.Core.Models.Sales;
using CRM.Core.Services;
using CRM.Core.Utilities;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.RfqListQueries;

public sealed partial class RfqItemListQuery
{
    private const short NoQuoteFoundItemStatus = 5;
    private const int BrandBreakdownTopN = 20;

    /// <inheritdoc />
    public async Task<RfqListAnalyticsDashboardDto> GetListAnalyticsDashboardAsync(
        RFQItemQueryRequest request,
        bool maskCustomerNames,
        CancellationToken cancellationToken = default)
    {
        var bundle = await LoadItemAnalyticsBundleAsync(request, cancellationToken);
        return new RfqListAnalyticsDashboardDto
        {
            Context = new RfqListAnalyticsContextDto { MaskCustomerNames = maskCustomerNames },
            Snapshot = BuildItemSnapshot(bundle)
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RfqListAnalyticsTrendPointDto>> GetListAnalyticsTrendsAsync(
        RFQItemQueryRequest request,
        string groupBy,
        CancellationToken cancellationToken = default)
    {
        var bundle = await LoadItemAnalyticsBundleAsync(request, cancellationToken);
        if (bundle.Items.Count == 0)
            return Array.Empty<RfqListAnalyticsTrendPointDto>();

        var minTime = bundle.Items.Min(i => i.RfqCreateTime);
        var maxTime = bundle.Items.Max(i => i.RfqCreateTime);
        var (dateFrom, dateToInclusive) = ResolveItemTrendDateBounds(request, minTime, maxTime);
        var normalizedGroupBy = NormalizeGroupBy(groupBy);
        var periods = BuildPeriodKeys(dateFrom, dateToInclusive, normalizedGroupBy);
        var result = new List<RfqListAnalyticsTrendPointDto>();

        foreach (var period in periods)
        {
            var (start, end) = ParsePeriodRange(period, normalizedGroupBy);
            var itemsInBucket = bundle.Items
                .Where(i => i.RfqCreateTime >= start && i.RfqCreateTime < end)
                .ToList();

            result.Add(new RfqListAnalyticsTrendPointDto
            {
                Period = period,
                CustomerCount = itemsInBucket
                    .Where(i => !string.IsNullOrWhiteSpace(i.CustomerId))
                    .Select(i => i.CustomerId!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                RfqCount = itemsInBucket
                    .Select(i => i.RfqId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                RfqItemCount = itemsInBucket.Count
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetListAnalyticsBreakdownsAsync(
        RFQItemQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var bundle = await LoadItemAnalyticsBundleAsync(request, cancellationToken);

        var statusItems = BuildHeaderDistinctBreakdown(
            bundle.Items,
            i => NormalizeMainStatus(i.RfqStatus).ToString(),
            i => FormatRfqMainStatus(NormalizeMainStatus(i.RfqStatus)));

        var rfqTypeItems = BuildHeaderDistinctBreakdown(
            bundle.Items,
            i => i.RfqType.ToString(),
            i => FormatRfqType(i.RfqType));

        var targetTypeItems = BuildHeaderDistinctBreakdown(
            bundle.Items,
            i => i.TargetType.ToString(),
            i => FormatTargetType(i.TargetType));

        var industryItems = BuildHeaderDistinctBreakdown(
            bundle.Items,
            i => string.IsNullOrWhiteSpace(i.Industry) ? "_unset" : i.Industry!.Trim(),
            i => string.IsNullOrWhiteSpace(i.Industry) ? "未设置" : i.Industry!.Trim());

        var currencyItems = BuildLineBreakdown(
            bundle.Items,
            i => i.PriceCurrency.ToString(),
            i => FormatPriceCurrency(i.PriceCurrency));

        var brandItems = CollapseBreakdownTailToOther(
            BuildLineBreakdown(
                bundle.Items,
                i => string.IsNullOrWhiteSpace(i.Brand) ? "_unset" : i.Brand.Trim(),
                i => string.IsNullOrWhiteSpace(i.Brand) ? "未设置" : i.Brand.Trim()),
            BrandBreakdownTopN);

        var purchaserItems = BuildPurchaserBreakdown(bundle);
        var quoteItems = BuildQuoteDistributionBreakdown(bundle);

        return new List<SalesAnalyticsBreakdownGroupDto>
        {
            new() { GroupKey = "rfqStatus", GroupLabel = "需求主状态", Items = statusItems },
            new() { GroupKey = "rfqType", GroupLabel = "需求类型", Items = rfqTypeItems },
            new() { GroupKey = "targetType", GroupLabel = "目标类型", Items = targetTypeItems },
            new() { GroupKey = "industry", GroupLabel = "行业", Items = industryItems },
            new() { GroupKey = "currency", GroupLabel = "币别", Items = currencyItems },
            new() { GroupKey = "brand", GroupLabel = "品牌分布", Items = brandItems },
            new() { GroupKey = "assignedPurchaser", GroupLabel = "分配采购员", Items = purchaserItems },
            new() { GroupKey = "quoteDistribution", GroupLabel = "报价分布", Items = quoteItems }
        };
    }

    /// <inheritdoc />
    public async Task<RfqItemListAnalyticsRankingsDto> GetListAnalyticsRankingsAsync(
        RFQItemQueryRequest request,
        bool maskCustomerNames,
        CancellationToken cancellationToken = default)
    {
        const int topN = 10;
        var bundle = await LoadItemAnalyticsBundleAsync(request, cancellationToken);
        var items = bundle.Items;

        var customerByLine = items
            .Where(i => !string.IsNullOrWhiteSpace(i.CustomerId))
            .GroupBy(i => i.CustomerId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = maskCustomerNames ? "—" : (g.First().CustomerName ?? g.Key),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.OrderCount)
            .Take(topN)
            .ToList();

        var salesUserByLine = items
            .GroupBy(i => i.SalesUserId ?? "_unset", StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.Key == "_unset"
                    ? "未分配业务员"
                    : (g.First().SalesUserName ?? g.Key),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.OrderCount)
            .Take(topN)
            .ToList();

        var mpnByLine = items
            .GroupBy(i => string.IsNullOrWhiteSpace(i.Mpn) ? "_unset" : i.Mpn.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.Key == "_unset" ? "未设置" : g.Key,
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.OrderCount)
            .Take(topN)
            .ToList();

        var mpnByQty = items
            .GroupBy(i => string.IsNullOrWhiteSpace(i.Mpn) ? "_unset" : i.Mpn.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.Key == "_unset" ? "未设置" : g.Key,
                OrderCount = (int)Math.Round(g.Sum(x => x.Quantity), MidpointRounding.AwayFromZero)
            })
            .OrderByDescending(x => x.OrderCount)
            .Take(topN)
            .ToList();

        var brandByLine = items
            .GroupBy(i => string.IsNullOrWhiteSpace(i.Brand) ? "_unset" : i.Brand.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.Key == "_unset" ? "未设置" : g.Key,
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.OrderCount)
            .Take(topN)
            .ToList();

        var brandByQty = items
            .GroupBy(i => string.IsNullOrWhiteSpace(i.Brand) ? "_unset" : i.Brand.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.Key == "_unset" ? "未设置" : g.Key,
                OrderCount = (int)Math.Round(g.Sum(x => x.Quantity), MidpointRounding.AwayFromZero)
            })
            .OrderByDescending(x => x.OrderCount)
            .Take(topN)
            .ToList();

        return new RfqItemListAnalyticsRankingsDto
        {
            CustomerByLineCount = customerByLine,
            SalesUserByLineCount = salesUserByLine,
            MpnByLineCount = mpnByLine,
            MpnByQty = mpnByQty,
            BrandByLineCount = brandByLine,
            BrandByQty = brandByQty
        };
    }

    private static RfqListAnalyticsSnapshotDto BuildItemSnapshot(RfqItemAnalyticsBundle bundle)
    {
        var customerRfqCounts = bundle.Items
            .Where(i => !string.IsNullOrWhiteSpace(i.CustomerId))
            .GroupBy(i => i.CustomerId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Select(x => x.RfqId).Distinct(StringComparer.OrdinalIgnoreCase).Count())
            .ToList();

        var effectiveDenominator = bundle.Items.Count(i => i.RawStatus != NoQuoteFoundItemStatus);
        var convertedCount = bundle.Items.Count(i => bundle.ConvertedItemIds.Contains(i.Id));
        decimal? rate = effectiveDenominator == 0
            ? null
            : Math.Round((decimal)convertedCount / effectiveDenominator * 100m, 2);

        return new RfqListAnalyticsSnapshotDto
        {
            PublishedCustomerCount = customerRfqCounts.Count,
            RepeatInquiryCustomerCount = customerRfqCounts.Count(c => c >= 2),
            RepeatInquiryRfqCount = customerRfqCounts.Sum(c => Math.Max(0, c - 1)),
            RfqCount = bundle.Items
                .Select(i => i.RfqId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(),
            RfqItemCount = bundle.Items.Count,
            ConvertedLineCount = convertedCount,
            ConversionRate = rate
        };
    }

    private async Task<RfqItemAnalyticsBundle> LoadItemAnalyticsBundleAsync(
        RFQItemQueryRequest request,
        CancellationToken cancellationToken)
    {
        var filtered = await RfqItemListFilter.BuildFilteredJoinQueryAsync(
            _db, _rbacService, _dataPermission, request, cancellationToken);

        var rows = await filtered
            .Select(x => new RfqItemAnalyticsRow
            {
                Id = x.Item.Id ?? string.Empty,
                RfqId = x.Item.RfqId,
                CustomerId = x.Rfq.CustomerId,
                CustomerName = x.Customer != null
                    ? (x.Customer.OfficialName ?? x.Customer.NickName ?? x.Customer.CustomerCode)
                    : null,
                SalesUserId = x.Rfq.SalesUserId,
                SalesUserName = x.SalesUser != null ? x.SalesUser.UserName : null,
                RfqStatus = x.Rfq.Status,
                RfqType = x.Rfq.RfqType,
                TargetType = x.Rfq.TargetType,
                Industry = x.Rfq.Industry,
                RfqCreateTime = x.Rfq.CreateTime,
                RawStatus = x.Item.Status,
                Mpn = x.Item.Mpn,
                Brand = x.Item.Brand,
                PriceCurrency = x.Item.PriceCurrency,
                Quantity = x.Item.Quantity,
                PurchaserId1 = x.Item.AssignedPurchaserUserId1,
                PurchaserId2 = x.Item.AssignedPurchaserUserId2
            })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return new RfqItemAnalyticsBundle();

        var itemIds = rows.Select(i => i.Id).Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
        var quotedItemIds = itemIds.Count == 0
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : (await _db.Quotes.AsNoTracking()
                .Where(q => q.RFQItemId != null && itemIds.Contains(q.RFQItemId))
                .Select(q => q.RFQItemId!)
                .Distinct()
                .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var convertedItemIds = itemIds.Count == 0
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : (await (
                from oi in _db.SellOrderItems.AsNoTracking()
                join so in _db.SellOrders.AsNoTracking() on oi.SellOrderId equals so.Id
                join q in _db.Quotes.AsNoTracking() on oi.QuoteId equals q.Id
                where !oi.IsDeleted
                      && oi.Status == 0
                      && oi.QuoteId != null
                      && so.Status >= SellOrderMainStatus.Approved
                      && q.RFQItemId != null
                      && itemIds.Contains(q.RFQItemId)
                select q.RFQItemId!
            ).Distinct().ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var purchaserIds = rows
            .SelectMany(i => new[] { i.PurchaserId1, i.PurchaserId2 })
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var purchaserNames = purchaserIds.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : await _db.Users.AsNoTracking()
                .Where(u => purchaserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName })
                .ToDictionaryAsync(x => x.Id, x => x.UserName ?? x.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        return new RfqItemAnalyticsBundle
        {
            Items = rows,
            QuotedItemIds = quotedItemIds,
            ConvertedItemIds = convertedItemIds,
            PurchaserNames = purchaserNames
        };
    }

    private static List<SalesAnalyticsBreakdownItemDto> BuildHeaderDistinctBreakdown(
        List<RfqItemAnalyticsRow> items,
        Func<RfqItemAnalyticsRow, string> keySelector,
        Func<RfqItemAnalyticsRow, string> labelSelector)
    {
        var breakdown = items
            .GroupBy(keySelector, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var sample = g.First();
                return new SalesAnalyticsBreakdownItemDto
                {
                    Key = g.Key,
                    Label = labelSelector(sample),
                    Value = g.Select(x => x.RfqId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                    Ratio = 0
                };
            })
            .ToList();
        ApplyRatios(breakdown);
        return breakdown;
    }

    private static List<SalesAnalyticsBreakdownItemDto> BuildLineBreakdown(
        List<RfqItemAnalyticsRow> items,
        Func<RfqItemAnalyticsRow, string> keySelector,
        Func<RfqItemAnalyticsRow, string> labelSelector)
    {
        var breakdown = items
            .GroupBy(keySelector, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var sample = g.First();
                return new SalesAnalyticsBreakdownItemDto
                {
                    Key = g.Key,
                    Label = labelSelector(sample),
                    Value = g.Count(),
                    Ratio = 0
                };
            })
            .ToList();
        ApplyRatios(breakdown);
        return breakdown;
    }

    private List<SalesAnalyticsBreakdownItemDto> BuildPurchaserBreakdown(RfqItemAnalyticsBundle bundle)
    {
        var counts = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in bundle.Items)
        {
            var purchasers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(item.PurchaserId1))
                purchasers.Add(item.PurchaserId1.Trim());
            if (!string.IsNullOrWhiteSpace(item.PurchaserId2))
                purchasers.Add(item.PurchaserId2.Trim());

            if (purchasers.Count == 0)
            {
                counts.TryGetValue("_unset", out var cur);
                counts["_unset"] = cur + 1;
                continue;
            }

            foreach (var pid in purchasers)
            {
                counts.TryGetValue(pid, out var cur);
                counts[pid] = cur + 1;
            }
        }

        var items = counts
            .Select(kv => new SalesAnalyticsBreakdownItemDto
            {
                Key = kv.Key,
                Label = kv.Key == "_unset"
                    ? "未分配采购员"
                    : (bundle.PurchaserNames.TryGetValue(kv.Key, out var name) ? name : kv.Key),
                Value = kv.Value,
                Ratio = 0
            })
            .ToList();
        ApplyRatios(items);
        return items;
    }

    private static List<SalesAnalyticsBreakdownItemDto> BuildQuoteDistributionBreakdown(RfqItemAnalyticsBundle bundle)
    {
        int hasQuote = 0, noQuote = 0, pending = 0;
        foreach (var item in bundle.Items)
        {
            if (item.RawStatus == NoQuoteFoundItemStatus)
            {
                noQuote++;
                continue;
            }

            if (item.RawStatus >= 1 || bundle.QuotedItemIds.Contains(item.Id))
            {
                hasQuote++;
                continue;
            }

            if (item.RawStatus == 0)
                pending++;
        }

        var items = new List<SalesAnalyticsBreakdownItemDto>
        {
            new() { Key = "hasQuote", Label = "有报价", Value = hasQuote, Ratio = 0 },
            new() { Key = "noQuoteFound", Label = "查无报价", Value = noQuote, Ratio = 0 },
            new() { Key = "pendingUnprocessed", Label = "采购未处理", Value = pending, Ratio = 0 }
        };
        ApplyRatios(items);
        return items;
    }

    private static (DateTime From, DateTime ToInclusive) ResolveItemTrendDateBounds(
        RFQItemQueryRequest request,
        DateTime minCreateTime,
        DateTime maxCreateTime)
    {
        var from = request.StartDate?.Date ?? minCreateTime.Date;
        var to = request.EndDate?.Date ?? maxCreateTime.Date;
        if (to < from) to = from;
        return (from, to);
    }

    private static short NormalizeMainStatus(short status) => status == 6 ? (short)7 : status;

    private static string FormatRfqMainStatus(short status) => status switch
    {
        0 => "待分配",
        1 => "已分配",
        2 => "报价中",
        3 => "已报价",
        4 => "已选价",
        5 => "已转订单",
        7 => "已关闭",
        8 => "已取消",
        _ => $"状态{status}"
    };

    private static string FormatRfqType(short type) => type switch
    {
        1 => "现货",
        2 => "排单",
        3 => "代理",
        4 => "自营",
        5 => "信息服务",
        _ => $"类型{type}"
    };

    private static string FormatTargetType(short type) => type switch
    {
        1 => "比价需求",
        2 => "独家需求",
        3 => "紧急需求",
        4 => "常规需求",
        _ => $"目标{type}"
    };

    private static string FormatPriceCurrency(short currency) => currency switch
    {
        1 => "RMB",
        2 => "USD",
        3 => "EUR",
        4 => "HKD",
        _ => $"币别{currency}"
    };

    private static string NormalizeGroupBy(string? groupBy) =>
        groupBy switch
        {
            "day" => "day",
            "week" => "week",
            _ => "month"
        };

    private static List<SalesAnalyticsBreakdownItemDto> CollapseBreakdownTailToOther(
        List<SalesAnalyticsBreakdownItemDto> items,
        int topN,
        string otherKey = "_other",
        string otherLabel = "其他")
    {
        if (items.Count <= topN)
        {
            var orderedOnly = items.OrderByDescending(x => x.Value).ToList();
            ApplyRatios(orderedOnly);
            return orderedOnly;
        }

        var sorted = items.OrderByDescending(x => x.Value).ToList();
        var top = sorted.Take(topN).ToList();
        var otherValue = sorted.Skip(topN).Sum(x => x.Value);
        if (otherValue > 0)
        {
            top.Add(new SalesAnalyticsBreakdownItemDto
            {
                Key = otherKey,
                Label = otherLabel,
                Value = otherValue,
                Ratio = 0
            });
        }

        ApplyRatios(top);
        return top;
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
        "week" => $"{date:yyyy}-W{System.Globalization.ISOWeek.GetWeekOfYear(date):D2}",
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
            var parts = period.Split("-W", StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && int.TryParse(parts[0], out var year) && int.TryParse(parts[1], out var week))
            {
                var start = System.Globalization.ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
                return (start, start.AddDays(7));
            }
        }

        return (DateTime.MinValue, DateTime.MaxValue);
    }

    private sealed class RfqItemAnalyticsBundle
    {
        public List<RfqItemAnalyticsRow> Items { get; set; } = new();
        public HashSet<string> QuotedItemIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> ConvertedItemIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> PurchaserNames { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class RfqItemAnalyticsRow
    {
        public string Id { get; set; } = string.Empty;
        public string RfqId { get; set; } = string.Empty;
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? SalesUserId { get; set; }
        public string? SalesUserName { get; set; }
        public short RfqStatus { get; set; }
        public short RfqType { get; set; }
        public short TargetType { get; set; }
        public string? Industry { get; set; }
        public DateTime RfqCreateTime { get; set; }
        public short RawStatus { get; set; }
        public string Mpn { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public short PriceCurrency { get; set; }
        public decimal Quantity { get; set; }
        public string? PurchaserId1 { get; set; }
        public string? PurchaserId2 { get; set; }
    }
}
