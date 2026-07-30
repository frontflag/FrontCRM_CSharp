using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;
using CRM.Infrastructure.Common;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.SalesOrders;

/// <summary>销售订单主表列表：EF 数据库分页。</summary>
public sealed class SalesOrderListQuery : ISalesOrderListQuery
{
    /// <summary>单页上限（与采购订单主表列表对齐，便于大批量导出）。</summary>
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public SalesOrderListQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    /// <inheritdoc />
    public async Task<PagedResult<SellOrder>> GetPagedAsync(
        SalesOrderQueryRequest request,
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

        return new PagedResult<SellOrder>
        {
            Items = items,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<SalesOrderListAggregates> GetAggregatesAsync(
        SalesOrderQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var q = await BuildFilteredQueryAsync(request, cancellationToken);
        return new SalesOrderListAggregates
        {
            TotalCount = await q.CountAsync(cancellationToken),
            PendingCount = await q.CountAsync(
                o => o.Status == SellOrderMainStatus.New || o.Status == SellOrderMainStatus.PendingAudit,
                cancellationToken),
            ApprovedPlusCount = await q.CountAsync(
                o => o.Status == SellOrderMainStatus.Approved
                    || o.Status == SellOrderMainStatus.InProgress
                    || o.Status == SellOrderMainStatus.Completed,
                cancellationToken),
            TotalAmountSum = await q.SumAsync(o => (decimal?)o.ConvertTotal, cancellationToken) ?? 0m
        };
    }

    private async Task<IQueryable<SellOrder>> BuildFilteredQueryAsync(
        SalesOrderQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = _db.SellOrders.AsNoTracking();
        q = await _dataPermission.ApplySellOrderDataScopeAsync(request.CurrentUserId, q, cancellationToken);

        var hasSplitFilters = !string.IsNullOrWhiteSpace(request.SellOrderCodeFilter)
            || !string.IsNullOrWhiteSpace(request.CustomerNameFilter);

        if (!string.IsNullOrWhiteSpace(request.Keyword) && !hasSplitFilters)
        {
            var k = request.Keyword.Trim();
            q = q.Where(o =>
                (o.SellOrderCode != null && o.SellOrderCode.ToLower().Contains(k.ToLower())) ||
                (o.CustomerName != null && o.CustomerName.ToLower().Contains(k.ToLower())));
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(request.SellOrderCodeFilter))
            {
                var c = request.SellOrderCodeFilter.Trim();
                q = q.Where(o => o.SellOrderCode != null && o.SellOrderCode.ToLower().Contains(c.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(request.CustomerNameFilter))
            {
                var v = request.CustomerNameFilter.Trim();
                q = q.Where(o => o.CustomerName != null && o.CustomerName.ToLower().Contains(v.ToLower()));
            }
        }

        var statuses = SellOrderStatusFilterHelper.Normalize(request.Status);
        if (statuses.Count > 0)
        {
            // 用枚举 Contains，确保 EF 译为 SQL IN（OR）
            var statusEnums = statuses.Select(s => (SellOrderMainStatus)s).ToList();
            q = q.Where(o => statusEnums.Contains(o.Status));
        }

        if (request.StartDate.HasValue)
        {
            var from = SalesAnalyticsDateFilter.ToUtcDateStart(request.StartDate.Value);
            q = q.Where(o => o.CreateTime >= from);
        }

        if (request.EndDate.HasValue)
        {
            var endExclusive = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(request.EndDate.Value);
            q = q.Where(o => o.CreateTime < endExclusive);
        }

        if (!string.IsNullOrWhiteSpace(request.SalesUserNameFilter))
        {
            var s = request.SalesUserNameFilter.Trim();
            q = q.Where(o =>
                o.SalesUserName != null &&
                o.SalesUserName.ToLower().Contains(s.ToLower()));
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

    /// <inheritdoc />
    public async Task<SalesOrderListAnalyticsComparable> GetAnalyticsComparableAsync(
        SalesOrderQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var q = await BuildFilteredQueryAsync(request, cancellationToken);
        q = SalesAnalyticsDateFilter.ApplyAnalyticsStatusFilter(q);

        var itemCount = await (
            from oi in _db.SellOrderItems.AsNoTracking()
            join o in q on oi.SellOrderId equals o.Id
            where oi.Status == 0
            select oi.Id
        ).CountAsync(cancellationToken);

        return new SalesOrderListAnalyticsComparable
        {
            OrderCount = await q.CountAsync(cancellationToken),
            CustomerCount = await q.Select(o => o.CustomerId).Distinct().CountAsync(cancellationToken),
            ItemCount = itemCount,
            ApprovedConvertTotal = await q
                .Where(o => o.Status >= SellOrderMainStatus.Approved)
                .SumAsync(o => (decimal?)o.ConvertTotal, cancellationToken) ?? 0m
        };
    }

    /// <inheritdoc />
    public async Task<SalesOrderListAnalyticsDashboardDto> GetListAnalyticsDashboardAsync(
        SalesOrderQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        // 列表看板口径：与搜索栏筛选结果一致，不再强制 status≥审核通过。
        var filtered = await BuildFilteredQueryAsync(request, cancellationToken);
        var rows = await filtered
            .Select(o => new OrderAnalyticsRow
            {
                CustomerId = o.CustomerId,
                ConvertTotal = o.ConvertTotal,
                Total = o.Total,
                Currency = o.Currency
            })
            .ToListAsync(cancellationToken);

        var customerGroups = rows
            .Where(o => !string.IsNullOrWhiteSpace(o.CustomerId))
            .GroupBy(o => o.CustomerId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Count())
            .ToList();

        var repeatCustomers = customerGroups.Count(c => c >= 2);
        var repeatOrders = customerGroups.Sum(c => Math.Max(0, c - 1));
        var amountUsd = rows.Sum(o => o.ConvertTotal);

        var currencyLines = rows
            .GroupBy(o => o.Currency)
            .Select(g => new SalesOrderListAnalyticsCurrencyLineDto
            {
                CurrencyKey = g.Key.ToString(),
                CurrencyLabel = ((CurrencyCode)g.Key).ToIsoText(),
                OriginalAmount = maskAmounts ? null : g.Sum(x => x.Total),
                UsdAmount = maskAmounts ? null : g.Sum(x => x.ConvertTotal)
            })
            .OrderByDescending(x => x.UsdAmount ?? x.OriginalAmount ?? 0m)
            .ToList();

        return new SalesOrderListAnalyticsDashboardDto
        {
            Context = new SalesOrderListAnalyticsContextDto { MaskAmounts = maskAmounts },
            Snapshot = new SalesOrderListAnalyticsSnapshotDto
            {
                ApprovedCustomerCount = customerGroups.Count,
                RepeatCustomerCount = repeatCustomers,
                ApprovedOrderCount = rows.Count,
                RepeatOrderCount = repeatOrders,
                ApprovedAmountUsd = maskAmounts ? null : amountUsd,
                CurrencyLines = currencyLines
            }
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SalesOrderListAnalyticsTrendPointDto>> GetListAnalyticsTrendsAsync(
        SalesOrderQueryRequest request,
        string groupBy,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var filtered = await BuildFilteredQueryAsync(request, cancellationToken);
        var orderRows = await filtered
            .Select(o => new { o.CreateTime, o.ConvertTotal })
            .ToListAsync(cancellationToken);

        if (orderRows.Count == 0)
            return Array.Empty<SalesOrderListAnalyticsTrendPointDto>();

        var (dateFrom, dateToInclusive) = ResolveTrendDateBounds(request, orderRows.Min(r => r.CreateTime), orderRows.Max(r => r.CreateTime));
        var normalizedGroupBy = NormalizeGroupBy(groupBy);
        var periods = BuildPeriodKeys(dateFrom, dateToInclusive, normalizedGroupBy);
        var result = new List<SalesOrderListAnalyticsTrendPointDto>();

        foreach (var period in periods)
        {
            var (start, end) = ParsePeriodRange(period, normalizedGroupBy);
            var inBucket = orderRows.Where(r => r.CreateTime >= start && r.CreateTime < end).ToList();
            result.Add(new SalesOrderListAnalyticsTrendPointDto
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
        SalesOrderQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var filtered = await BuildFilteredQueryAsync(request, cancellationToken);

        var statusRows = await filtered
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Amount = g.Sum(x => x.ConvertTotal) })
            .ToListAsync(cancellationToken);

        var statusItems = statusRows.Select(r => new SalesAnalyticsBreakdownItemDto
        {
            Key = ((short)r.Status).ToString(),
            Label = FormatOrderStatus(r.Status),
            Value = maskAmounts ? r.Count : r.Amount,
            Ratio = 0
        }).ToList();
        ApplyRatios(statusItems);

        var currencyRows = await filtered
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

        var customerDimRows = await (
            from o in filtered
            join c in _db.Customers.AsNoTracking() on o.CustomerId equals c.Id into cj
            from c in cj.DefaultIfEmpty()
            select new
            {
                o.ConvertTotal,
                CustomerType = c != null ? c.CustomerType : (short?)null,
                CustomerLevel = c != null ? c.CustomerLevel : null,
                Industry = c != null ? c.Industry : null
            }
        ).ToListAsync(cancellationToken);

        var typeItems = BuildCustomerDimensionBreakdown(
            customerDimRows,
            r => r.CustomerType?.ToString() ?? "_unset",
            r => r.CustomerType.HasValue ? $"类型 {r.CustomerType}" : "未设置",
            r => maskAmounts ? 1m : r.ConvertTotal,
            maskAmounts);
        var levelItems = BuildCustomerDimensionBreakdown(
            customerDimRows,
            r => string.IsNullOrWhiteSpace(r.CustomerLevel) ? "_unset" : r.CustomerLevel!.Trim(),
            r => string.IsNullOrWhiteSpace(r.CustomerLevel) ? "未设置" : r.CustomerLevel!.Trim(),
            r => maskAmounts ? 1m : r.ConvertTotal,
            maskAmounts);
        var industryItems = BuildCustomerDimensionBreakdown(
            customerDimRows,
            r => string.IsNullOrWhiteSpace(r.Industry) ? "_unset" : r.Industry!.Trim(),
            r => string.IsNullOrWhiteSpace(r.Industry) ? "未设置" : r.Industry!.Trim(),
            r => maskAmounts ? 1m : r.ConvertTotal,
            maskAmounts);

        var salesUserRows = await filtered
            .GroupBy(o => new { o.SalesUserId, o.SalesUserName })
            .Select(g => new
            {
                g.Key.SalesUserId,
                Name = g.Key.SalesUserName,
                Amount = g.Sum(x => x.ConvertTotal),
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        var salesUserItems = salesUserRows.Select(r => new SalesAnalyticsBreakdownItemDto
        {
            Key = r.SalesUserId ?? "_unset",
            Label = string.IsNullOrWhiteSpace(r.Name) ? "未分配业务员" : r.Name!,
            Value = maskAmounts ? r.Count : r.Amount,
            Ratio = 0
        }).ToList();
        ApplyRatios(salesUserItems);

        return new List<SalesAnalyticsBreakdownGroupDto>
        {
            new() { GroupKey = "orderStatus", GroupLabel = "订单主状态", Items = statusItems },
            new() { GroupKey = "currency", GroupLabel = "币别构成", Items = currencyItems },
            new() { GroupKey = "customerType", GroupLabel = "客户类型（USD）", Items = typeItems },
            new() { GroupKey = "customerLevel", GroupLabel = "客户等级（USD）", Items = levelItems },
            new() { GroupKey = "customerIndustry", GroupLabel = "客户行业（USD）", Items = industryItems },
            new() { GroupKey = "salesUser", GroupLabel = "业务员（USD）", Items = salesUserItems }
        };
    }

    /// <inheritdoc />
    public async Task<SalesOrderListAnalyticsRankingsDto> GetListAnalyticsRankingsAsync(
        SalesOrderQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        const int topN = 10;
        var filtered = await BuildFilteredQueryAsync(request, cancellationToken);
        var orderRows = await filtered
            .Select(o => new
            {
                o.CustomerId,
                o.CustomerName,
                o.SalesUserId,
                o.SalesUserName,
                o.ConvertTotal
            })
            .ToListAsync(cancellationToken);

        var customerByAmount = orderRows
            .Where(o => !string.IsNullOrWhiteSpace(o.CustomerId))
            .GroupBy(o => o.CustomerId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.First().CustomerName ?? g.Key,
                Amount = maskAmounts ? null : g.Sum(x => x.ConvertTotal),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.Amount ?? x.OrderCount)
            .Take(topN)
            .ToList();

        var customerByOrderCount = orderRows
            .Where(o => !string.IsNullOrWhiteSpace(o.CustomerId))
            .GroupBy(o => o.CustomerId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.First().CustomerName ?? g.Key,
                Amount = maskAmounts ? null : g.Sum(x => x.ConvertTotal),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.OrderCount)
            .ThenByDescending(x => x.Amount ?? 0m)
            .Take(topN)
            .ToList();

        var customerByRepeat = orderRows
            .Where(o => !string.IsNullOrWhiteSpace(o.CustomerId))
            .GroupBy(o => o.CustomerId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.First().CustomerName ?? g.Key,
                Amount = maskAmounts ? null : g.Sum(x => x.ConvertTotal),
                OrderCount = Math.Max(0, g.Count() - 1)
            })
            .Where(x => x.OrderCount > 0)
            .OrderByDescending(x => x.OrderCount)
            .ThenByDescending(x => x.Amount ?? 0m)
            .Take(topN)
            .ToList();

        var salesUserByAmount = orderRows
            .GroupBy(o => o.SalesUserId ?? "_unset", StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.Key == "_unset"
                    ? "未分配业务员"
                    : (g.First().SalesUserName ?? g.Key),
                Amount = maskAmounts ? null : g.Sum(x => x.ConvertTotal),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.Amount ?? x.OrderCount)
            .Take(topN)
            .ToList();

        return new SalesOrderListAnalyticsRankingsDto
        {
            CustomerByAmount = customerByAmount,
            CustomerByOrderCount = customerByOrderCount,
            CustomerByRepeatOrderCount = customerByRepeat,
            SalesUserByAmount = salesUserByAmount
        };
    }

    private static (DateTime From, DateTime ToInclusive) ResolveTrendDateBounds(
        SalesOrderQueryRequest request,
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

    private static List<SalesAnalyticsBreakdownItemDto> BuildCustomerDimensionBreakdown<T>(
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
        public string? CustomerId { get; set; }
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

    private static string FormatOrderStatus(SellOrderMainStatus status) => status switch
    {
        SellOrderMainStatus.New => "新建",
        SellOrderMainStatus.PendingAudit => "待审核",
        SellOrderMainStatus.Approved => "审核通过",
        SellOrderMainStatus.InProgress => "进行中",
        SellOrderMainStatus.Completed => "完成",
        SellOrderMainStatus.AuditFailed => "审核失败",
        SellOrderMainStatus.Cancelled => "取消",
        _ => status.ToString()
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
            var parts = period.Split("-W", StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && int.TryParse(parts[0], out var year) && int.TryParse(parts[1], out var week))
            {
                var start = System.Globalization.ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
                return (start, start.AddDays(7));
            }
        }

        return (DateTime.MinValue, DateTime.MaxValue);
    }
}
