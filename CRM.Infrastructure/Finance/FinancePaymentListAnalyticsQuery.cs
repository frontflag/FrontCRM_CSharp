using System.Globalization;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Finance;

/// <summary>
/// 付款记录列表看板聚合（筛选与 <see cref="FinancePaymentListQuery"/> 共用）。
/// 金额为明细已付货款 <c>FinancePaymentItem.PaymentAmount</c> 合计（不含费用），按付款单头原币分面，不折美金。
/// </summary>
public sealed class FinancePaymentListAnalyticsQuery : IFinancePaymentListAnalyticsQuery
{
    private const int TopN = 10;
    private const int IdChunkSize = 800;
    private const string UnsetVendor = "未关联供应商";
    private const string UnsetPurchaseUser = "未分配采购员";
    private const string MaskedName = "—";

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public FinancePaymentListAnalyticsQuery(
        ApplicationDbContext db,
        IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    /// <inheritdoc />
    public async Task<FinancePaymentListAnalyticsDashboardDto> GetDashboardAsync(
        FinancePaymentQueryRequest query,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var headers = await LoadHeadersAsync(query, cancellationToken);
        return new FinancePaymentListAnalyticsDashboardDto
        {
            Context = new FinancePaymentListAnalyticsContextDto { MaskAmounts = maskAmounts },
            Snapshot = new FinancePaymentListAnalyticsSnapshotDto
            {
                VendorCount = CountDistinctVendors(headers),
                HeaderCount = headers.Count,
                CurrencyLines = maskAmounts
                    ? Array.Empty<FinancePaymentListAnalyticsCurrencyLineDto>()
                    : BuildCurrencyLines(headers)
            }
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FinancePaymentListAnalyticsTrendPointDto>> GetTrendsAsync(
        FinancePaymentQueryRequest query,
        string groupBy,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var headers = await LoadHeadersAsync(query, cancellationToken);
        var dated = headers.Where(h => h.BucketDate.HasValue).ToList();
        if (dated.Count == 0)
            return Array.Empty<FinancePaymentListAnalyticsTrendPointDto>();

        var dateFrom = dated.Min(h => h.BucketDate!.Value).Date;
        var dateToInclusive = dated.Max(h => h.BucketDate!.Value).Date;
        var normalizedGroupBy = NormalizeGroupBy(groupBy);
        var periods = BuildPeriodKeys(dateFrom, dateToInclusive, normalizedGroupBy);
        var currencies = dated
            .Select(h => h.Currency)
            .Distinct()
            .OrderBy(c => c)
            .Select(FormatCurrency)
            .ToList();

        var result = new List<FinancePaymentListAnalyticsTrendPointDto>(periods.Count);
        foreach (var period in periods)
        {
            var (start, end) = ParsePeriodRange(period, normalizedGroupBy);
            var inBucket = dated.Where(h => h.BucketDate!.Value >= start && h.BucketDate.Value < end).ToList();
            result.Add(new FinancePaymentListAnalyticsTrendPointDto
            {
                Period = period,
                HeaderCount = inBucket.Count,
                AmountsByCurrency = currencies.Select(ccy => new FinancePaymentListAnalyticsTrendCurrencyAmountDto
                {
                    CurrencyKey = ccy.Key,
                    CurrencyLabel = ccy.Label,
                    Amount = maskAmounts
                        ? null
                        : Math.Round(
                            inBucket.Where(h => h.Currency.ToString() == ccy.Key).Sum(h => h.GoodsAmount),
                            2,
                            MidpointRounding.AwayFromZero)
                }).ToList()
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FinancePaymentListAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        FinancePaymentQueryRequest query,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var headers = await LoadHeadersAsync(query, cancellationToken);

        var groups = new List<FinancePaymentListAnalyticsBreakdownGroupDto>
        {
            new()
            {
                GroupKey = "verificationStatus",
                GroupLabel = "核销状态",
                Items = BuildHeaderBreakdown(
                    headers,
                    h => h.VerificationStatus.ToString(),
                    h => FormatVerificationStatus(h.VerificationStatus),
                    _ => 1m)
            }
        };

        foreach (var ccy in headers.Select(h => h.Currency).Distinct().OrderBy(c => c))
        {
            var (key, label) = FormatCurrency(ccy);
            var attrs = headers
                .Where(h => h.Currency == ccy)
                .SelectMany(h => h.PurchaseUserAttributions)
                .ToList();
            groups.Add(new FinancePaymentListAnalyticsBreakdownGroupDto
            {
                GroupKey = "purchaseUser",
                GroupLabel = "采购员",
                CurrencyKey = key,
                CurrencyLabel = label,
                Items = BuildAttributionBreakdown(
                    attrs,
                    a => string.IsNullOrWhiteSpace(a.PurchaseUserId) ? "_unset" : a.PurchaseUserId,
                    a => string.IsNullOrWhiteSpace(a.PurchaseUserName) ? UnsetPurchaseUser : a.PurchaseUserName,
                    a => maskAmounts ? 1m : a.Amount)
            });
        }

        return groups;
    }

    /// <inheritdoc />
    public async Task<FinancePaymentListAnalyticsRankingsDto> GetRankingsAsync(
        FinancePaymentQueryRequest query,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var headers = await LoadHeadersAsync(query, cancellationToken);
        var currencies = headers.Select(h => h.Currency).Distinct().OrderBy(c => c).ToList();

        return new FinancePaymentListAnalyticsRankingsDto
        {
            VendorByAmount = currencies.Select(ccy =>
            {
                var (key, label) = FormatCurrency(ccy);
                return new FinancePaymentListAnalyticsRankingFacetDto
                {
                    CurrencyKey = key,
                    CurrencyLabel = label,
                    Rows = RankHeadersBy(
                        headers.Where(h => h.Currency == ccy),
                        h => string.IsNullOrWhiteSpace(h.VendorId) ? "_unset" : h.VendorId!,
                        h => maskAmounts
                            ? MaskedName
                            : (string.IsNullOrWhiteSpace(h.VendorId)
                                ? UnsetVendor
                                : (string.IsNullOrWhiteSpace(h.VendorName) ? h.VendorId! : h.VendorName!)),
                        maskAmounts)
                };
            }).ToList(),
            PurchaseUserByAmount = currencies.Select(ccy =>
            {
                var (key, label) = FormatCurrency(ccy);
                var attrs = headers
                    .Where(h => h.Currency == ccy)
                    .SelectMany(h => h.PurchaseUserAttributions)
                    .ToList();
                return new FinancePaymentListAnalyticsRankingFacetDto
                {
                    CurrencyKey = key,
                    CurrencyLabel = label,
                    Rows = RankAttributionsBy(
                        attrs,
                        a => string.IsNullOrWhiteSpace(a.PurchaseUserId) ? "_unset" : a.PurchaseUserId,
                        a => string.IsNullOrWhiteSpace(a.PurchaseUserName) ? UnsetPurchaseUser : a.PurchaseUserName,
                        maskAmounts)
                };
            }).ToList()
        };
    }

    private async Task<List<HeaderRow>> LoadHeadersAsync(
        FinancePaymentQueryRequest query,
        CancellationToken cancellationToken)
    {
        var filtered = await FinancePaymentListFilter.BuildFilteredQueryAsync(
            _db, _dataPermission, query, cancellationToken);

        var snaps = await filtered
            .Select(p => new HeaderSnap
            {
                Id = p.Id,
                VendorId = p.VendorId,
                VendorName = p.VendorName,
                PaymentCurrency = p.PaymentCurrency,
                PaymentDate = p.PaymentDate
            })
            .ToListAsync(cancellationToken);

        if (snaps.Count == 0)
            return new List<HeaderRow>();

        var ids = snaps.Select(s => s.Id).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var items = await LoadItemsAsync(ids, cancellationToken);
        var poById = await LoadPurchaseOrdersAsync(items, cancellationToken);
        var userIds = poById.Values
            .Select(po => po.PurchaseUserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var userNames = await LoadUsersAsync(userIds, cancellationToken);

        var itemsByPayment = items
            .GroupBy(i => i.FinancePaymentId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var rows = new List<HeaderRow>(snaps.Count);
        foreach (var s in snaps)
        {
            itemsByPayment.TryGetValue(s.Id, out var paymentItems);
            paymentItems ??= new List<ItemSnap>();

            var goodsAmount = paymentItems.Sum(i => i.PaymentAmount);
            short verification;
            if (paymentItems.Count == 0)
            {
                verification = FinanceVerificationStatusCode.Pending;
            }
            else
            {
                verification = FinancePaymentHeaderVerification.Resolve(
                    paymentItems.Min(i => i.VerificationStatus),
                    paymentItems.Max(i => i.VerificationStatus));
            }

            var attributions = BuildPurchaseUserAttributions(paymentItems, poById, userNames);
            rows.Add(new HeaderRow
            {
                VendorId = s.VendorId,
                VendorName = s.VendorName,
                GoodsAmount = goodsAmount,
                Currency = s.PaymentCurrency > 0 ? s.PaymentCurrency : (byte)CurrencyCode.RMB,
                VerificationStatus = verification,
                BucketDate = ToBucketDate(s.PaymentDate),
                PurchaseUserAttributions = attributions
            });
        }

        return rows;
    }

    private static List<PurchaseUserAttribution> BuildPurchaseUserAttributions(
        List<ItemSnap> paymentItems,
        Dictionary<string, PurchaseOrderSnap> poById,
        Dictionary<string, string> userNames)
    {
        if (paymentItems.Count == 0)
        {
            return new List<PurchaseUserAttribution>
            {
                new() { PurchaseUserId = null, PurchaseUserName = UnsetPurchaseUser, Amount = 0m }
            };
        }

        var buckets = new Dictionary<string, PurchaseUserAttribution>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in paymentItems)
        {
            string? userId = null;
            string? userName = null;
            if (!string.IsNullOrWhiteSpace(item.ResolvedPurchaseOrderId)
                && poById.TryGetValue(item.ResolvedPurchaseOrderId, out var po))
            {
                userId = string.IsNullOrWhiteSpace(po.PurchaseUserId) ? null : po.PurchaseUserId.Trim();
                if (userId != null && userNames.TryGetValue(userId, out var fromUser))
                    userName = fromUser;
                if (string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(po.PurchaseUserName))
                    userName = po.PurchaseUserName.Trim();
            }

            var key = userId ?? "_unset";
            if (!buckets.TryGetValue(key, out var bucket))
            {
                bucket = new PurchaseUserAttribution
                {
                    PurchaseUserId = userId,
                    PurchaseUserName = string.IsNullOrWhiteSpace(userName) ? UnsetPurchaseUser : userName,
                    Amount = 0m
                };
                buckets[key] = bucket;
            }

            bucket.Amount += item.PaymentAmount;
        }

        return buckets.Values.ToList();
    }

    private async Task<List<ItemSnap>> LoadItemsAsync(List<string> paymentIds, CancellationToken cancellationToken)
    {
        var result = new List<ItemSnap>();
        if (paymentIds.Count == 0) return result;

        foreach (var chunk in Chunk(paymentIds, IdChunkSize))
        {
            var rows = await _db.FinancePaymentItems.AsNoTracking()
                .Where(i => chunk.Contains(i.FinancePaymentId))
                .Select(i => new ItemSnap
                {
                    FinancePaymentId = i.FinancePaymentId,
                    PurchaseOrderId = i.PurchaseOrderId,
                    PurchaseOrderItemId = i.PurchaseOrderItemId,
                    PaymentAmount = i.PaymentAmount,
                    VerificationStatus = i.VerificationStatus
                })
                .ToListAsync(cancellationToken);
            result.AddRange(rows);
        }

        var poiIds = result
            .Where(i => string.IsNullOrWhiteSpace(i.PurchaseOrderId) && !string.IsNullOrWhiteSpace(i.PurchaseOrderItemId))
            .Select(i => i.PurchaseOrderItemId!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var poiToPo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in Chunk(poiIds, IdChunkSize))
        {
            var rows = await _db.PurchaseOrderItems.AsNoTracking()
                .Where(poi => chunk.Contains(poi.Id))
                .Select(poi => new { poi.Id, poi.PurchaseOrderId })
                .ToListAsync(cancellationToken);
            foreach (var row in rows)
            {
                if (!string.IsNullOrWhiteSpace(row.Id) && !string.IsNullOrWhiteSpace(row.PurchaseOrderId))
                    poiToPo[row.Id] = row.PurchaseOrderId.Trim();
            }
        }

        foreach (var item in result)
        {
            if (!string.IsNullOrWhiteSpace(item.PurchaseOrderId))
            {
                item.ResolvedPurchaseOrderId = item.PurchaseOrderId.Trim();
                continue;
            }

            if (!string.IsNullOrWhiteSpace(item.PurchaseOrderItemId)
                && poiToPo.TryGetValue(item.PurchaseOrderItemId.Trim(), out var poId))
                item.ResolvedPurchaseOrderId = poId;
        }

        return result;
    }

    private async Task<Dictionary<string, PurchaseOrderSnap>> LoadPurchaseOrdersAsync(
        List<ItemSnap> items,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, PurchaseOrderSnap>(StringComparer.OrdinalIgnoreCase);
        var poIds = items
            .Select(i => i.ResolvedPurchaseOrderId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (poIds.Count == 0) return map;

        foreach (var chunk in Chunk(poIds, IdChunkSize))
        {
            var rows = await _db.PurchaseOrders.AsNoTracking()
                .Where(po => chunk.Contains(po.Id))
                .Select(po => new PurchaseOrderSnap
                {
                    Id = po.Id,
                    PurchaseUserId = po.PurchaseUserId,
                    PurchaseUserName = po.PurchaseUserName
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

    private static IReadOnlyList<FinancePaymentListAnalyticsCurrencyLineDto> BuildCurrencyLines(List<HeaderRow> headers)
    {
        return headers
            .GroupBy(h => h.Currency)
            .Select(g =>
            {
                var (key, label) = FormatCurrency(g.Key);
                return new FinancePaymentListAnalyticsCurrencyLineDto
                {
                    CurrencyKey = key,
                    CurrencyLabel = label,
                    OriginalAmount = Math.Round(g.Sum(x => x.GoodsAmount), 2, MidpointRounding.AwayFromZero)
                };
            })
            .OrderBy(x => x.CurrencyKey)
            .ToList();
    }

    private static List<SalesAnalyticsBreakdownItemDto> BuildHeaderBreakdown(
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

    private static List<SalesAnalyticsBreakdownItemDto> BuildAttributionBreakdown(
        List<PurchaseUserAttribution> rows,
        Func<PurchaseUserAttribution, string> keySelector,
        Func<PurchaseUserAttribution, string> labelSelector,
        Func<PurchaseUserAttribution, decimal> valueSelector)
    {
        var items = rows
            .GroupBy(keySelector, StringComparer.OrdinalIgnoreCase)
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

    private static List<SalesAnalyticsRankingRowDto> RankHeadersBy(
        IEnumerable<HeaderRow> headers,
        Func<HeaderRow, string> keySelector,
        Func<HeaderRow, string> nameSelector,
        bool maskAmounts)
    {
        return headers
            .GroupBy(keySelector, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = nameSelector(g.First()),
                Amount = maskAmounts
                    ? null
                    : Math.Round(g.Sum(x => x.GoodsAmount), 2, MidpointRounding.AwayFromZero),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.Amount ?? x.OrderCount)
            .Take(TopN)
            .ToList();
    }

    private static List<SalesAnalyticsRankingRowDto> RankAttributionsBy(
        IEnumerable<PurchaseUserAttribution> rows,
        Func<PurchaseUserAttribution, string> keySelector,
        Func<PurchaseUserAttribution, string> nameSelector,
        bool maskAmounts)
    {
        return rows
            .GroupBy(keySelector, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = nameSelector(g.First()),
                Amount = maskAmounts
                    ? null
                    : Math.Round(g.Sum(x => x.Amount), 2, MidpointRounding.AwayFromZero),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.Amount ?? x.OrderCount)
            .Take(TopN)
            .ToList();
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

    private static (string Key, string Label) FormatCurrency(byte currency)
    {
        var code = (CurrencyCode)currency;
        return (currency.ToString(), Enum.IsDefined(typeof(CurrencyCode), code) ? code.ToIsoText() : currency.ToString());
    }

    private static string FormatVerificationStatus(short status) =>
        status switch
        {
            FinanceVerificationStatusCode.Complete => "核销完成",
            FinanceVerificationStatusCode.Partial => "部分核销",
            _ => "未核销"
        };

    private static DateTime? ToBucketDate(DateTime? paymentDate)
    {
        if (!paymentDate.HasValue) return null;
        var d = paymentDate.Value;
        return d.Year < 2000 ? null : d.Date;
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

    private sealed class HeaderSnap
    {
        public string Id { get; set; } = string.Empty;
        public string? VendorId { get; set; }
        public string? VendorName { get; set; }
        public byte PaymentCurrency { get; set; }
        public DateTime? PaymentDate { get; set; }
    }

    private sealed class ItemSnap
    {
        public string FinancePaymentId { get; set; } = string.Empty;
        public string? PurchaseOrderId { get; set; }
        public string? PurchaseOrderItemId { get; set; }
        public string? ResolvedPurchaseOrderId { get; set; }
        public decimal PaymentAmount { get; set; }
        public short VerificationStatus { get; set; }
    }

    private sealed class PurchaseOrderSnap
    {
        public string Id { get; set; } = string.Empty;
        public string? PurchaseUserId { get; set; }
        public string? PurchaseUserName { get; set; }
    }

    private sealed class PurchaseUserAttribution
    {
        public string? PurchaseUserId { get; set; }
        public string PurchaseUserName { get; set; } = UnsetPurchaseUser;
        public decimal Amount { get; set; }
    }

    private sealed class HeaderRow
    {
        public string? VendorId { get; set; }
        public string? VendorName { get; set; }
        public decimal GoodsAmount { get; set; }
        public byte Currency { get; set; }
        public short VerificationStatus { get; set; }
        public DateTime? BucketDate { get; set; }
        public List<PurchaseUserAttribution> PurchaseUserAttributions { get; set; } = new();
    }
}
