using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Purchase;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.PurchaseOrders;

/// <summary>采购订单列表：EF 数据库分页（与内存全表方案行为对齐）。</summary>
public sealed class PurchaseOrderListQuery : IPurchaseOrderListQuery
{
    /// <summary>成单口径：审核通过及以上。</summary>
    private const short ApprovedStatusThreshold = 10;

    /// <summary>单页上限；与采购订单明细页批量拉主单（pageSize=2000）对齐，普通列表 UI 仍建议使用较小分页。</summary>
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public PurchaseOrderListQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    /// <inheritdoc />
    public async Task<PagedResult<PurchaseOrder>> GetPagedAsync(
        PurchaseOrderQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var filtered = await BuildFilteredQueryAsync(request, cancellationToken);
        var total = await filtered.CountAsync(cancellationToken);
        var items = await filtered
            .OrderByDescending(o => o.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PurchaseOrder>
        {
            Items = items,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderListAggregates> GetAggregatesAsync(
        PurchaseOrderQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var q = await BuildFilteredQueryAsync(request, cancellationToken);
        return new PurchaseOrderListAggregates
        {
            TotalCount = await q.CountAsync(cancellationToken),
            PendingConfirmCount = await q.CountAsync(o => o.Status == 20, cancellationToken),
            InProgressCount = await q.CountAsync(o => o.Status == 50, cancellationToken),
            TotalAmountSum = await q.SumAsync(o => (decimal?)o.ConvertTotal, cancellationToken) ?? 0m
        };
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderListAnalyticsDashboardDto> GetListAnalyticsDashboardAsync(
        PurchaseOrderQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var approved = await BuildApprovedOrderQueryAsync(request, cancellationToken);
        var rows = await approved
            .Select(o => new OrderAnalyticsRow
            {
                VendorId = o.VendorId,
                ConvertTotal = o.ConvertTotal,
                Total = o.Total,
                Currency = o.Currency
            })
            .ToListAsync(cancellationToken);

        var vendorGroups = rows
            .Where(o => !string.IsNullOrWhiteSpace(o.VendorId))
            .GroupBy(o => o.VendorId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Count())
            .ToList();

        var repeatVendors = vendorGroups.Count(c => c >= 2);
        var repeatOrders = vendorGroups.Sum(c => Math.Max(0, c - 1));
        var approvedAmount = rows.Sum(o => o.ConvertTotal);

        var currencyLines = rows
            .GroupBy(o => o.Currency)
            .Select(g => new PurchaseOrderListAnalyticsCurrencyLineDto
            {
                CurrencyKey = g.Key.ToString(),
                CurrencyLabel = ((CurrencyCode)g.Key).ToIsoText(),
                OriginalAmount = maskAmounts ? null : g.Sum(x => x.Total),
                UsdAmount = maskAmounts ? null : g.Sum(x => x.ConvertTotal)
            })
            .OrderByDescending(x => x.UsdAmount ?? x.OriginalAmount ?? 0m)
            .ToList();

        return new PurchaseOrderListAnalyticsDashboardDto
        {
            Context = new PurchaseOrderListAnalyticsContextDto { MaskAmounts = maskAmounts },
            Snapshot = new PurchaseOrderListAnalyticsSnapshotDto
            {
                ApprovedVendorCount = vendorGroups.Count,
                RepeatVendorCount = repeatVendors,
                ApprovedOrderCount = rows.Count,
                RepeatOrderCount = repeatOrders,
                ApprovedAmountUsd = maskAmounts ? null : approvedAmount,
                CurrencyLines = currencyLines
            }
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PurchaseOrderListAnalyticsTrendPointDto>> GetListAnalyticsTrendsAsync(
        PurchaseOrderQueryRequest request,
        string groupBy,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var approved = await BuildApprovedOrderQueryAsync(request, cancellationToken);
        var orderRows = await approved
            .Select(o => new { o.CreateTime, o.ConvertTotal })
            .ToListAsync(cancellationToken);

        if (orderRows.Count == 0)
            return Array.Empty<PurchaseOrderListAnalyticsTrendPointDto>();

        var (dateFrom, dateToInclusive) = ResolveTrendDateBounds(
            request,
            orderRows.Min(r => r.CreateTime),
            orderRows.Max(r => r.CreateTime));
        var normalizedGroupBy = NormalizeGroupBy(groupBy);
        var periods = BuildPeriodKeys(dateFrom, dateToInclusive, normalizedGroupBy);
        var result = new List<PurchaseOrderListAnalyticsTrendPointDto>();

        foreach (var period in periods)
        {
            var (start, end) = ParsePeriodRange(period, normalizedGroupBy);
            var inBucket = orderRows.Where(r => r.CreateTime >= start && r.CreateTime < end).ToList();
            result.Add(new PurchaseOrderListAnalyticsTrendPointDto
            {
                Period = period,
                ApprovedOrderCount = inBucket.Count,
                ApprovedAmountUsd = maskAmounts ? null : inBucket.Sum(r => r.ConvertTotal)
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetListAnalyticsBreakdownsAsync(
        PurchaseOrderQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var filtered = await BuildFilteredQueryAsync(request, cancellationToken);
        var approved = filtered.Where(o => o.Status >= ApprovedStatusThreshold);

        var statusRows = await filtered
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Amount = g.Sum(x => x.ConvertTotal) })
            .ToListAsync(cancellationToken);

        var statusItems = statusRows.Select(r => new SalesAnalyticsBreakdownItemDto
        {
            Key = r.Status.ToString(),
            Label = FormatOrderStatus(r.Status),
            Value = maskAmounts ? r.Count : r.Amount,
            Ratio = 0
        }).ToList();
        ApplyRatios(statusItems);

        var currencyRows = await approved
            .GroupBy(o => o.Currency)
            .Select(g => new { Currency = g.Key, Amount = g.Sum(x => x.ConvertTotal), Count = g.Count() })
            .ToListAsync(cancellationToken);

        var currencyItems = currencyRows.Select(r => new SalesAnalyticsBreakdownItemDto
        {
            Key = r.Currency.ToString(),
            Label = ((CurrencyCode)r.Currency).ToIsoText(),
            Value = maskAmounts ? r.Count : r.Amount,
            Ratio = 0
        }).ToList();
        ApplyRatios(currencyItems);

        var vendorDimRows = await (
            from o in approved
            join v in _db.Vendors.AsNoTracking() on o.VendorId equals v.Id into vj
            from v in vj.DefaultIfEmpty()
            select new
            {
                o.ConvertTotal,
                VendorIdentity = v != null ? v.Credit : (short?)null,
                VendorLevel = v != null ? v.Level : (short?)null,
                Industry = v != null ? v.Industry : null
            }
        ).ToListAsync(cancellationToken);

        var identityItems = BuildVendorDimensionBreakdown(
            vendorDimRows,
            r => r.VendorIdentity?.ToString() ?? "_unset",
            r => r.VendorIdentity.HasValue ? $"身份 {r.VendorIdentity}" : "未设置",
            r => maskAmounts ? 1m : r.ConvertTotal,
            maskAmounts);
        var levelItems = BuildVendorDimensionBreakdown(
            vendorDimRows,
            r => r.VendorLevel?.ToString() ?? "_unset",
            r => r.VendorLevel.HasValue ? $"等级 {r.VendorLevel}" : "未设置",
            r => maskAmounts ? 1m : r.ConvertTotal,
            maskAmounts);
        var industryItems = BuildVendorDimensionBreakdown(
            vendorDimRows,
            r => string.IsNullOrWhiteSpace(r.Industry) ? "_unset" : r.Industry!.Trim(),
            r => string.IsNullOrWhiteSpace(r.Industry) ? "未设置" : r.Industry!.Trim(),
            r => maskAmounts ? 1m : r.ConvertTotal,
            maskAmounts);

        var purchaseUserRows = await approved
            .GroupBy(o => new { o.PurchaseUserId, o.PurchaseUserName })
            .Select(g => new
            {
                g.Key.PurchaseUserId,
                Name = g.Key.PurchaseUserName,
                Amount = g.Sum(x => x.ConvertTotal),
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        var purchaseUserItems = purchaseUserRows.Select(r => new SalesAnalyticsBreakdownItemDto
        {
            Key = r.PurchaseUserId ?? "_unset",
            Label = string.IsNullOrWhiteSpace(r.Name) ? "未分配采购员" : r.Name!,
            Value = maskAmounts ? r.Count : r.Amount,
            Ratio = 0
        }).ToList();
        ApplyRatios(purchaseUserItems);

        return new List<SalesAnalyticsBreakdownGroupDto>
        {
            new() { GroupKey = "orderStatus", GroupLabel = "订单主状态", Items = statusItems },
            new() { GroupKey = "currency", GroupLabel = "币别金额（成单）", Items = currencyItems },
            new() { GroupKey = "vendorIdentity", GroupLabel = "供应商身份（成单 USD）", Items = identityItems },
            new() { GroupKey = "vendorLevel", GroupLabel = "供应商等级（成单 USD）", Items = levelItems },
            new() { GroupKey = "vendorIndustry", GroupLabel = "供应商行业（成单 USD）", Items = industryItems },
            new() { GroupKey = "purchaseUser", GroupLabel = "采购员（成单 USD）", Items = purchaseUserItems }
        };
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderListAnalyticsRankingsDto> GetListAnalyticsRankingsAsync(
        PurchaseOrderQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        const int topN = 10;
        var approved = await BuildApprovedOrderQueryAsync(request, cancellationToken);
        var orderRows = await approved
            .Select(o => new
            {
                o.VendorId,
                o.VendorName,
                o.PurchaseUserId,
                o.PurchaseUserName,
                o.ConvertTotal
            })
            .ToListAsync(cancellationToken);

        var vendorByAmount = orderRows
            .Where(o => !string.IsNullOrWhiteSpace(o.VendorId))
            .GroupBy(o => o.VendorId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.First().VendorName ?? g.Key,
                Amount = maskAmounts ? null : g.Sum(x => x.ConvertTotal),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.Amount ?? x.OrderCount)
            .Take(topN)
            .ToList();

        var vendorByOrderCount = orderRows
            .Where(o => !string.IsNullOrWhiteSpace(o.VendorId))
            .GroupBy(o => o.VendorId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.First().VendorName ?? g.Key,
                Amount = maskAmounts ? null : g.Sum(x => x.ConvertTotal),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.OrderCount)
            .ThenByDescending(x => x.Amount ?? 0m)
            .Take(topN)
            .ToList();

        var vendorByRepeat = orderRows
            .Where(o => !string.IsNullOrWhiteSpace(o.VendorId))
            .GroupBy(o => o.VendorId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.First().VendorName ?? g.Key,
                Amount = maskAmounts ? null : g.Sum(x => x.ConvertTotal),
                OrderCount = Math.Max(0, g.Count() - 1)
            })
            .Where(x => x.OrderCount > 0)
            .OrderByDescending(x => x.OrderCount)
            .ThenByDescending(x => x.Amount ?? 0m)
            .Take(topN)
            .ToList();

        var purchaseUserByAmount = orderRows
            .GroupBy(o => o.PurchaseUserId ?? "_unset", StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.Key == "_unset"
                    ? "未分配采购员"
                    : (g.First().PurchaseUserName ?? g.Key),
                Amount = maskAmounts ? null : g.Sum(x => x.ConvertTotal),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.Amount ?? x.OrderCount)
            .Take(topN)
            .ToList();

        return new PurchaseOrderListAnalyticsRankingsDto
        {
            VendorByAmount = vendorByAmount,
            VendorByOrderCount = vendorByOrderCount,
            VendorByRepeatOrderCount = vendorByRepeat,
            PurchaseUserByAmount = purchaseUserByAmount
        };
    }

    private async Task<IQueryable<PurchaseOrder>> BuildFilteredQueryAsync(
        PurchaseOrderQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = _db.PurchaseOrders.AsNoTracking();
        q = await _dataPermission.ApplyPurchaseOrderDataScopeAsync(request.CurrentUserId, q, cancellationToken);

        var hasSplitFilters = !string.IsNullOrWhiteSpace(request.PurchaseOrderCodeFilter)
            || !string.IsNullOrWhiteSpace(request.VendorNameFilter)
            || !string.IsNullOrWhiteSpace(request.FreightForwarderOrderNoFilter);

        if (!string.IsNullOrWhiteSpace(request.Keyword) && !hasSplitFilters)
        {
            var k = request.Keyword.Trim();
            q = q.Where(o =>
                (o.PurchaseOrderCode != null && o.PurchaseOrderCode.ToLower().Contains(k.ToLower())) ||
                (o.VendorName != null && o.VendorName.ToLower().Contains(k.ToLower())));
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(request.PurchaseOrderCodeFilter))
            {
                var c = request.PurchaseOrderCodeFilter.Trim();
                q = q.Where(o => o.PurchaseOrderCode != null && o.PurchaseOrderCode.ToLower().Contains(c.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(request.VendorNameFilter))
            {
                var v = request.VendorNameFilter.Trim();
                q = q.Where(o => o.VendorName != null && o.VendorName.ToLower().Contains(v.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(request.FreightForwarderOrderNoFilter))
            {
                var f = request.FreightForwarderOrderNoFilter.Trim();
                q = q.Where(o =>
                    o.FreightForwarderOrderNo != null &&
                    o.FreightForwarderOrderNo.ToLower().Contains(f.ToLower()));
            }
        }

        if (request.Status.HasValue)
            q = q.Where(o => o.Status == request.Status.Value);

        if (request.OrderType.HasValue)
            q = q.Where(o => o.Type == request.OrderType.Value);

        if (request.StartDate.HasValue)
        {
            var from = PurchaseAnalyticsDateFilter.ToUtcDateStart(request.StartDate.Value);
            q = q.Where(o => o.CreateTime >= from);
        }

        if (request.EndDate.HasValue)
        {
            var endExclusive = PurchaseAnalyticsDateFilter.ToUtcDateEndExclusive(request.EndDate.Value);
            q = q.Where(o => o.CreateTime < endExclusive);
        }

        if (!string.IsNullOrWhiteSpace(request.PurchaseUserNameFilter))
        {
            var p = request.PurchaseUserNameFilter.Trim();
            q = q.Where(o =>
                o.PurchaseUserName != null &&
                o.PurchaseUserName.ToLower().Contains(p.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.CommentFilter))
        {
            var c = request.CommentFilter.Trim();
            q = q.Where(o =>
                o.Comment != null &&
                o.Comment.ToLower().Contains(c.ToLower()));
        }

        return q;
    }

    private async Task<IQueryable<PurchaseOrder>> BuildApprovedOrderQueryAsync(
        PurchaseOrderQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = await BuildFilteredQueryAsync(request, cancellationToken);
        return q.Where(o => o.Status >= ApprovedStatusThreshold);
    }

    private static (DateTime From, DateTime ToInclusive) ResolveTrendDateBounds(
        PurchaseOrderQueryRequest request,
        DateTime minCreateTime,
        DateTime maxCreateTime)
    {
        var from = request.StartDate?.Date ?? minCreateTime.Date;
        var to = request.EndDate?.Date ?? maxCreateTime.Date;
        if (to < from)
            to = from;
        return (from, to);
    }

    private static string NormalizeGroupBy(string? groupBy) =>
        groupBy switch
        {
            "day" => "day",
            "week" => "week",
            _ => "month"
        };

    private static List<SalesAnalyticsBreakdownItemDto> BuildVendorDimensionBreakdown<T>(
        IEnumerable<T> rows,
        Func<T, string> keySelector,
        Func<T, string> labelSelector,
        Func<T, decimal> valueSelector,
        bool maskAmounts)
    {
        var items = rows
            .GroupBy(keySelector, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var sample = g.First();
                return new SalesAnalyticsBreakdownItemDto
                {
                    Key = g.Key,
                    Label = labelSelector(sample),
                    Value = maskAmounts ? g.Count() : g.Sum(valueSelector),
                    Ratio = 0
                };
            })
            .ToList();

        ApplyRatios(items);
        return items;
    }

    private sealed class OrderAnalyticsRow
    {
        public string? VendorId { get; set; }
        public decimal ConvertTotal { get; set; }
        public decimal Total { get; set; }
        public short Currency { get; set; }
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

    private static string FormatOrderStatus(short status) => status switch
    {
        0 => "草稿",
        1 => "新建",
        2 => "待审核",
        10 => "审核通过",
        20 => "待确认",
        30 => "已确认",
        50 => "进行中",
        100 => "采购完成",
        -1 => "审核失败",
        -2 => "取消",
        _ => $"状态{status}"
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
            var parts = period.Split("-W", StringSplitOptions.None);
            if (parts.Length == 2
                && int.TryParse(parts[0], out var year)
                && int.TryParse(parts[1], out var week))
            {
                var start = System.Globalization.ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
                return (start, start.AddDays(7));
            }
        }

        return (DateTime.MinValue, DateTime.MinValue);
    }
}
