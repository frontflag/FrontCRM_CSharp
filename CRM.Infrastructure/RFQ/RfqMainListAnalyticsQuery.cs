using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Quote;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.RfqListQueries;

public sealed partial class RfqMainListQuery
{
    private const short NoQuoteFoundItemStatus = 5;

    /// <inheritdoc />
    public async Task<RfqListAnalyticsDashboardDto> GetListAnalyticsDashboardAsync(
        RFQQueryRequest request,
        bool maskCustomerNames,
        CancellationToken cancellationToken = default)
    {
        var bundle = await LoadAnalyticsBundleAsync(request, cancellationToken);
        var snapshot = BuildSnapshot(bundle);

        return new RfqListAnalyticsDashboardDto
        {
            Context = new RfqListAnalyticsContextDto { MaskCustomerNames = maskCustomerNames },
            Snapshot = snapshot
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RfqListAnalyticsTrendPointDto>> GetListAnalyticsTrendsAsync(
        RFQQueryRequest request,
        string groupBy,
        CancellationToken cancellationToken = default)
    {
        var bundle = await LoadAnalyticsBundleAsync(request, cancellationToken);
        if (bundle.Rfqs.Count == 0)
            return Array.Empty<RfqListAnalyticsTrendPointDto>();

        var (dateFrom, dateToInclusive) = ResolveTrendDateBounds(
            request,
            bundle.Rfqs.Min(r => r.CreateTime),
            bundle.Rfqs.Max(r => r.CreateTime));
        var normalizedGroupBy = NormalizeGroupBy(groupBy);
        var periods = BuildPeriodKeys(dateFrom, dateToInclusive, normalizedGroupBy);
        var result = new List<RfqListAnalyticsTrendPointDto>();

        foreach (var period in periods)
        {
            var (start, end) = ParsePeriodRange(period, normalizedGroupBy);
            var rfqsInBucket = bundle.Rfqs
                .Where(r => r.CreateTime >= start && r.CreateTime < end)
                .ToList();
            var rfqIds = rfqsInBucket.Select(r => r.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var itemsInBucket = bundle.Items.Where(i => rfqIds.Contains(i.RfqId)).ToList();

            result.Add(new RfqListAnalyticsTrendPointDto
            {
                Period = period,
                CustomerCount = rfqsInBucket
                    .Where(r => !string.IsNullOrWhiteSpace(r.CustomerId))
                    .Select(r => r.CustomerId!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                RfqCount = rfqsInBucket.Count,
                RfqItemCount = itemsInBucket.Count
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetListAnalyticsBreakdownsAsync(
        RFQQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var bundle = await LoadAnalyticsBundleAsync(request, cancellationToken);

        var statusItems = bundle.Rfqs
            .GroupBy(r => NormalizeMainStatus(r.Status))
            .Select(g => new SalesAnalyticsBreakdownItemDto
            {
                Key = g.Key.ToString(),
                Label = FormatRfqMainStatus(g.Key),
                Value = g.Count(),
                Ratio = 0
            })
            .ToList();
        ApplyRatios(statusItems);

        var rfqTypeItems = BuildRfqHeaderBreakdown(
            bundle.Rfqs,
            r => r.RfqType.ToString(),
            r => FormatRfqType(r.RfqType));

        var targetTypeItems = BuildRfqHeaderBreakdown(
            bundle.Rfqs,
            r => r.TargetType.ToString(),
            r => FormatTargetType(r.TargetType));

        var industryItems = BuildRfqHeaderBreakdown(
            bundle.Rfqs,
            r => string.IsNullOrWhiteSpace(r.Industry) ? "_unset" : r.Industry!.Trim(),
            r => string.IsNullOrWhiteSpace(r.Industry) ? "未设置" : r.Industry!.Trim());

        var purchaserItems = BuildPurchaserBreakdown(bundle);
        var quoteItems = BuildQuoteDistributionBreakdown(bundle);

        return new List<SalesAnalyticsBreakdownGroupDto>
        {
            new() { GroupKey = "rfqStatus", GroupLabel = "需求主状态", Items = statusItems },
            new() { GroupKey = "rfqType", GroupLabel = "需求类型", Items = rfqTypeItems },
            new() { GroupKey = "targetType", GroupLabel = "目标类型", Items = targetTypeItems },
            new() { GroupKey = "industry", GroupLabel = "行业", Items = industryItems },
            new() { GroupKey = "assignedPurchaser", GroupLabel = "分配采购员", Items = purchaserItems },
            new() { GroupKey = "quoteDistribution", GroupLabel = "报价分布", Items = quoteItems }
        };
    }

    /// <inheritdoc />
    public async Task<RfqListAnalyticsRankingsDto> GetListAnalyticsRankingsAsync(
        RFQQueryRequest request,
        bool maskCustomerNames,
        CancellationToken cancellationToken = default)
    {
        const int topN = 10;
        var bundle = await LoadAnalyticsBundleAsync(request, cancellationToken);
        var rfqById = bundle.Rfqs.ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);

        var customerByLine = bundle.Items
            .Select(i =>
            {
                rfqById.TryGetValue(i.RfqId, out var rfq);
                return new
                {
                    i.Id,
                    rfq?.CustomerId,
                    CustomerName = rfq?.CustomerName
                };
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.CustomerId))
            .GroupBy(x => x.CustomerId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = maskCustomerNames ? "—" : (g.First().CustomerName ?? g.Key),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.OrderCount)
            .Take(topN)
            .ToList();

        var salesUserByLine = bundle.Items
            .Select(i =>
            {
                rfqById.TryGetValue(i.RfqId, out var rfq);
                return new
                {
                    i.Id,
                    rfq?.SalesUserId,
                    rfq?.SalesUserName
                };
            })
            .GroupBy(x => x.SalesUserId ?? "_unset", StringComparer.OrdinalIgnoreCase)
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

        return new RfqListAnalyticsRankingsDto
        {
            CustomerByLineCount = customerByLine,
            SalesUserByLineCount = salesUserByLine
        };
    }

    private static RfqListAnalyticsSnapshotDto BuildSnapshot(RfqAnalyticsBundle bundle)
    {
        var customerGroups = bundle.Rfqs
            .Where(r => !string.IsNullOrWhiteSpace(r.CustomerId))
            .GroupBy(r => r.CustomerId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Count())
            .ToList();

        var effectiveDenominator = bundle.Items.Count(i => i.Status != NoQuoteFoundItemStatus);
        var convertedCount = bundle.Items.Count(i => bundle.ConvertedItemIds.Contains(i.Id));
        decimal? rate = effectiveDenominator == 0
            ? null
            : Math.Round((decimal)convertedCount / effectiveDenominator * 100m, 2);

        return new RfqListAnalyticsSnapshotDto
        {
            PublishedCustomerCount = customerGroups.Count,
            RepeatInquiryCustomerCount = customerGroups.Count(c => c >= 2),
            RepeatInquiryRfqCount = customerGroups.Sum(c => Math.Max(0, c - 1)),
            RfqCount = bundle.Rfqs.Count,
            RfqItemCount = bundle.Items.Count,
            ConvertedLineCount = convertedCount,
            ConversionRate = rate
        };
    }

    private async Task<RfqAnalyticsBundle> LoadAnalyticsBundleAsync(
        RFQQueryRequest request,
        CancellationToken cancellationToken)
    {
        var filtered = await BuildFilteredQueryAsync(request, cancellationToken);
        var rfqs = await (
            from r in filtered
            join c in _db.Customers.AsNoTracking() on r.CustomerId equals c.Id into cj
            from c in cj.DefaultIfEmpty()
            join u in _db.Users.AsNoTracking() on r.SalesUserId equals u.Id into uj
            from u in uj.DefaultIfEmpty()
            select new RfqAnalyticsHeaderRow
            {
                Id = r.Id,
                CustomerId = r.CustomerId,
                CustomerName = c != null ? (c.OfficialName ?? c.NickName ?? c.CustomerCode) : null,
                SalesUserId = r.SalesUserId,
                SalesUserName = u != null ? u.UserName : null,
                Status = r.Status,
                RfqType = r.RfqType,
                TargetType = r.TargetType,
                Industry = r.Industry,
                CreateTime = r.CreateTime
            }
        ).ToListAsync(cancellationToken);

        if (rfqs.Count == 0)
            return new RfqAnalyticsBundle();

        var rfqIds = rfqs.Select(r => r.Id).ToList();
        var items = await _db.RFQItems.AsNoTracking()
            .Where(i => !i.IsDeleted && rfqIds.Contains(i.RfqId))
            .Select(i => new RfqAnalyticsItemRow
            {
                Id = i.Id,
                RfqId = i.RfqId,
                Status = i.Status,
                PurchaserId1 = i.AssignedPurchaserUserId1,
                PurchaserId2 = i.AssignedPurchaserUserId2
            })
            .ToListAsync(cancellationToken);

        var itemIds = items.Select(i => i.Id).ToList();
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

        var purchaserIds = items
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

        return new RfqAnalyticsBundle
        {
            Rfqs = rfqs,
            Items = items,
            QuotedItemIds = quotedItemIds,
            ConvertedItemIds = convertedItemIds,
            PurchaserNames = purchaserNames
        };
    }

    private static List<SalesAnalyticsBreakdownItemDto> BuildRfqHeaderBreakdown(
        List<RfqAnalyticsHeaderRow> rfqs,
        Func<RfqAnalyticsHeaderRow, string> keySelector,
        Func<RfqAnalyticsHeaderRow, string> labelSelector)
    {
        var items = rfqs
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
        ApplyRatios(items);
        return items;
    }

    private List<SalesAnalyticsBreakdownItemDto> BuildPurchaserBreakdown(RfqAnalyticsBundle bundle)
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

    private static List<SalesAnalyticsBreakdownItemDto> BuildQuoteDistributionBreakdown(RfqAnalyticsBundle bundle)
    {
        int hasQuote = 0, noQuote = 0, pending = 0;
        foreach (var item in bundle.Items)
        {
            if (item.Status == NoQuoteFoundItemStatus)
            {
                noQuote++;
                continue;
            }

            if (item.Status >= 1 || bundle.QuotedItemIds.Contains(item.Id))
            {
                hasQuote++;
                continue;
            }

            if (item.Status == 0)
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

    private static (DateTime From, DateTime ToInclusive) ResolveTrendDateBounds(
        RFQQueryRequest request,
        DateTime minCreateTime,
        DateTime maxCreateTime)
    {
        var from = request.StartDate?.Date ?? minCreateTime.Date;
        var to = request.EndDate?.Date ?? maxCreateTime.Date;
        if (to < from) to = from;
        return (from, to);
    }

    private static string NormalizeGroupBy(string? groupBy) =>
        groupBy switch
        {
            "day" => "day",
            "week" => "week",
            _ => "month"
        };

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

    private sealed class RfqAnalyticsBundle
    {
        public List<RfqAnalyticsHeaderRow> Rfqs { get; set; } = new();
        public List<RfqAnalyticsItemRow> Items { get; set; } = new();
        public HashSet<string> QuotedItemIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> ConvertedItemIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> PurchaserNames { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class RfqAnalyticsHeaderRow
    {
        public string Id { get; set; } = string.Empty;
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? SalesUserId { get; set; }
        public string? SalesUserName { get; set; }
        public short Status { get; set; }
        public short RfqType { get; set; }
        public short TargetType { get; set; }
        public string? Industry { get; set; }
        public DateTime CreateTime { get; set; }
    }

    private sealed class RfqAnalyticsItemRow
    {
        public string Id { get; set; } = string.Empty;
        public string RfqId { get; set; } = string.Empty;
        public short Status { get; set; }
        public string? PurchaserId1 { get; set; }
        public string? PurchaserId2 { get; set; }
    }
}
