using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Quote;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Analytics;

public sealed class SalesAnalyticsQuery : ISalesAnalyticsQuery
{
    private const int RankingTopN = 10;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public SalesAnalyticsQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    public async Task<SalesAnalyticsDashboardDto> GetDashboardAsync(
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await BuildSnapshotAsync(scope, cancellationToken);
        var todo = await BuildTodoAsync(scope, cancellationToken);
        var rankings = await BuildRankingsAsync(scope, cancellationToken);

        return new SalesAnalyticsDashboardDto
        {
            ScopeContext = scope.ScopeContext,
            Snapshot = snapshot,
            Todo = todo,
            Rankings = rankings
        };
    }

    public async Task<IReadOnlyList<SalesAnalyticsTrendPointDto>> GetTrendsAsync(
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default)
    {
        var userId = scope.Summary.UserId;
        var dateFrom = SalesAnalyticsDateFilter.ToUtcDateStart(scope.DateFrom);
        var dateEnd = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(scope.DateTo);

        var rfqItems = await BuildRfqItemQueryAsync(userId, scope, cancellationToken);
        var rfqInPeriod = rfqItems.Where(x => x.Item.CreateTime >= dateFrom && x.Item.CreateTime < dateEnd);

        var orders = await BuildSellOrderQueryAsync(userId, scope, cancellationToken);
        var ordersInPeriod = orders.Where(o =>
            o.CreateTime >= dateFrom && o.CreateTime < dateEnd);

        var orderItems = from oi in _db.SellOrderItems.AsNoTracking()
                         join o in ordersInPeriod on oi.SellOrderId equals o.Id
                         where !oi.IsDeleted && oi.Status == 0
                         select new { oi, o };

        var convertedRfqItemIds = from oi in _db.SellOrderItems.AsNoTracking()
                                  join q in _db.Quotes.AsNoTracking() on oi.QuoteId equals q.Id
                                  where !oi.IsDeleted && oi.QuoteId != null && q.RFQItemId != null
                                  select q.RFQItemId!;

        var rfqRows = await rfqInPeriod
            .Select(x => new { x.Item.Id, x.Item.CreateTime, x.Rfq.CustomerId })
            .ToListAsync(cancellationToken);

        var convertedSet = await convertedRfqItemIds.Distinct().ToListAsync(cancellationToken);
        var convertedHash = convertedSet.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var orderRows = await ordersInPeriod
            .Select(o => new { o.Id, o.CreateTime, o.CustomerId, o.ConvertTotal, o.Status })
            .ToListAsync(cancellationToken);

        var itemRows = await orderItems
            .Select(x => new { x.oi.Id, x.o.CreateTime })
            .ToListAsync(cancellationToken);

        var periods = BuildPeriodKeys(dateFrom, scope.DateTo, scope.GroupBy);
        var result = new List<SalesAnalyticsTrendPointDto>();

        foreach (var period in periods)
        {
            var (start, end) = ParsePeriodRange(period, scope.GroupBy);
            var rfqInBucket = rfqRows.Where(r => r.CreateTime >= start && r.CreateTime < end).ToList();
            var rfqCount = rfqInBucket.Count;
            var convertedInBucket = rfqInBucket.Count(r => convertedHash.Contains(r.Id));
            var rate = rfqCount == 0 ? (decimal?)null : Math.Round((decimal)convertedInBucket / rfqCount * 100m, 2);

            var itemsInBucket = itemRows.Count(r => r.CreateTime >= start && r.CreateTime < end);
            var ordersInBucket = orderRows.Where(r => r.CreateTime >= start && r.CreateTime < end).ToList();
            var approvedAmount = ordersInBucket
                .Where(o => o.Status >= SellOrderMainStatus.Approved)
                .Sum(o => o.ConvertTotal);

            result.Add(new SalesAnalyticsTrendPointDto
            {
                Period = period,
                RfqItemCount = rfqCount,
                SalesOrderItemCount = itemsInBucket,
                SalesAmountApproved = scope.MaskAmounts ? null : approvedAmount,
                RfqToSalesConversionRate = rate
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default)
    {
        var userId = scope.Summary.UserId;
        var dateFrom = SalesAnalyticsDateFilter.ToUtcDateStart(scope.DateFrom);
        var dateEnd = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(scope.DateTo);

        var orders = await BuildSellOrderQueryAsync(userId, scope, cancellationToken);
        var ordersInPeriod = orders.Where(o => o.CreateTime >= dateFrom && o.CreateTime < dateEnd);

        var statusRows = await ordersInPeriod
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Amount = g.Sum(x => x.ConvertTotal) })
            .ToListAsync(cancellationToken);

        var statusItems = statusRows.Select(r => new SalesAnalyticsBreakdownItemDto
        {
            Key = ((short)r.Status).ToString(),
            Label = FormatOrderStatus(r.Status),
            Value = scope.MaskAmounts ? r.Count : r.Amount,
            Ratio = 0
        }).ToList();
        ApplyRatios(statusItems);

        var currencyRows = await ordersInPeriod
            .Where(o => o.Status >= SellOrderMainStatus.Approved)
            .GroupBy(o => o.Currency)
            .Select(g => new { Currency = g.Key, Amount = g.Sum(x => x.ConvertTotal) })
            .ToListAsync(cancellationToken);

        var currencyItems = currencyRows.Select(r => new SalesAnalyticsBreakdownItemDto
        {
            Key = r.Currency.ToString(),
            Label = ((CurrencyCode)r.Currency).ToIsoText(),
            Value = scope.MaskAmounts ? 0 : r.Amount,
            Ratio = 0
        }).ToList();
        ApplyRatios(currencyItems);

        var progressRows = await (
            from ext in _db.SellOrderItemExtends.AsNoTracking()
            join oi in _db.SellOrderItems.AsNoTracking() on ext.Id equals oi.Id
            join o in ordersInPeriod on oi.SellOrderId equals o.Id
            where !oi.IsDeleted && !ext.IsDeleted && oi.Status == 0
            group ext by ext.StockOutProgressStatus into g
            select new { Status = g.Key, Count = g.Count() }
        ).ToListAsync(cancellationToken);

        var progressItems = progressRows.Select(r => new SalesAnalyticsBreakdownItemDto
        {
            Key = r.Status.ToString(),
            Label = FormatStockOutProgress(r.Status),
            Value = r.Count,
            Ratio = 0
        }).ToList();
        ApplyRatios(progressItems);

        return new List<SalesAnalyticsBreakdownGroupDto>
        {
            new() { GroupKey = "orderStatus", GroupLabel = "订单主状态", Items = statusItems },
            new() { GroupKey = "currency", GroupLabel = "币别金额（成单已审核）", Items = currencyItems },
            new() { GroupKey = "stockOutProgress", GroupLabel = "出库进度（明细行）", Items = progressItems }
        };
    }

    private async Task<SalesAnalyticsSnapshotDto> BuildSnapshotAsync(
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var userId = scope.Summary.UserId;
        var dateFrom = SalesAnalyticsDateFilter.ToUtcDateStart(scope.DateFrom);
        var dateEnd = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(scope.DateTo);

        if (BusinessDepartmentRules.UseSellOrderAssistorOnlyScope(scope.Summary))
        {
            return await BuildSnapshotForAssistorOnlyAsync(scope, dateFrom, dateEnd, cancellationToken);
        }

        var rfqItems = await BuildRfqItemQueryAsync(userId, scope, cancellationToken);
        var rfqInPeriod = rfqItems.Where(x => x.Item.CreateTime >= dateFrom && x.Item.CreateTime < dateEnd);

        var rfqItemCount = await rfqInPeriod.CountAsync(cancellationToken);
        var rfqCustomerCount = await rfqInPeriod
            .Where(x => x.Rfq.CustomerId != null)
            .Select(x => x.Rfq.CustomerId!)
            .Distinct()
            .CountAsync(cancellationToken);

        var rfqItemIds = await rfqInPeriod.Select(x => x.Item.Id).ToListAsync(cancellationToken);
        var convertedCount = 0;
        if (rfqItemIds.Count > 0)
        {
            convertedCount = await (
                from oi in _db.SellOrderItems.AsNoTracking()
                join q in _db.Quotes.AsNoTracking() on oi.QuoteId equals q.Id
                where !oi.IsDeleted && oi.QuoteId != null && q.RFQItemId != null && rfqItemIds.Contains(q.RFQItemId)
                select q.RFQItemId
            ).Distinct().CountAsync(cancellationToken);
        }

        var orders = await BuildSellOrderQueryAsync(userId, scope, cancellationToken);
        var ordersInPeriod = orders.Where(o => o.CreateTime >= dateFrom && o.CreateTime < dateEnd);

        var salesOrderItemCount = await (
            from oi in _db.SellOrderItems.AsNoTracking()
            join o in ordersInPeriod on oi.SellOrderId equals o.Id
            where !oi.IsDeleted && oi.Status == 0
            select oi.Id
        ).CountAsync(cancellationToken);

        var salesOrderCustomerCount = await ordersInPeriod
            .Select(o => o.CustomerId)
            .Distinct()
            .CountAsync(cancellationToken);

        var salesAmountApproved = await ordersInPeriod
            .Where(o => o.Status >= SellOrderMainStatus.Approved)
            .SumAsync(o => (decimal?)o.ConvertTotal, cancellationToken) ?? 0m;

        decimal? rate = rfqItemCount == 0
            ? null
            : Math.Round((decimal)convertedCount / rfqItemCount * 100m, 2);

        return new SalesAnalyticsSnapshotDto
        {
            RfqItemCount = rfqItemCount,
            RfqCustomerCount = rfqCustomerCount,
            RfqToSalesConversionRate = rate,
            SalesOrderItemCount = salesOrderItemCount,
            SalesOrderCustomerCount = salesOrderCustomerCount,
            SalesAmountApproved = scope.MaskAmounts ? null : salesAmountApproved
        };
    }

    private async Task<SalesAnalyticsSnapshotDto> BuildSnapshotForAssistorOnlyAsync(
        SalesAnalyticsResolvedScope scope,
        DateTime dateFrom,
        DateTime dateEnd,
        CancellationToken cancellationToken)
    {
        var orders = await BuildSellOrderQueryAsync(scope.Summary.UserId, scope, cancellationToken);
        var ordersInPeriod = orders.Where(o => o.CreateTime >= dateFrom && o.CreateTime < dateEnd);

        var salesOrderItemCount = await (
            from oi in _db.SellOrderItems.AsNoTracking()
            join o in ordersInPeriod on oi.SellOrderId equals o.Id
            where !oi.IsDeleted && oi.Status == 0
            select oi.Id
        ).CountAsync(cancellationToken);

        var salesOrderCustomerCount = await ordersInPeriod.Select(o => o.CustomerId).Distinct().CountAsync(cancellationToken);
        var salesAmountApproved = await ordersInPeriod
            .Where(o => o.Status >= SellOrderMainStatus.Approved)
            .SumAsync(o => (decimal?)o.ConvertTotal, cancellationToken) ?? 0m;

        return new SalesAnalyticsSnapshotDto
        {
            RfqItemCount = 0,
            RfqCustomerCount = 0,
            RfqToSalesConversionRate = null,
            SalesOrderItemCount = salesOrderItemCount,
            SalesOrderCustomerCount = salesOrderCustomerCount,
            SalesAmountApproved = scope.MaskAmounts ? null : salesAmountApproved
        };
    }

    private async Task<SalesAnalyticsTodoDto> BuildTodoAsync(
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var userId = scope.Summary.UserId;
        var orders = await BuildSellOrderQueryAsync(userId, scope, cancellationToken);
        var activeOrders = orders.Where(o =>
            o.Status != SellOrderMainStatus.Cancelled && o.Status != SellOrderMainStatus.AuditFailed);

        var receivable = await (
            from ext in _db.SellOrderItemExtends.AsNoTracking()
            join oi in _db.SellOrderItems.AsNoTracking() on ext.Id equals oi.Id
            join o in activeOrders on oi.SellOrderId equals o.Id
            where !oi.IsDeleted && !ext.IsDeleted && oi.Status == 0
            select ext.ReceiptAmountNot
        ).SumAsync(cancellationToken);

        var pendingStockOut = await (
            from ext in _db.SellOrderItemExtends.AsNoTracking()
            join oi in _db.SellOrderItems.AsNoTracking() on ext.Id equals oi.Id
            join o in activeOrders on oi.SellOrderId equals o.Id
            where !oi.IsDeleted && !ext.IsDeleted && oi.Status == 0
                  && (ext.StockOutProgressStatus == 0 || ext.StockOutProgressStatus == 1)
            select oi.Id
        ).CountAsync(cancellationToken);

        return new SalesAnalyticsTodoDto
        {
            ReceivableAmount = scope.MaskAmounts ? null : receivable,
            PendingStockOutItemCount = pendingStockOut
        };
    }

    private async Task<SalesAnalyticsRankingsDto> BuildRankingsAsync(
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var userId = scope.Summary.UserId;
        var dateFrom = SalesAnalyticsDateFilter.ToUtcDateStart(scope.DateFrom);
        var dateEnd = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(scope.DateTo);

        var orders = await BuildSellOrderQueryAsync(userId, scope, cancellationToken);
        var ordersInPeriod = orders.Where(o =>
            o.CreateTime >= dateFrom && o.CreateTime < dateEnd
            && o.Status >= SellOrderMainStatus.Approved);

        var primaryDeptMap = await (
            from ud in _db.RbacUserDepartments.AsNoTracking()
            where ud.IsPrimary
            select new { ud.UserId, ud.DepartmentId }
        ).ToDictionaryAsync(x => x.UserId, x => x.DepartmentId, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var departments = await _db.RbacDepartments.AsNoTracking()
            .Where(d => d.Status == 1)
            .ToDictionaryAsync(d => d.Id, d => d.DepartmentName, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var users = await _db.Users.AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u.RealName ?? u.UserName, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var orderRows = await ordersInPeriod
            .Select(o => new { o.SalesUserId, o.CustomerId, o.CustomerName, o.ConvertTotal })
            .ToListAsync(cancellationToken);

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Company)
        {
            var deptGroups = orderRows
                .GroupBy(o =>
                {
                    if (string.IsNullOrWhiteSpace(o.SalesUserId)) return SalesAnalyticsScopeValidator.UnassignedDepartmentId;
                    return primaryDeptMap.TryGetValue(o.SalesUserId, out var did) ? did : SalesAnalyticsScopeValidator.UnassignedDepartmentId;
                })
                .Select(g => new SalesAnalyticsRankingRowDto
                {
                    Id = g.Key,
                    Name = g.Key == SalesAnalyticsScopeValidator.UnassignedDepartmentId
                        ? "未分配部门"
                        : departments.GetValueOrDefault(g.Key, g.Key),
                    Amount = scope.MaskAmounts ? null : g.Sum(x => x.ConvertTotal),
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.Amount ?? x.OrderCount)
                .Take(RankingTopN)
                .ToList();

            var customerGroups = orderRows
                .GroupBy(o => o.CustomerId)
                .Select(g => new SalesAnalyticsRankingRowDto
                {
                    Id = g.Key,
                    Name = g.First().CustomerName ?? g.Key,
                    Amount = scope.MaskAmounts ? null : g.Sum(x => x.ConvertTotal),
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.Amount ?? x.OrderCount)
                .Take(RankingTopN)
                .ToList();

            return new SalesAnalyticsRankingsDto { Primary = deptGroups, Secondary = customerGroups };
        }

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Department)
        {
            var userGroups = orderRows
                .GroupBy(o => o.SalesUserId ?? SalesAnalyticsScopeValidator.UnassignedDepartmentId)
                .Select(g => new SalesAnalyticsRankingRowDto
                {
                    Id = g.Key,
                    Name = g.Key == SalesAnalyticsScopeValidator.UnassignedDepartmentId
                        ? "未分配业务员"
                        : users.GetValueOrDefault(g.Key, g.Key),
                    Amount = scope.MaskAmounts ? null : g.Sum(x => x.ConvertTotal),
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.Amount ?? x.OrderCount)
                .Take(RankingTopN)
                .ToList();

            var customerGroups = orderRows
                .GroupBy(o => o.CustomerId)
                .Select(g => new SalesAnalyticsRankingRowDto
                {
                    Id = g.Key,
                    Name = g.First().CustomerName ?? g.Key,
                    Amount = scope.MaskAmounts ? null : g.Sum(x => x.ConvertTotal),
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.Amount ?? x.OrderCount)
                .Take(RankingTopN)
                .ToList();

            return new SalesAnalyticsRankingsDto { Primary = userGroups, Secondary = customerGroups };
        }

        var targetUser = scope.SalesUserId ?? scope.Summary.UserId;
        var personalOrders = orderRows
            .Where(o => string.Equals(o.SalesUserId, targetUser, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var customers = personalOrders
            .GroupBy(o => o.CustomerId)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.First().CustomerName ?? g.Key,
                Amount = scope.MaskAmounts ? null : g.Sum(x => x.ConvertTotal),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.Amount ?? x.OrderCount)
            .Take(RankingTopN)
            .ToList();

        return new SalesAnalyticsRankingsDto { Primary = customers, Secondary = Array.Empty<SalesAnalyticsRankingRowDto>() };
    }

    private async Task<IQueryable<SellOrder>> BuildSellOrderQueryAsync(
        string userId,
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var q = _db.SellOrders.AsNoTracking();
        q = SalesAnalyticsDateFilter.ApplyAnalyticsStatusFilter(q);
        q = await _dataPermission.ApplySellOrderDataScopeAsync(userId, q, cancellationToken);

        // Scope=1「仅本人」：数据范围已收窄，不再按 sales_user_id 二次过滤（避免漏掉 assistor 可见单）。
        // 部门/公司下钻到指定业务员时仍按 sales_user_id 透镜。
        if (scope.ViewLevel == SalesAnalyticsViewLevels.Personal
            && !BusinessDepartmentRules.UseSellOrderAssistorOnlyScope(scope.Summary)
            && !string.IsNullOrWhiteSpace(scope.SalesUserId)
            && scope.Summary.SaleDataScope != 1)
        {
            q = q.Where(o => o.SalesUserId == scope.SalesUserId);
        }

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Department)
        {
            q = await ApplyDepartmentLensAsync(q, scope, cancellationToken);
        }

        return q;
    }

    private Task<IQueryable<SellOrder>> ApplyDepartmentLensAsync(
        IQueryable<SellOrder> q,
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var deptId = scope.DepartmentId;
        if (string.IsNullOrWhiteSpace(deptId))
            deptId = scope.Summary.PrimaryDepartmentId;

        if (string.IsNullOrWhiteSpace(deptId))
            return Task.FromResult(q);

        if (string.Equals(deptId, SalesAnalyticsScopeValidator.UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase))
        {
            var withPrimary = _db.RbacUserDepartments.AsNoTracking()
                .Where(ud => ud.IsPrimary)
                .Select(ud => ud.UserId);
            return Task.FromResult(q.Where(o =>
                o.SalesUserId == null
                || !withPrimary.Contains(o.SalesUserId)));
        }

        var userIdsInDept = _db.RbacUserDepartments.AsNoTracking()
            .Where(ud => ud.IsPrimary && ud.DepartmentId == deptId)
            .Select(ud => ud.UserId);

        return Task.FromResult(q.Where(o => o.SalesUserId != null && userIdsInDept.Contains(o.SalesUserId)));
    }

    private async Task<IQueryable<RfqItemJoinRow>> BuildRfqItemQueryAsync(
        string userId,
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        if (BusinessDepartmentRules.UseSellOrderAssistorOnlyScope(scope.Summary))
        {
            return from item in _db.RFQItems.AsNoTracking()
                   join rfq in _db.RFQs.AsNoTracking() on item.RfqId equals rfq.Id
                   where false
                   select new RfqItemJoinRow { Item = item, Rfq = rfq };
        }

        var baseQ =
            from item in _db.RFQItems.AsNoTracking()
            join rfq in _db.RFQs.AsNoTracking() on item.RfqId equals rfq.Id
            where !item.IsDeleted && !rfq.IsDeleted
            select new { item, rfq };

        var scopedRfqs = await _dataPermission.ApplyRfqMainListDataScopeAsync(
            userId,
            _db.RFQs.AsNoTracking().Where(r => !r.IsDeleted),
            cancellationToken);

        var rfqIds = scopedRfqs.Select(r => r.Id);
        var q = baseQ.Where(x => rfqIds.Contains(x.rfq.Id));

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Personal && !string.IsNullOrWhiteSpace(scope.SalesUserId))
            q = q.Where(x => x.rfq.SalesUserId == scope.SalesUserId);

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Department)
        {
            var deptId = scope.DepartmentId ?? scope.Summary.PrimaryDepartmentId;
            if (!string.IsNullOrWhiteSpace(deptId))
            {
                if (string.Equals(deptId, SalesAnalyticsScopeValidator.UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase))
                {
                    var withPrimary = _db.RbacUserDepartments.AsNoTracking()
                        .Where(ud => ud.IsPrimary)
                        .Select(ud => ud.UserId);
                    q = q.Where(x =>
                        x.rfq.SalesUserId == null
                        || !withPrimary.Contains(x.rfq.SalesUserId));
                }
                else
                {
                    var userIdsInDept = _db.RbacUserDepartments.AsNoTracking()
                        .Where(ud => ud.IsPrimary && ud.DepartmentId == deptId)
                        .Select(ud => ud.UserId);
                    q = q.Where(x => x.rfq.SalesUserId != null && userIdsInDept.Contains(x.rfq.SalesUserId));
                }
            }
        }

        return q.Select(x => new RfqItemJoinRow { Item = x.item, Rfq = x.rfq });
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
        _ => status.ToString()
    };

    private static string FormatStockOutProgress(short status) => status switch
    {
        0 => "待出库",
        1 => "部分出库",
        2 => "出库完成",
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
