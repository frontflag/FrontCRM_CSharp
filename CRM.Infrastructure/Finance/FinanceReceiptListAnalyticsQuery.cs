using System.Globalization;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Finance;

/// <summary>收款记录列表看板聚合（筛选与 <see cref="FinanceReceiptListQuery"/> 共用；金额为单头原币，不折美金）。</summary>
public sealed class FinanceReceiptListAnalyticsQuery : IFinanceReceiptListAnalyticsQuery
{
    private const int TopN = 10;
    private const int IdChunkSize = 800;
    private const string UnsetCustomer = "未关联客户";
    private const string UnsetSalesUser = "未分配业务员";
    private const string MaskedName = "—";

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public FinanceReceiptListAnalyticsQuery(
        ApplicationDbContext db,
        IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    /// <inheritdoc />
    public async Task<FinanceReceiptListAnalyticsDashboardDto> GetDashboardAsync(
        FinanceReceiptQueryRequest query,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var headers = await LoadHeadersAsync(query, cancellationToken);
        return new FinanceReceiptListAnalyticsDashboardDto
        {
            Context = new FinanceReceiptListAnalyticsContextDto { MaskAmounts = maskAmounts },
            Snapshot = new FinanceReceiptListAnalyticsSnapshotDto
            {
                CustomerCount = CountDistinctCustomers(headers),
                HeaderCount = headers.Count,
                CurrencyLines = maskAmounts
                    ? Array.Empty<FinanceReceiptListAnalyticsCurrencyLineDto>()
                    : BuildCurrencyLines(headers)
            }
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FinanceReceiptListAnalyticsTrendPointDto>> GetTrendsAsync(
        FinanceReceiptQueryRequest query,
        string groupBy,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var headers = await LoadHeadersAsync(query, cancellationToken);
        var dated = headers.Where(h => h.BucketDate.HasValue).ToList();
        if (dated.Count == 0)
            return Array.Empty<FinanceReceiptListAnalyticsTrendPointDto>();

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

        var result = new List<FinanceReceiptListAnalyticsTrendPointDto>(periods.Count);
        foreach (var period in periods)
        {
            var (start, end) = ParsePeriodRange(period, normalizedGroupBy);
            var inBucket = dated.Where(h => h.BucketDate!.Value >= start && h.BucketDate.Value < end).ToList();
            result.Add(new FinanceReceiptListAnalyticsTrendPointDto
            {
                Period = period,
                HeaderCount = inBucket.Count,
                AmountsByCurrency = currencies.Select(ccy => new FinanceReceiptListAnalyticsTrendCurrencyAmountDto
                {
                    CurrencyKey = ccy.Key,
                    CurrencyLabel = ccy.Label,
                    Amount = maskAmounts
                        ? null
                        : Math.Round(
                            inBucket.Where(h => h.Currency.ToString() == ccy.Key).Sum(h => h.Amount),
                            2,
                            MidpointRounding.AwayFromZero)
                }).ToList()
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FinanceReceiptListAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        FinanceReceiptQueryRequest query,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var headers = await LoadHeadersAsync(query, cancellationToken);

        var groups = new List<FinanceReceiptListAnalyticsBreakdownGroupDto>
        {
            new()
            {
                GroupKey = "verificationStatus",
                GroupLabel = "核销状态",
                Items = BuildBreakdown(
                    headers,
                    h => h.VerificationStatus.ToString(),
                    h => FormatVerificationStatus(h.VerificationStatus),
                    _ => 1m)
            }
        };

        foreach (var ccy in headers.Select(h => h.Currency).Distinct().OrderBy(c => c))
        {
            var (key, label) = FormatCurrency(ccy);
            var inCcy = headers.Where(h => h.Currency == ccy).ToList();
            groups.Add(new FinanceReceiptListAnalyticsBreakdownGroupDto
            {
                GroupKey = "salesUser",
                GroupLabel = "业务员",
                CurrencyKey = key,
                CurrencyLabel = label,
                Items = BuildBreakdown(
                    inCcy,
                    h => string.IsNullOrWhiteSpace(h.SalesUserId) ? "_unset" : h.SalesUserId!,
                    h => string.IsNullOrWhiteSpace(h.SalesUserName) ? UnsetSalesUser : h.SalesUserName!,
                    h => maskAmounts ? 1m : h.Amount)
            });
        }

        return groups;
    }

    /// <inheritdoc />
    public async Task<FinanceReceiptListAnalyticsRankingsDto> GetRankingsAsync(
        FinanceReceiptQueryRequest query,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var headers = await LoadHeadersAsync(query, cancellationToken);
        var currencies = headers.Select(h => h.Currency).Distinct().OrderBy(c => c).ToList();

        return new FinanceReceiptListAnalyticsRankingsDto
        {
            CustomerByAmount = currencies.Select(ccy =>
            {
                var (key, label) = FormatCurrency(ccy);
                return new FinanceReceiptListAnalyticsRankingFacetDto
                {
                    CurrencyKey = key,
                    CurrencyLabel = label,
                    Rows = RankBy(
                        headers.Where(h => h.Currency == ccy),
                        h => string.IsNullOrWhiteSpace(h.CustomerId) ? "_unset" : h.CustomerId!,
                        h => maskAmounts
                            ? MaskedName
                            : (string.IsNullOrWhiteSpace(h.CustomerId)
                                ? UnsetCustomer
                                : (string.IsNullOrWhiteSpace(h.CustomerName) ? h.CustomerId! : h.CustomerName!)),
                        maskAmounts)
                };
            }).ToList(),
            SalesUserByAmount = currencies.Select(ccy =>
            {
                var (key, label) = FormatCurrency(ccy);
                return new FinanceReceiptListAnalyticsRankingFacetDto
                {
                    CurrencyKey = key,
                    CurrencyLabel = label,
                    Rows = RankBy(
                        headers.Where(h => h.Currency == ccy),
                        h => string.IsNullOrWhiteSpace(h.SalesUserId) ? "_unset" : h.SalesUserId!,
                        h => string.IsNullOrWhiteSpace(h.SalesUserName) ? UnsetSalesUser : h.SalesUserName!,
                        maskAmounts)
                };
            }).ToList()
        };
    }

    private async Task<List<HeaderRow>> LoadHeadersAsync(
        FinanceReceiptQueryRequest query,
        CancellationToken cancellationToken)
    {
        var filtered = await FinanceReceiptListFilter.BuildFilteredQueryAsync(
            _db, _dataPermission, query, cancellationToken);

        var snaps = await filtered
            .Select(r => new HeaderSnap
            {
                Id = r.Id,
                CustomerId = r.CustomerId,
                CustomerName = r.CustomerName,
                SalesUserId = r.SalesUserId,
                ReceiptAmount = r.ReceiptAmount,
                ReceiptCurrency = r.ReceiptCurrency,
                ReceiptDate = r.ReceiptDate
            })
            .ToListAsync(cancellationToken);

        if (snaps.Count == 0)
            return new List<HeaderRow>();

        var ids = snaps.Select(s => s.Id).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var verifyMap = await LoadVerificationAsync(ids, cancellationToken);
        var salesIds = snaps
            .Select(s => s.SalesUserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var userNames = await LoadUsersAsync(salesIds, cancellationToken);

        var rows = new List<HeaderRow>(snaps.Count);
        foreach (var s in snaps)
        {
            var salesId = string.IsNullOrWhiteSpace(s.SalesUserId) ? null : s.SalesUserId.Trim();
            string? salesName = null;
            if (salesId != null)
                userNames.TryGetValue(salesId, out salesName);

            rows.Add(new HeaderRow
            {
                CustomerId = s.CustomerId,
                CustomerName = s.CustomerName,
                SalesUserId = salesId,
                SalesUserName = salesName,
                Amount = s.ReceiptAmount,
                Currency = s.ReceiptCurrency > 0 ? s.ReceiptCurrency : (byte)CurrencyCode.RMB,
                VerificationStatus = verifyMap.TryGetValue(s.Id, out var vs)
                    ? vs
                    : FinanceVerificationStatusCode.Pending,
                BucketDate = ToBucketDate(s.ReceiptDate)
            });
        }

        return rows;
    }

    private async Task<Dictionary<string, short>> LoadVerificationAsync(
        List<string> receiptIds,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, short>(StringComparer.OrdinalIgnoreCase);
        if (receiptIds.Count == 0) return map;

        foreach (var chunk in Chunk(receiptIds, IdChunkSize))
        {
            var rows = await _db.FinanceReceiptItems.AsNoTracking()
                .Where(i => chunk.Contains(i.FinanceReceiptId))
                .GroupBy(i => i.FinanceReceiptId)
                .Select(g => new
                {
                    ReceiptId = g.Key,
                    MinStatus = g.Min(i => i.VerificationStatus),
                    MaxStatus = g.Max(i => i.VerificationStatus)
                })
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.ReceiptId)) continue;
                map[row.ReceiptId] = FinanceReceiptListFilter.ResolveHeaderVerificationStatus(
                    row.MinStatus, row.MaxStatus);
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

    private static IReadOnlyList<FinanceReceiptListAnalyticsCurrencyLineDto> BuildCurrencyLines(List<HeaderRow> headers)
    {
        return headers
            .GroupBy(h => h.Currency)
            .Select(g =>
            {
                var (key, label) = FormatCurrency(g.Key);
                return new FinanceReceiptListAnalyticsCurrencyLineDto
                {
                    CurrencyKey = key,
                    CurrencyLabel = label,
                    OriginalAmount = Math.Round(g.Sum(x => x.Amount), 2, MidpointRounding.AwayFromZero)
                };
            })
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

    private static List<SalesAnalyticsRankingRowDto> RankBy(
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

    private static int CountDistinctCustomers(List<HeaderRow> headers) =>
        headers
            .Where(h => !string.IsNullOrWhiteSpace(h.CustomerId))
            .Select(h => h.CustomerId!)
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

    private static DateTime? ToBucketDate(DateTime? receiptDate)
    {
        if (!receiptDate.HasValue) return null;
        var d = receiptDate.Value;
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
        public string CustomerId { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? SalesUserId { get; set; }
        public decimal ReceiptAmount { get; set; }
        public byte ReceiptCurrency { get; set; }
        public DateTime? ReceiptDate { get; set; }
    }

    private sealed class HeaderRow
    {
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? SalesUserId { get; set; }
        public string? SalesUserName { get; set; }
        public decimal Amount { get; set; }
        public byte Currency { get; set; }
        public short VerificationStatus { get; set; }
        public DateTime? BucketDate { get; set; }
    }
}
