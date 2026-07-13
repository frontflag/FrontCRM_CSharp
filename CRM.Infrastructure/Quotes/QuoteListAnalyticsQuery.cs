using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Quote;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Quotes;

public sealed partial class QuoteListQuery
{
    private const short NoQuoteFoundItemStatus = 5;
    private const int BrandBreakdownTopN = 20;
    private const int RankingTopN = 10;

    /// <inheritdoc />
    public async Task<QuoteListAnalyticsDashboardDto> GetListAnalyticsDashboardAsync(
        QuoteQueryRequest request,
        bool maskCustomerNames,
        bool maskVendorNames,
        CancellationToken cancellationToken = default)
    {
        var bundle = await LoadQuoteAnalyticsBundleAsync(request, cancellationToken);
        return new QuoteListAnalyticsDashboardDto
        {
            Context = new QuoteListAnalyticsContextDto
            {
                MaskCustomerNames = maskCustomerNames,
                MaskVendorNames = maskVendorNames
            },
            Snapshot = BuildQuoteSnapshot(bundle)
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<QuoteListAnalyticsTrendPointDto>> GetListAnalyticsTrendsAsync(
        QuoteQueryRequest request,
        string groupBy,
        CancellationToken cancellationToken = default)
    {
        var bundle = await LoadQuoteAnalyticsBundleAsync(request, cancellationToken);
        if (bundle.DemandItems.Count == 0)
            return Array.Empty<QuoteListAnalyticsTrendPointDto>();

        var minTime = bundle.DemandItems.Min(i => i.RfqCreateTime);
        var maxTime = bundle.DemandItems.Max(i => i.RfqCreateTime);
        var (dateFrom, dateToInclusive) = ResolveQuoteTrendDateBounds(request, minTime, maxTime);
        var normalizedGroupBy = NormalizeGroupBy(groupBy);
        var periods = BuildPeriodKeys(dateFrom, dateToInclusive, normalizedGroupBy);
        var result = new List<QuoteListAnalyticsTrendPointDto>();

        foreach (var period in periods)
        {
            var (start, end) = ParsePeriodRange(period, normalizedGroupBy);
            var itemsInBucket = bundle.DemandItems
                .Where(i => i.RfqCreateTime >= start && i.RfqCreateTime < end)
                .ToList();
            var itemIdSet = itemsInBucket
                .Select(i => i.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var vendorIds = bundle.QuoteItemRows
                .Where(qi =>
                    !string.IsNullOrWhiteSpace(qi.VendorId) &&
                    itemIdSet.Contains(qi.RfqItemId))
                .Select(qi => qi.VendorId!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            var validQuotesInBucket = bundle.FilteredQuotes
                .Count(q => q.RfqItemId != null && itemIdSet.Contains(q.RfqItemId));

            result.Add(new QuoteListAnalyticsTrendPointDto
            {
                Period = period,
                QuoteVendorCount = vendorIds,
                RfqItemCount = itemsInBucket.Count,
                TotalDemandCount = itemsInBucket.Count,
                ValidQuoteCount = validQuotesInBucket
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetListAnalyticsBreakdownsAsync(
        QuoteQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var bundle = await LoadQuoteAnalyticsBundleAsync(request, cancellationToken);

        var quoteStatusItems = BuildQuoteBreakdown(
            bundle.FilteredQuotes,
            q => q.Status.ToString(),
            q => FormatQuoteMainStatus(q.Status));

        var quoteDistributionItems = BuildQuoteDistributionBreakdown(bundle);
        var assignedPurchaserItems = BuildAssignedPurchaserBreakdown(bundle);

        var quotePurchaserItems = BuildQuotePurchaserBreakdown(bundle);

        var labelTypeItems = BuildFirstItemQuoteBreakdown(
            bundle,
            x => x.LabelType.ToString(),
            x => FormatLabelType(x.LabelType));

        var waferOriginItems = BuildFirstItemQuoteBreakdown(
            bundle,
            x => x.WaferOrigin.ToString(),
            x => FormatOrigin(x.WaferOrigin));

        var packageOriginItems = BuildFirstItemQuoteBreakdown(
            bundle,
            x => x.PackageOrigin.ToString(),
            x => FormatOrigin(x.PackageOrigin));

        var freeShippingItems = BuildFirstItemQuoteBreakdown(
            bundle,
            x => x.FreeShipping ? "1" : "0",
            x => x.FreeShipping ? "是" : "否");

        var brandItems = CollapseBreakdownTailToOther(
            BuildFirstItemQuoteBreakdown(
                bundle,
                x => string.IsNullOrWhiteSpace(x.Brand) ? "_unset" : x.Brand.Trim(),
                x => string.IsNullOrWhiteSpace(x.Brand) ? "未设置" : x.Brand.Trim()),
            BrandBreakdownTopN);

        return new List<SalesAnalyticsBreakdownGroupDto>
        {
            new() { GroupKey = "quoteStatus", GroupLabel = "报价主状态", Items = quoteStatusItems },
            new() { GroupKey = "quoteDistribution", GroupLabel = "报价分布", Items = quoteDistributionItems },
            new() { GroupKey = "labelType", GroupLabel = "涂标", Items = labelTypeItems },
            new() { GroupKey = "waferOrigin", GroupLabel = "晶圆产地", Items = waferOriginItems },
            new() { GroupKey = "packageOrigin", GroupLabel = "封装产地", Items = packageOriginItems },
            new() { GroupKey = "freeShipping", GroupLabel = "包邮", Items = freeShippingItems },
            new() { GroupKey = "brand", GroupLabel = "品牌分布", Items = brandItems },
            new() { GroupKey = "assignedPurchaser", GroupLabel = "分配采购员", Items = assignedPurchaserItems },
            new() { GroupKey = "quotePurchaser", GroupLabel = "报价采购员", Items = quotePurchaserItems }
        };
    }

    /// <inheritdoc />
    public async Task<QuoteListAnalyticsRankingsDto> GetListAnalyticsRankingsAsync(
        QuoteQueryRequest request,
        bool maskCustomerNames,
        bool maskVendorNames,
        CancellationToken cancellationToken = default)
    {
        _ = maskCustomerNames;
        var bundle = await LoadQuoteAnalyticsBundleAsync(request, cancellationToken);

        var vendorByRfqItem = bundle.QuoteItemRows
            .Where(qi => !string.IsNullOrWhiteSpace(qi.VendorId) && !string.IsNullOrWhiteSpace(qi.RfqItemId))
            .GroupBy(qi => qi.VendorId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = maskVendorNames
                    ? "—"
                    : ResolveVendorDisplayName(g.Key, g.First().VendorName, bundle.VendorNames),
                OrderCount = g.Select(x => x.RfqItemId).Distinct(StringComparer.OrdinalIgnoreCase).Count()
            })
            .OrderByDescending(x => x.OrderCount)
            .Take(RankingTopN)
            .ToList();

        var purchaserByQuoteCount = bundle.FilteredQuotes
            .GroupBy(q => q.PurchaseUserId ?? "_unset", StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.Key == "_unset"
                    ? "未分配采购员"
                    : (bundle.UserNames.TryGetValue(g.Key, out var n) ? n : g.Key),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.OrderCount)
            .Take(RankingTopN)
            .ToList();

        var assignedCounts = BuildAssignedDemandCounts(bundle);
        var quoteCountsByPurchaser = bundle.FilteredQuotes
            .Where(q => !string.IsNullOrWhiteSpace(q.PurchaseUserId))
            .GroupBy(q => q.PurchaseUserId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var purchaserByQuoteRate = assignedCounts
            .Select(kv =>
            {
                quoteCountsByPurchaser.TryGetValue(kv.Key, out var quoteCnt);
                decimal? rate = kv.Value == 0
                    ? null
                    : Math.Round((decimal)quoteCnt / kv.Value * 100m, 2);
                return new SalesAnalyticsRankingRowDto
                {
                    Id = kv.Key,
                    Name = kv.Key == "_unset"
                        ? "未分配采购员"
                        : (bundle.UserNames.TryGetValue(kv.Key, out var n) ? n : kv.Key),
                    OrderCount = quoteCnt,
                    Amount = rate
                };
            })
            .OrderByDescending(x => x.Amount ?? 0m)
            .ThenByDescending(x => x.OrderCount)
            .Take(RankingTopN)
            .ToList();

        var demandQtyByItemId = bundle.DemandItems.ToDictionary(
            i => i.Id,
            i => i.Quantity,
            StringComparer.OrdinalIgnoreCase);

        var mpnByQuoteCount = bundle.FilteredQuotes
            .GroupBy(q => string.IsNullOrWhiteSpace(q.Mpn) ? "_unset" : q.Mpn.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.Key == "_unset" ? "未设置" : g.Key,
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.OrderCount)
            .Take(RankingTopN)
            .ToList();

        var mpnByQty = bundle.FilteredQuotes
            .Where(q => q.RfqItemId != null)
            .GroupBy(q => string.IsNullOrWhiteSpace(q.Mpn) ? "_unset" : q.Mpn.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.Key == "_unset" ? "未设置" : g.Key,
                OrderCount = (int)Math.Round(
                    g.Sum(x => demandQtyByItemId.TryGetValue(x.RfqItemId!, out var qty) ? qty : 0m),
                    MidpointRounding.AwayFromZero)
            })
            .OrderByDescending(x => x.OrderCount)
            .Take(RankingTopN)
            .ToList();

        var brandByQuoteCount = bundle.FilteredQuotes
            .Select(q => bundle.FirstItemByQuoteId.TryGetValue(q.Id, out var fi) ? fi : null)
            .Where(fi => fi != null)
            .GroupBy(
                fi => string.IsNullOrWhiteSpace(fi!.Brand) ? "_unset" : fi.Brand.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.Key == "_unset" ? "未设置" : g.Key,
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.OrderCount)
            .Take(RankingTopN)
            .ToList();

        var brandByQty = bundle.FilteredQuotes
            .Where(q => q.RfqItemId != null && bundle.FirstItemByQuoteId.ContainsKey(q.Id))
            .GroupBy(q =>
            {
                var fi = bundle.FirstItemByQuoteId[q.Id];
                return string.IsNullOrWhiteSpace(fi.Brand) ? "_unset" : fi.Brand.Trim();
            }, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.Key == "_unset" ? "未设置" : g.Key,
                OrderCount = (int)Math.Round(
                    g.Sum(x => demandQtyByItemId.TryGetValue(x.RfqItemId!, out var qty) ? qty : 0m),
                    MidpointRounding.AwayFromZero)
            })
            .OrderByDescending(x => x.OrderCount)
            .Take(RankingTopN)
            .ToList();

        return new QuoteListAnalyticsRankingsDto
        {
            VendorByRfqItemCount = vendorByRfqItem,
            PurchaserByQuoteCount = purchaserByQuoteCount,
            PurchaserByQuoteRate = purchaserByQuoteRate,
            MpnByQuoteCount = mpnByQuoteCount,
            MpnByQty = mpnByQty,
            BrandByQuoteCount = brandByQuoteCount,
            BrandByQty = brandByQty
        };
    }

    private static QuoteListAnalyticsSnapshotDto BuildQuoteSnapshot(QuoteAnalyticsBundle bundle)
    {
        var vendorCount = bundle.QuoteItemRows
            .Where(qi => !string.IsNullOrWhiteSpace(qi.VendorId))
            .Select(qi => qi.VendorId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var validQuoteCount = bundle.FilteredQuotes.Count;
        var noQuoteFoundCount = bundle.DemandItems.Count(i => i.RawStatus == NoQuoteFoundItemStatus);

        var effectiveDemand = bundle.DemandItems.Where(i => i.RawStatus != NoQuoteFoundItemStatus).ToList();
        var effectiveDenominator = effectiveDemand.Count;
        var quotedDemandCount = effectiveDemand.Count(i => bundle.ScopedQuotedItemIds.Contains(i.Id));

        decimal? rfqQuoteRate = effectiveDenominator == 0
            ? null
            : Math.Round((decimal)quotedDemandCount / effectiveDenominator * 100m, 2);

        var responseSamples = new List<double>();
        foreach (var item in effectiveDemand.Where(i => bundle.ScopedQuotedItemIds.Contains(i.Id)))
        {
            if (!bundle.EarliestQuoteCreateByItemId.TryGetValue(item.Id, out var earliest))
                continue;
            responseSamples.Add((earliest - item.RfqCreateTime).TotalMinutes);
        }

        decimal? avgResponseMinutes = responseSamples.Count == 0
            ? null
            : Math.Round((decimal)responseSamples.Average(), 1);

        var quotedItemsForAvg = effectiveDemand.Where(i => bundle.ScopedQuotedItemIds.Contains(i.Id)).ToList();
        decimal? avgQuotesPerItem = null;
        if (quotedItemsForAvg.Count > 0)
        {
            var totalQuotes = bundle.ScopedQuotes.Count(q =>
                q.RfqItemId != null && quotedItemsForAvg.Any(i => i.Id.Equals(q.RfqItemId, StringComparison.OrdinalIgnoreCase)));
            avgQuotesPerItem = Math.Round((decimal)totalQuotes / quotedItemsForAvg.Count, 1);
        }

        var convertedCount = bundle.DemandItems.Count(i => bundle.ConvertedItemIds.Contains(i.Id));
        var quoteConversionDenominator = effectiveDemand.Count(i => bundle.ScopedQuotedItemIds.Contains(i.Id));
        decimal? quoteConversionRate = quoteConversionDenominator == 0
            ? null
            : Math.Round((decimal)convertedCount / quoteConversionDenominator * 100m, 2);

        return new QuoteListAnalyticsSnapshotDto
        {
            QuoteVendorCount = vendorCount,
            ValidQuoteCount = validQuoteCount,
            NoQuoteFoundItemCount = noQuoteFoundCount,
            RfqQuoteRate = rfqQuoteRate,
            AvgResponseMinutes = avgResponseMinutes,
            AvgQuotesPerRfqItem = avgQuotesPerItem,
            ConvertedLineCount = convertedCount,
            QuoteConversionRate = quoteConversionRate
        };
    }

    private async Task<QuoteAnalyticsBundle> LoadQuoteAnalyticsBundleAsync(
        QuoteQueryRequest request,
        CancellationToken cancellationToken)
    {
        var demandQuery = await QuoteListFilter.BuildParallelDemandQueryAsync(
            _db, _rbacService, _dataPermission, _purchaseQuoterPoolService, request, cancellationToken);
        var demandRows = await demandQuery
            .Select(x => new QuoteDemandAnalyticsRow
            {
                Id = x.Item.Id ?? string.Empty,
                RawStatus = x.Item.Status,
                Quantity = x.Item.Quantity,
                RfqCreateTime = x.Rfq.CreateTime,
                PurchaserId1 = x.Item.AssignedPurchaserUserId1,
                PurchaserId2 = x.Item.AssignedPurchaserUserId2
            })
            .ToListAsync(cancellationToken);

        var filteredQuoteQuery = await QuoteListFilter.BuildFilteredQuotesQueryAsync(
            _db, _dataPermission, request, cancellationToken);
        var filteredQuotes = await filteredQuoteQuery
            .Select(q => new QuoteAnalyticsRow
            {
                Id = q.Id,
                RfqItemId = q.RFQItemId,
                Status = q.Status,
                Mpn = q.Mpn,
                PurchaseUserId = q.PurchaseUserId,
                CreateTime = q.CreateTime
            })
            .ToListAsync(cancellationToken);

        if (demandRows.Count == 0 && filteredQuotes.Count == 0)
            return new QuoteAnalyticsBundle();

        var scopedRequest = CloneWithoutStatusFilter(request);
        var scopedQuoteQuery = await QuoteListFilter.BuildFilteredQuotesQueryAsync(
            _db, _dataPermission, scopedRequest, cancellationToken);
        var scopedQuotes = await scopedQuoteQuery
            .Select(q => new QuoteAnalyticsRow
            {
                Id = q.Id,
                RfqItemId = q.RFQItemId,
                Status = q.Status,
                Mpn = q.Mpn,
                PurchaseUserId = q.PurchaseUserId,
                CreateTime = q.CreateTime
            })
            .ToListAsync(cancellationToken);

        var demandItemIds = demandRows
            .Select(i => i.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToList();

        var scopedQuotedItemIds = scopedQuotes
            .Where(q => q.RfqItemId != null)
            .Select(q => q.RfqItemId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var earliestQuoteCreateByItemId = scopedQuotes
            .Where(q => q.RfqItemId != null)
            .GroupBy(q => q.RfqItemId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Min(x => x.CreateTime), StringComparer.OrdinalIgnoreCase);

        var convertedItemIds = demandItemIds.Count == 0
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
                      && demandItemIds.Contains(q.RFQItemId)
                select q.RFQItemId!
            ).Distinct().ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var filteredQuoteIds = filteredQuotes.Select(q => q.Id).ToList();
        List<QuoteItemAnalyticsRow> quoteItemRows;
        if (filteredQuoteIds.Count == 0)
        {
            quoteItemRows = new List<QuoteItemAnalyticsRow>();
        }
        else
        {
            var rfqItemByQuoteId = filteredQuotes.ToDictionary(
                q => q.Id,
                q => q.RfqItemId ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);
            quoteItemRows = await _db.QuoteItems.AsNoTracking()
                .Where(qi => !qi.IsDeleted && filteredQuoteIds.Contains(qi.QuoteId))
                .Select(qi => new QuoteItemAnalyticsRow
                {
                    QuoteId = qi.QuoteId,
                    VendorId = qi.VendorId,
                    VendorName = qi.VendorName,
                    Brand = qi.Brand,
                    LabelType = qi.LabelType,
                    WaferOrigin = qi.WaferOrigin,
                    PackageOrigin = qi.PackageOrigin,
                    FreeShipping = qi.FreeShipping,
                    CreateTime = qi.CreateTime
                })
                .ToListAsync(cancellationToken);
            foreach (var row in quoteItemRows)
                row.RfqItemId = rfqItemByQuoteId.TryGetValue(row.QuoteId, out var rid) ? rid : string.Empty;
        }

        var firstItemByQuoteId = quoteItemRows
            .GroupBy(qi => qi.QuoteId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.CreateTime).First(),
                StringComparer.OrdinalIgnoreCase);

        var purchaserIds = demandRows
            .SelectMany(i => new[] { i.PurchaserId1, i.PurchaserId2 })
            .Concat(filteredQuotes.Select(q => q.PurchaseUserId))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var userNames = purchaserIds.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : await _db.Users.AsNoTracking()
                .Where(u => purchaserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName })
                .ToDictionaryAsync(x => x.Id, x => x.UserName ?? x.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var vendorIds = quoteItemRows
            .Where(qi => !string.IsNullOrWhiteSpace(qi.VendorId))
            .Select(qi => qi.VendorId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var vendorNames = vendorIds.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : await _db.Vendors.AsNoTracking()
                .Where(v => vendorIds.Contains(v.Id))
                .Select(v => new { v.Id, Name = v.OfficialName ?? v.NickName ?? v.Code ?? v.Id })
                .ToDictionaryAsync(x => x.Id, x => x.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);

        return new QuoteAnalyticsBundle
        {
            DemandItems = demandRows,
            FilteredQuotes = filteredQuotes,
            ScopedQuotes = scopedQuotes,
            ScopedQuotedItemIds = scopedQuotedItemIds,
            EarliestQuoteCreateByItemId = earliestQuoteCreateByItemId,
            ConvertedItemIds = convertedItemIds,
            QuoteItemRows = quoteItemRows,
            FirstItemByQuoteId = firstItemByQuoteId,
            UserNames = userNames,
            VendorNames = vendorNames
        };
    }

    private static QuoteQueryRequest CloneWithoutStatusFilter(QuoteQueryRequest request) =>
        new()
        {
            Keyword = request.Keyword,
            RfqItemId = request.RfqItemId,
            CurrentUserId = request.CurrentUserId,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

    private static List<SalesAnalyticsBreakdownItemDto> BuildQuoteBreakdown(
        List<QuoteAnalyticsRow> quotes,
        Func<QuoteAnalyticsRow, string> keySelector,
        Func<QuoteAnalyticsRow, string> labelSelector)
    {
        var breakdown = quotes
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

    private static List<SalesAnalyticsBreakdownItemDto> BuildFirstItemQuoteBreakdown(
        QuoteAnalyticsBundle bundle,
        Func<QuoteItemAnalyticsRow, string> keySelector,
        Func<QuoteItemAnalyticsRow, string> labelSelector)
    {
        var rows = bundle.FilteredQuotes
            .Where(q => bundle.FirstItemByQuoteId.ContainsKey(q.Id))
            .Select(q => bundle.FirstItemByQuoteId[q.Id])
            .ToList();

        var breakdown = rows
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

    private static List<SalesAnalyticsBreakdownItemDto> BuildQuoteDistributionBreakdown(QuoteAnalyticsBundle bundle)
    {
        int hasQuote = 0, noQuote = 0, pending = 0;
        foreach (var item in bundle.DemandItems)
        {
            if (item.RawStatus == NoQuoteFoundItemStatus)
            {
                noQuote++;
                continue;
            }

            if (item.RawStatus >= 1 || bundle.ScopedQuotedItemIds.Contains(item.Id))
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

    private List<SalesAnalyticsBreakdownItemDto> BuildAssignedPurchaserBreakdown(QuoteAnalyticsBundle bundle)
    {
        var counts = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in bundle.DemandItems)
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
                    : (bundle.UserNames.TryGetValue(kv.Key, out var name) ? name : kv.Key),
                Value = kv.Value,
                Ratio = 0
            })
            .ToList();
        ApplyRatios(items);
        return items;
    }

    private List<SalesAnalyticsBreakdownItemDto> BuildQuotePurchaserBreakdown(QuoteAnalyticsBundle bundle)
    {
        var breakdown = bundle.FilteredQuotes
            .GroupBy(q => q.PurchaseUserId ?? "_unset", StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsBreakdownItemDto
            {
                Key = g.Key,
                Label = g.Key == "_unset"
                    ? "未分配采购员"
                    : (bundle.UserNames.TryGetValue(g.Key, out var name) ? name : g.Key),
                Value = g.Count(),
                Ratio = 0
            })
            .ToList();
        ApplyRatios(breakdown);
        return breakdown;
    }

    private static Dictionary<string, int> BuildAssignedDemandCounts(QuoteAnalyticsBundle bundle)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in bundle.DemandItems)
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

        return counts;
    }

    private static string ResolveVendorDisplayName(
        string vendorId,
        string? fallbackName,
        Dictionary<string, string> vendorNames)
    {
        if (vendorNames.TryGetValue(vendorId, out var name) && !string.IsNullOrWhiteSpace(name))
            return name;
        if (!string.IsNullOrWhiteSpace(fallbackName))
            return fallbackName.Trim();
        return vendorId;
    }

    private static (DateTime From, DateTime ToInclusive) ResolveQuoteTrendDateBounds(
        QuoteQueryRequest request,
        DateTime minCreateTime,
        DateTime maxCreateTime)
    {
        var from = request.StartDate?.Date ?? minCreateTime.Date;
        var to = request.EndDate?.Date ?? maxCreateTime.Date;
        if (to < from) to = from;
        return (from, to);
    }

    private static string FormatQuoteMainStatus(short status) => status switch
    {
        (short)QuoteMainStatus.New => "新建",
        (short)QuoteMainStatus.Won => "成单",
        (short)QuoteMainStatus.Closed => "关闭",
        _ => $"状态{status}"
    };

    private static string FormatLabelType(short labelType) => labelType switch
    {
        0 => "不涂标",
        1 => "涂标",
        _ => "待确定"
    };

    private static string FormatOrigin(short origin) => origin switch
    {
        0 => "美产",
        1 => "非美产",
        _ => "待确定"
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

    private sealed class QuoteAnalyticsBundle
    {
        public List<QuoteDemandAnalyticsRow> DemandItems { get; set; } = new();
        public List<QuoteAnalyticsRow> FilteredQuotes { get; set; } = new();
        public List<QuoteAnalyticsRow> ScopedQuotes { get; set; } = new();
        public HashSet<string> ScopedQuotedItemIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, DateTime> EarliestQuoteCreateByItemId { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> ConvertedItemIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<QuoteItemAnalyticsRow> QuoteItemRows { get; set; } = new();
        public Dictionary<string, QuoteItemAnalyticsRow> FirstItemByQuoteId { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> UserNames { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> VendorNames { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class QuoteDemandAnalyticsRow
    {
        public string Id { get; set; } = string.Empty;
        public short RawStatus { get; set; }
        public decimal Quantity { get; set; }
        public DateTime RfqCreateTime { get; set; }
        public string? PurchaserId1 { get; set; }
        public string? PurchaserId2 { get; set; }
    }

    private sealed class QuoteAnalyticsRow
    {
        public string Id { get; set; } = string.Empty;
        public string? RfqItemId { get; set; }
        public short Status { get; set; }
        public string? Mpn { get; set; }
        public string? PurchaseUserId { get; set; }
        public DateTime CreateTime { get; set; }
    }

    private sealed class QuoteItemAnalyticsRow
    {
        public string QuoteId { get; set; } = string.Empty;
        public string RfqItemId { get; set; } = string.Empty;
        public string? VendorId { get; set; }
        public string? VendorName { get; set; }
        public string? Brand { get; set; }
        public short LabelType { get; set; }
        public short WaferOrigin { get; set; }
        public short PackageOrigin { get; set; }
        public bool FreeShipping { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
