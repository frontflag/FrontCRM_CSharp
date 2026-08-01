using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Vendor;
using CRM.Core.Utilities;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Vendors;

public sealed partial class VendorListQuery
{
    private const int RankingTopN = 10;
    private const short PoApproved = 10;

    /// <inheritdoc />
    public async Task<PurchaseAnalyticsVendorDto> GetListAnalyticsVendorAsync(
        VendorQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var userId = request.CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return EmptyVendorAnalytics(maskAmounts);

        if (request.FavoriteVendorIds != null && request.FavoriteVendorIds.Count == 0)
            return EmptyVendorAnalytics(maskAmounts);

        var filteredVendors = await BuildFilteredVendorQueryAsync(request, cancellationToken);

        var orders = _db.PurchaseOrders.AsNoTracking();
        orders = PurchaseAnalyticsDateFilter.ApplyAnalyticsStatusFilter(orders);
        orders = await _dataPermission.ApplyPurchaseOrderDataScopeAsync(userId, orders, cancellationToken);

        var approved =
            from o in orders
            where o.Status >= PoApproved
            join v in filteredVendors on o.VendorId equals v.Id
            select new { Order = o, Vendor = v };

        var orderRows = await approved
            .Select(x => new
            {
                x.Order.VendorId,
                x.Order.VendorName,
                x.Order.ConvertTotal,
                VendorCredit = x.Vendor.Credit,
                VendorLevel = x.Vendor.Level,
                x.Vendor.Industry
            })
            .ToListAsync(cancellationToken);

        var vendorGroups = orderRows
            .Where(o => !string.IsNullOrWhiteSpace(o.VendorId))
            .GroupBy(o => o.VendorId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                Count = g.Count(),
                Amount = g.Sum(x => x.ConvertTotal),
                Name = g.First().VendorName,
                Id = g.Key
            })
            .ToList();

        var snapshot = new PurchaseAnalyticsVendorSnapshotDto
        {
            ApprovedVendorCount = vendorGroups.Count,
            RepeatVendorCount = vendorGroups.Count(v => v.Count >= 2)
        };

        var creditItems = BuildVendorDimensionBreakdown(
            orderRows,
            r => r.VendorCredit?.ToString() ?? "_unset",
            r => r.VendorCredit.HasValue ? $"身份 {r.VendorCredit}" : "未设置",
            r => r.ConvertTotal,
            maskAmounts);
        var levelItems = BuildVendorDimensionBreakdown(
            orderRows,
            r => r.VendorLevel?.ToString() ?? "_unset",
            r => r.VendorLevel.HasValue ? $"等级 {r.VendorLevel}" : "未设置",
            r => r.ConvertTotal,
            maskAmounts);
        var industryItems = BuildVendorDimensionBreakdown(
            orderRows,
            r => string.IsNullOrWhiteSpace(r.Industry) ? "_unset" : r.Industry!.Trim(),
            r => string.IsNullOrWhiteSpace(r.Industry) ? "未设置" : r.Industry!.Trim(),
            r => r.ConvertTotal,
            maskAmounts);

        var breakdowns = new List<SalesAnalyticsBreakdownGroupDto>
        {
            new()
            {
                GroupKey = "vendorCredit",
                GroupLabel = maskAmounts ? "供应商身份（成单数）" : "供应商身份（成单 USD）",
                Items = creditItems
            },
            new()
            {
                GroupKey = "vendorLevel",
                GroupLabel = maskAmounts ? "供应商等级（成单数）" : "供应商等级（成单 USD）",
                Items = levelItems
            },
            new()
            {
                GroupKey = "vendorIndustry",
                GroupLabel = maskAmounts ? "供应商行业（成单数）" : "供应商行业（成单 USD）",
                Items = industryItems
            }
        };

        var vendorByAmount = vendorGroups
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

        var vendorByOrderCount = vendorGroups
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

        var vendorByRepeat = vendorGroups
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

        return new PurchaseAnalyticsVendorDto
        {
            ScopeContext = new PurchaseAnalyticsScopeContextDto { MaskAmounts = maskAmounts },
            Snapshot = snapshot,
            Breakdowns = breakdowns,
            Rankings = new PurchaseAnalyticsVendorRankingsDto
            {
                VendorByAmount = vendorByAmount,
                VendorByOrderCount = vendorByOrderCount,
                VendorByRepeatOrderCount = vendorByRepeat
            }
        };
    }

    private static PurchaseAnalyticsVendorDto EmptyVendorAnalytics(bool maskAmounts) => new()
    {
        ScopeContext = new PurchaseAnalyticsScopeContextDto { MaskAmounts = maskAmounts },
        Snapshot = new PurchaseAnalyticsVendorSnapshotDto(),
        Breakdowns = Array.Empty<SalesAnalyticsBreakdownGroupDto>(),
        Rankings = new PurchaseAnalyticsVendorRankingsDto()
    };

    private static List<SalesAnalyticsBreakdownItemDto> BuildVendorDimensionBreakdown<T>(
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
