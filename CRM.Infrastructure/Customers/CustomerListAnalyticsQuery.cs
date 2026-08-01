using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Customers;

public sealed partial class CustomerListQuery
{
    private const int RankingTopN = 10;

    /// <inheritdoc />
    public async Task<SalesAnalyticsCustomerDto> GetListAnalyticsCustomerAsync(
        CustomerQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var userId = request.CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return EmptyCustomerAnalytics(maskAmounts);
        }

        if (request.FavoriteCustomerIds != null && request.FavoriteCustomerIds.Count == 0)
            return EmptyCustomerAnalytics(maskAmounts);

        var filteredCustomers = await BuildFilteredCustomerQueryAsync(request, cancellationToken);

        var orders = _db.SellOrders.AsNoTracking();
        orders = SalesAnalyticsDateFilter.ApplyAnalyticsStatusFilter(orders);
        orders = await _dataPermission.ApplySellOrderDataScopeAsync(userId, orders, cancellationToken);

        var approved =
            from o in orders
            where o.Status >= SellOrderMainStatus.Approved
            join c in filteredCustomers on o.CustomerId equals c.Id
            select new { Order = o, Customer = c };

        var orderRows = await approved
            .Select(x => new
            {
                x.Order.CustomerId,
                x.Order.CustomerName,
                x.Order.ConvertTotal,
                CustomerType = x.Customer.Type,
                CustomerLevel = x.Customer.Level,
                x.Customer.Industry
            })
            .ToListAsync(cancellationToken);

        var customerGroups = orderRows
            .Where(o => !string.IsNullOrWhiteSpace(o.CustomerId))
            .GroupBy(o => o.CustomerId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                Count = g.Count(),
                Amount = g.Sum(x => x.ConvertTotal),
                Name = g.First().CustomerName,
                Id = g.Key
            })
            .ToList();

        var snapshot = new SalesAnalyticsCustomerSnapshotDto
        {
            ApprovedCustomerCount = customerGroups.Count,
            RepeatCustomerCount = customerGroups.Count(c => c.Count >= 2)
        };

        var typeItems = BuildCustomerDimensionBreakdown(
            orderRows,
            r => r.CustomerType?.ToString() ?? "_unset",
            r => r.CustomerType.HasValue ? $"类型 {r.CustomerType}" : "未设置",
            r => r.ConvertTotal,
            maskAmounts);
        var levelItems = BuildCustomerDimensionBreakdown(
            orderRows,
            r => LevelToCode(r.CustomerLevel) ?? "_unset",
            r => LevelToCode(r.CustomerLevel) ?? "未设置",
            r => r.ConvertTotal,
            maskAmounts);
        var industryItems = BuildCustomerDimensionBreakdown(
            orderRows,
            r => string.IsNullOrWhiteSpace(r.Industry) ? "_unset" : r.Industry!.Trim(),
            r => string.IsNullOrWhiteSpace(r.Industry) ? "未设置" : r.Industry!.Trim(),
            r => r.ConvertTotal,
            maskAmounts);

        var breakdowns = new List<SalesAnalyticsBreakdownGroupDto>
        {
            new()
            {
                GroupKey = "customerType",
                GroupLabel = maskAmounts ? "客户类型（成单数）" : "客户类型（成单 USD）",
                Items = typeItems
            },
            new()
            {
                GroupKey = "customerLevel",
                GroupLabel = maskAmounts ? "客户等级（成单数）" : "客户等级（成单 USD）",
                Items = levelItems
            },
            new()
            {
                GroupKey = "customerIndustry",
                GroupLabel = maskAmounts ? "客户行业（成单数）" : "客户行业（成单 USD）",
                Items = industryItems
            }
        };

        var customerByAmount = customerGroups
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Id,
                Name = g.Name ?? g.Id,
                Amount = maskAmounts ? null : g.Amount,
                OrderCount = g.Count
            })
            .OrderByDescending(x => x.Amount ?? x.OrderCount)
            .Take(RankingTopN)
            .ToList();

        var customerByOrderCount = customerGroups
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Id,
                Name = g.Name ?? g.Id,
                Amount = maskAmounts ? null : g.Amount,
                OrderCount = g.Count
            })
            .OrderByDescending(x => x.OrderCount)
            .ThenByDescending(x => x.Amount ?? 0m)
            .Take(RankingTopN)
            .ToList();

        var customerByRepeat = customerGroups
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Id,
                Name = g.Name ?? g.Id,
                Amount = maskAmounts ? null : g.Amount,
                OrderCount = Math.Max(0, g.Count - 1)
            })
            .Where(x => x.OrderCount > 0)
            .OrderByDescending(x => x.OrderCount)
            .ThenByDescending(x => x.Amount ?? 0m)
            .Take(RankingTopN)
            .ToList();

        return new SalesAnalyticsCustomerDto
        {
            ScopeContext = new SalesAnalyticsScopeContextDto { MaskAmounts = maskAmounts },
            Snapshot = snapshot,
            Breakdowns = breakdowns,
            Rankings = new SalesAnalyticsCustomerRankingsDto
            {
                CustomerByAmount = customerByAmount,
                CustomerByOrderCount = customerByOrderCount,
                CustomerByRepeatOrderCount = customerByRepeat
            }
        };
    }

    private static SalesAnalyticsCustomerDto EmptyCustomerAnalytics(bool maskAmounts) => new()
    {
        ScopeContext = new SalesAnalyticsScopeContextDto { MaskAmounts = maskAmounts },
        Snapshot = new SalesAnalyticsCustomerSnapshotDto(),
        Breakdowns = Array.Empty<SalesAnalyticsBreakdownGroupDto>(),
        Rankings = new SalesAnalyticsCustomerRankingsDto()
    };

    private static string? LevelToCode(short level) => level switch
    {
        1 => "D",
        2 => "C",
        3 => "B",
        4 => "BPO",
        5 => "VIP",
        6 => "VPO",
        _ => null
    };

    private static List<SalesAnalyticsBreakdownItemDto> BuildCustomerDimensionBreakdown<T>(
        IEnumerable<T> rows,
        Func<T, string> keySelector,
        Func<T, string> labelSelector,
        Func<T, decimal> amountSelector,
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
                    Value = maskAmounts ? g.Count() : g.Sum(amountSelector),
                    Ratio = 0
                };
            })
            .ToList();

        var total = items.Sum(x => x.Value);
        if (total <= 0)
        {
            foreach (var it in items) it.Ratio = 0;
            return items;
        }

        foreach (var it in items)
            it.Ratio = Math.Round(it.Value / total, 4);

        return items;
    }
}
