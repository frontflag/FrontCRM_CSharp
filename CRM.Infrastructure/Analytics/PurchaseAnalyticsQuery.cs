using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Quote;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Analytics;

public sealed class PurchaseAnalyticsQuery : IPurchaseAnalyticsQuery
{
    private const int RankingTopN = 10;
    private const short PoApproved = 10;
    private const short PoItemCancelled = -2;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public PurchaseAnalyticsQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    public async Task<PurchaseAnalyticsDashboardDto> GetDashboardAsync(
        PurchaseAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await BuildSnapshotAsync(scope, cancellationToken);
        var todo = await BuildTodoAsync(scope, cancellationToken);
        var rankings = await BuildRankingsAsync(scope, cancellationToken);

        return new PurchaseAnalyticsDashboardDto
        {
            ScopeContext = scope.ScopeContext,
            Snapshot = snapshot,
            Todo = todo,
            Rankings = rankings
        };
    }

    public async Task<IReadOnlyList<PurchaseAnalyticsTrendPointDto>> GetTrendsAsync(
        PurchaseAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default)
    {
        var userId = scope.Summary.UserId;
        var dateFrom = PurchaseAnalyticsDateFilter.ToUtcDateStart(scope.DateFrom);
        var dateEnd = PurchaseAnalyticsDateFilter.ToUtcDateEndExclusive(scope.DateTo);

        var quoteItems = await BuildScopedQuoteItemQueryAsync(userId, scope, cancellationToken);
        var quotesInPeriod = FilterQuoteItemsByPeriod(quoteItems, dateFrom, dateEnd);

        var orders = await BuildPurchaseOrderQueryAsync(userId, scope, cancellationToken);
        var ordersInPeriod = orders.Where(o => o.CreateTime >= dateFrom && o.CreateTime < dateEnd);

        var orderItems = from oi in _db.PurchaseOrderItems.AsNoTracking()
                         join o in ordersInPeriod on oi.PurchaseOrderId equals o.Id
                         where !oi.IsDeleted && oi.Status != PoItemCancelled
                         select new { oi, o };

        var convertedQuoteItemIds = BuildConvertedQuoteItemIdsQuery();

        var quoteRows = await quotesInPeriod
            .Select(i => new { Id = i.Id, CreateTime = i.CreateTime, VendorId = i.VendorId })
            .ToListAsync(cancellationToken);

        var convertedSet = await convertedQuoteItemIds.Distinct().ToListAsync(cancellationToken);
        var convertedHash = convertedSet.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var orderRows = await ordersInPeriod
            .Select(o => new { o.Id, o.CreateTime, o.VendorId, o.VendorName, o.ConvertTotal, o.Status })
            .ToListAsync(cancellationToken);

        var itemRows = await orderItems
            .Select(x => new { x.oi.Id, x.o.CreateTime })
            .ToListAsync(cancellationToken);

        var lineMetricRows = await (
            from ext in _db.PurchaseOrderItemExtends.AsNoTracking()
            join oi in _db.PurchaseOrderItems.AsNoTracking() on ext.Id equals oi.Id
            join o in ordersInPeriod.Where(o => o.Status >= PoApproved) on oi.PurchaseOrderId equals o.Id
            where !oi.IsDeleted && !ext.IsDeleted && oi.Status != PoItemCancelled
            select new
            {
                o.CreateTime,
                StockInAmount = ext.QtyReceiveTotal * oi.ConvertPrice,
                ext.PaymentAmountFinish,
                ext.PaymentAmountNot
            }
        ).ToListAsync(cancellationToken);

        var periods = BuildPeriodKeys(dateFrom, scope.DateTo, scope.GroupBy);
        var result = new List<PurchaseAnalyticsTrendPointDto>();

        foreach (var period in periods)
        {
            var (start, end) = ParsePeriodRange(period, scope.GroupBy);
            var quotesInBucket = quoteRows.Where(r => r.CreateTime >= start && r.CreateTime < end).ToList();
            var quoteCount = quotesInBucket.Count;
            var quoteVendorCount = quotesInBucket
                .Where(r => !string.IsNullOrWhiteSpace(r.VendorId))
                .Select(r => r.VendorId!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            var convertedInBucket = quotesInBucket.Count(r => convertedHash.Contains(r.Id));
            var rate = quoteCount == 0 ? (decimal?)null : Math.Round((decimal)convertedInBucket / quoteCount * 100m, 2);

            var itemsInBucket = itemRows.Count(r => r.CreateTime >= start && r.CreateTime < end);
            var ordersInBucket = orderRows.Where(r => r.CreateTime >= start && r.CreateTime < end).ToList();
            var purchaseOrderVendorCount = ordersInBucket
                .Where(o => !string.IsNullOrWhiteSpace(o.VendorId))
                .Select(o => o.VendorId!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            var approvedAmount = ordersInBucket
                .Where(o => o.Status >= PoApproved)
                .Sum(o => o.ConvertTotal);

            var linesInBucket = lineMetricRows.Where(r => r.CreateTime >= start && r.CreateTime < end).ToList();
            var stockInAmount = linesInBucket.Sum(r => r.StockInAmount);
            var paidAmount = linesInBucket.Sum(r => r.PaymentAmountFinish);
            var payableAmount = linesInBucket.Sum(r => r.PaymentAmountNot);

            result.Add(new PurchaseAnalyticsTrendPointDto
            {
                Period = period,
                QuoteItemCount = quoteCount,
                QuoteVendorCount = quoteVendorCount,
                PurchaseOrderItemCount = itemsInBucket,
                PurchaseOrderVendorCount = purchaseOrderVendorCount,
                PurchaseAmountApproved = scope.MaskAmounts ? null : approvedAmount,
                PurchaseAmountStockIn = scope.MaskAmounts ? null : stockInAmount,
                PurchaseAmountPaid = scope.MaskAmounts ? null : paidAmount,
                PayableAmount = scope.MaskAmounts ? null : payableAmount,
                QuoteToPurchaseConversionRate = rate
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        PurchaseAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default)
    {
        var userId = scope.Summary.UserId;
        var dateFrom = PurchaseAnalyticsDateFilter.ToUtcDateStart(scope.DateFrom);
        var dateEnd = PurchaseAnalyticsDateFilter.ToUtcDateEndExclusive(scope.DateTo);

        var orders = await BuildPurchaseOrderQueryAsync(userId, scope, cancellationToken);
        var ordersInPeriod = orders.Where(o => o.CreateTime >= dateFrom && o.CreateTime < dateEnd);

        var statusRows = await ordersInPeriod
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Amount = g.Sum(x => x.ConvertTotal) })
            .ToListAsync(cancellationToken);

        var statusItems = statusRows.Select(r => new SalesAnalyticsBreakdownItemDto
        {
            Key = r.Status.ToString(),
            Label = FormatOrderStatus(r.Status),
            Value = scope.MaskAmounts ? r.Count : r.Amount,
            Ratio = 0
        }).ToList();
        ApplyRatios(statusItems);

        var currencyRows = await ordersInPeriod
            .Where(o => o.Status >= PoApproved)
            .GroupBy(o => o.Currency)
            .Select(g => new { Currency = g.Key, Amount = g.Sum(x => x.ConvertTotal), Count = g.Count() })
            .ToListAsync(cancellationToken);

        var currencyItems = currencyRows.Select(r => new SalesAnalyticsBreakdownItemDto
        {
            Key = r.Currency.ToString(),
            Label = ((CurrencyCode)r.Currency).ToIsoText(),
            Value = scope.MaskAmounts ? r.Count : r.Amount,
            Ratio = 0
        }).ToList();
        ApplyRatios(currencyItems);

        var progressRows = await (
            from ext in _db.PurchaseOrderItemExtends.AsNoTracking()
            join oi in _db.PurchaseOrderItems.AsNoTracking() on ext.Id equals oi.Id
            join o in ordersInPeriod on oi.PurchaseOrderId equals o.Id
            where !oi.IsDeleted && !ext.IsDeleted && oi.Status != PoItemCancelled
            group ext by ext.StockInProgressStatus into g
            select new { Status = g.Key, Count = g.Count() }
        ).ToListAsync(cancellationToken);

        var progressItems = progressRows.Select(r => new SalesAnalyticsBreakdownItemDto
        {
            Key = r.Status.ToString(),
            Label = FormatStockInProgress(r.Status),
            Value = r.Count,
            Ratio = 0
        }).ToList();
        ApplyRatios(progressItems);

        var pipelineRows = await (
            from ext in _db.PurchaseOrderItemExtends.AsNoTracking()
            join oi in _db.PurchaseOrderItems.AsNoTracking() on ext.Id equals oi.Id
            join o in ordersInPeriod.Where(o => o.Status >= PoApproved) on oi.PurchaseOrderId equals o.Id
            where !oi.IsDeleted && !ext.IsDeleted && oi.Status != PoItemCancelled
            select new
            {
                ext.StockInProgressStatus,
                ext.PaymentProgressStatus,
                ext.InvoiceProgressStatus,
                ext.PaymentAmountNot,
                ext.PurchaseInvoiceToBe
            }
        ).ToListAsync(cancellationToken);

        var pipelineGroups = pipelineRows
            .GroupBy(r => PurchaseAnalyticsPipelineClassifier.Classify(
                r.StockInProgressStatus,
                r.PaymentProgressStatus,
                r.InvoiceProgressStatus,
                r.PaymentAmountNot,
                r.PurchaseInvoiceToBe))
            .Select(g => new SalesAnalyticsBreakdownItemDto
            {
                Key = g.Key,
                Label = PurchaseAnalyticsPipelineClassifier.Label(g.Key),
                Value = g.Count(),
                Ratio = 0
            })
            .OrderByDescending(x => x.Value)
            .ToList();
        ApplyRatios(pipelineGroups);

        return new List<SalesAnalyticsBreakdownGroupDto>
        {
            new() { GroupKey = "orderStatus", GroupLabel = "订单主状态", Items = statusItems },
            new() { GroupKey = "currency", GroupLabel = "币别金额（成单已审核）", Items = currencyItems },
            new() { GroupKey = "pipelineStage", GroupLabel = "全链路环节（明细行）", Items = pipelineGroups },
            new() { GroupKey = "stockInProgress", GroupLabel = "入库进度（明细行）", Items = progressItems }
        };
    }

    /// <inheritdoc />
    public async Task<PurchaseAnalyticsVendorDto> GetVendorAsync(
        PurchaseAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default)
    {
        var userId = scope.Summary.UserId;
        var dateFrom = PurchaseAnalyticsDateFilter.ToUtcDateStart(scope.DateFrom);
        var dateEnd = PurchaseAnalyticsDateFilter.ToUtcDateEndExclusive(scope.DateTo);
        var maskAmounts = scope.MaskAmounts;

        var orders = await BuildPurchaseOrderQueryAsync(userId, scope, cancellationToken);
        var approved = orders.Where(o =>
            o.CreateTime >= dateFrom && o.CreateTime < dateEnd
            && o.Status >= PoApproved);

        var orderRows = await approved
            .Select(o => new
            {
                o.VendorId,
                o.VendorName,
                o.ConvertTotal
            })
            .ToListAsync(cancellationToken);

        var vendorGroups = orderRows
            .Where(o => !string.IsNullOrWhiteSpace(o.VendorId))
            .GroupBy(o => o.VendorId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Count = g.Count(), Amount = g.Sum(x => x.ConvertTotal), Name = g.First().VendorName, Id = g.Key })
            .ToList();

        var snapshot = new PurchaseAnalyticsVendorSnapshotDto
        {
            ApprovedVendorCount = vendorGroups.Count,
            RepeatVendorCount = vendorGroups.Count(v => v.Count >= 2)
        };

        var vendorDimRows = await (
            from o in approved
            join v in _db.Vendors.AsNoTracking() on o.VendorId equals v.Id into vj
            from v in vj.DefaultIfEmpty()
            select new
            {
                o.ConvertTotal,
                VendorCredit = v != null ? v.Credit : (short?)null,
                VendorLevel = v != null ? v.Level : (short?)null,
                Industry = v != null ? v.Industry : null
            }
        ).ToListAsync(cancellationToken);

        var creditItems = BuildVendorDimensionBreakdown(
            vendorDimRows,
            r => r.VendorCredit?.ToString() ?? "_unset",
            r => r.VendorCredit.HasValue ? $"身份 {r.VendorCredit}" : "未设置",
            r => r.ConvertTotal,
            maskAmounts);
        var levelItems = BuildVendorDimensionBreakdown(
            vendorDimRows,
            r => r.VendorLevel?.ToString() ?? "_unset",
            r => r.VendorLevel.HasValue ? $"等级 {r.VendorLevel}" : "未设置",
            r => r.ConvertTotal,
            maskAmounts);
        var industryItems = BuildVendorDimensionBreakdown(
            vendorDimRows,
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
            ScopeContext = scope.ScopeContext,
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

    private async Task<PurchaseAnalyticsSnapshotDto> BuildSnapshotAsync(
        PurchaseAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var userId = scope.Summary.UserId;
        var dateFrom = PurchaseAnalyticsDateFilter.ToUtcDateStart(scope.DateFrom);
        var dateEnd = PurchaseAnalyticsDateFilter.ToUtcDateEndExclusive(scope.DateTo);

        if (BusinessDepartmentRules.UsePurchaseOrderAssistorOnlyScope(scope.Summary))
        {
            return await BuildSnapshotForAssistorOnlyAsync(scope, dateFrom, dateEnd, cancellationToken);
        }

        var quoteItems = await BuildScopedQuoteItemQueryAsync(userId, scope, cancellationToken);
        var quotesInPeriod = FilterQuoteItemsByPeriod(quoteItems, dateFrom, dateEnd);

        var quoteItemCount = await quotesInPeriod.CountAsync(cancellationToken);
        var quoteVendorCount = await quotesInPeriod
            .Where(i => i.VendorId != null)
            .Select(i => i.VendorId!)
            .Distinct()
            .CountAsync(cancellationToken);

        var quoteItemIds = await quotesInPeriod.Select(i => i.Id).ToListAsync(cancellationToken);
        var convertedCount = 0;
        if (quoteItemIds.Count > 0)
        {
            convertedCount = await BuildConvertedQuoteItemIdsQuery()
                .Where(id => quoteItemIds.Contains(id))
                .Distinct()
                .CountAsync(cancellationToken);
        }

        var orders = await BuildPurchaseOrderQueryAsync(userId, scope, cancellationToken);
        var ordersInPeriod = orders.Where(o => o.CreateTime >= dateFrom && o.CreateTime < dateEnd);

        var purchaseOrderItemCount = await (
            from oi in _db.PurchaseOrderItems.AsNoTracking()
            join o in ordersInPeriod on oi.PurchaseOrderId equals o.Id
            where !oi.IsDeleted && oi.Status != PoItemCancelled
            select oi.Id
        ).CountAsync(cancellationToken);

        var purchaseOrderVendorCount = await ordersInPeriod
            .Select(o => o.VendorId)
            .Distinct()
            .CountAsync(cancellationToken);

        var purchaseAmountApproved = await ordersInPeriod
            .Where(o => o.Status >= PoApproved)
            .SumAsync(o => (decimal?)o.ConvertTotal, cancellationToken) ?? 0m;

        var (purchaseAmountStockIn, purchaseAmountPaid) = await SumApprovedLineAmountsAsync(
            ordersInPeriod,
            cancellationToken);

        decimal? rate = quoteItemCount == 0
            ? null
            : Math.Round((decimal)convertedCount / quoteItemCount * 100m, 2);

        return new PurchaseAnalyticsSnapshotDto
        {
            QuoteItemCount = quoteItemCount,
            QuoteVendorCount = quoteVendorCount,
            QuoteToPurchaseConversionRate = rate,
            PurchaseOrderItemCount = purchaseOrderItemCount,
            PurchaseOrderVendorCount = purchaseOrderVendorCount,
            PurchaseAmountApproved = scope.MaskAmounts ? null : purchaseAmountApproved,
            PurchaseAmountStockIn = scope.MaskAmounts ? null : purchaseAmountStockIn,
            PurchaseAmountPaid = scope.MaskAmounts ? null : purchaseAmountPaid
        };
    }

    private async Task<(decimal StockInAmount, decimal PaidAmount)> SumApprovedLineAmountsAsync(
        IQueryable<PurchaseOrder> ordersInPeriod,
        CancellationToken cancellationToken)
    {
        var approvedOrders = ordersInPeriod.Where(o => o.Status >= PoApproved);

        var stockInAmount = await (
            from ext in _db.PurchaseOrderItemExtends.AsNoTracking()
            join oi in _db.PurchaseOrderItems.AsNoTracking() on ext.Id equals oi.Id
            join o in approvedOrders on oi.PurchaseOrderId equals o.Id
            where !oi.IsDeleted && !ext.IsDeleted && oi.Status != PoItemCancelled
            select ext.QtyReceiveTotal * oi.ConvertPrice
        ).SumAsync(cancellationToken);

        var paidAmount = await (
            from ext in _db.PurchaseOrderItemExtends.AsNoTracking()
            join oi in _db.PurchaseOrderItems.AsNoTracking() on ext.Id equals oi.Id
            join o in approvedOrders on oi.PurchaseOrderId equals o.Id
            where !oi.IsDeleted && !ext.IsDeleted && oi.Status != PoItemCancelled
            select ext.PaymentAmountFinish
        ).SumAsync(cancellationToken);

        return (stockInAmount, paidAmount);
    }

    private async Task<PurchaseAnalyticsSnapshotDto> BuildSnapshotForAssistorOnlyAsync(
        PurchaseAnalyticsResolvedScope scope,
        DateTime dateFrom,
        DateTime dateEnd,
        CancellationToken cancellationToken)
    {
        var orders = await BuildPurchaseOrderQueryAsync(scope.Summary.UserId, scope, cancellationToken);
        var ordersInPeriod = orders.Where(o => o.CreateTime >= dateFrom && o.CreateTime < dateEnd);

        var purchaseOrderItemCount = await (
            from oi in _db.PurchaseOrderItems.AsNoTracking()
            join o in ordersInPeriod on oi.PurchaseOrderId equals o.Id
            where !oi.IsDeleted && oi.Status != PoItemCancelled
            select oi.Id
        ).CountAsync(cancellationToken);

        var purchaseOrderVendorCount = await ordersInPeriod.Select(o => o.VendorId).Distinct().CountAsync(cancellationToken);
        var purchaseAmountApproved = await ordersInPeriod
            .Where(o => o.Status >= PoApproved)
            .SumAsync(o => (decimal?)o.ConvertTotal, cancellationToken) ?? 0m;

        var (purchaseAmountStockIn, purchaseAmountPaid) = await SumApprovedLineAmountsAsync(
            ordersInPeriod,
            cancellationToken);

        return new PurchaseAnalyticsSnapshotDto
        {
            QuoteItemCount = 0,
            QuoteVendorCount = 0,
            QuoteToPurchaseConversionRate = null,
            PurchaseOrderItemCount = purchaseOrderItemCount,
            PurchaseOrderVendorCount = purchaseOrderVendorCount,
            PurchaseAmountApproved = scope.MaskAmounts ? null : purchaseAmountApproved,
            PurchaseAmountStockIn = scope.MaskAmounts ? null : purchaseAmountStockIn,
            PurchaseAmountPaid = scope.MaskAmounts ? null : purchaseAmountPaid
        };
    }

    private async Task<PurchaseAnalyticsTodoDto> BuildTodoAsync(
        PurchaseAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var userId = scope.Summary.UserId;
        var orders = await BuildPurchaseOrderQueryAsync(userId, scope, cancellationToken);
        const short statusAuditFailed = -1;
        const short statusCancelled = -2;
        var activeOrders = orders.Where(o => o.Status != statusCancelled && o.Status != statusAuditFailed);

        var payable = await (
            from ext in _db.PurchaseOrderItemExtends.AsNoTracking()
            join oi in _db.PurchaseOrderItems.AsNoTracking() on ext.Id equals oi.Id
            join o in activeOrders on oi.PurchaseOrderId equals o.Id
            where !oi.IsDeleted && !ext.IsDeleted && oi.Status != PoItemCancelled
            select ext.PaymentAmountNot
        ).SumAsync(cancellationToken);

        var pendingStockIn = await (
            from ext in _db.PurchaseOrderItemExtends.AsNoTracking()
            join oi in _db.PurchaseOrderItems.AsNoTracking() on ext.Id equals oi.Id
            join o in activeOrders on oi.PurchaseOrderId equals o.Id
            where !oi.IsDeleted && !ext.IsDeleted && oi.Status != PoItemCancelled
                  && (ext.StockInProgressStatus == 0 || ext.StockInProgressStatus == 1)
            select oi.Id
        ).CountAsync(cancellationToken);

        return new PurchaseAnalyticsTodoDto
        {
            PayableAmount = scope.MaskAmounts ? null : payable,
            PendingStockInItemCount = pendingStockIn
        };
    }

    private async Task<SalesAnalyticsRankingsDto> BuildRankingsAsync(
        PurchaseAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var userId = scope.Summary.UserId;
        var dateFrom = PurchaseAnalyticsDateFilter.ToUtcDateStart(scope.DateFrom);
        var dateEnd = PurchaseAnalyticsDateFilter.ToUtcDateEndExclusive(scope.DateTo);

        var orders = await BuildPurchaseOrderQueryAsync(userId, scope, cancellationToken);
        var ordersInPeriod = orders.Where(o =>
            o.CreateTime >= dateFrom && o.CreateTime < dateEnd
            && o.Status >= PoApproved);

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
            .Select(o => new { o.PurchaseUserId, o.VendorId, o.VendorName, o.ConvertTotal })
            .ToListAsync(cancellationToken);

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Company)
        {
            var deptGroups = orderRows
                .GroupBy(o =>
                {
                    if (string.IsNullOrWhiteSpace(o.PurchaseUserId)) return PurchaseAnalyticsScopeValidator.UnassignedDepartmentId;
                    return primaryDeptMap.TryGetValue(o.PurchaseUserId, out var did) ? did : PurchaseAnalyticsScopeValidator.UnassignedDepartmentId;
                })
                .Select(g => new SalesAnalyticsRankingRowDto
                {
                    Id = g.Key,
                    Name = g.Key == PurchaseAnalyticsScopeValidator.UnassignedDepartmentId
                        ? "未分配部门"
                        : departments.GetValueOrDefault(g.Key, g.Key),
                    Amount = scope.MaskAmounts ? null : g.Sum(x => x.ConvertTotal),
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.Amount ?? x.OrderCount)
                .Take(RankingTopN)
                .ToList();

            return new SalesAnalyticsRankingsDto
            {
                Primary = deptGroups,
                Secondary = Array.Empty<SalesAnalyticsRankingRowDto>()
            };
        }

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Department)
        {
            var userGroups = orderRows
                .GroupBy(o => o.PurchaseUserId ?? PurchaseAnalyticsScopeValidator.UnassignedDepartmentId)
                .Select(g => new SalesAnalyticsRankingRowDto
                {
                    Id = g.Key,
                    Name = g.Key == PurchaseAnalyticsScopeValidator.UnassignedDepartmentId
                        ? "未分配采购员"
                        : users.GetValueOrDefault(g.Key, g.Key),
                    Amount = scope.MaskAmounts ? null : g.Sum(x => x.ConvertTotal),
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.Amount ?? x.OrderCount)
                .Take(RankingTopN)
                .ToList();

            return new SalesAnalyticsRankingsDto
            {
                Primary = userGroups,
                Secondary = Array.Empty<SalesAnalyticsRankingRowDto>()
            };
        }

        return new SalesAnalyticsRankingsDto
        {
            Primary = Array.Empty<SalesAnalyticsRankingRowDto>(),
            Secondary = Array.Empty<SalesAnalyticsRankingRowDto>()
        };
    }

    private async Task<IQueryable<PurchaseOrder>> BuildPurchaseOrderQueryAsync(
        string userId,
        PurchaseAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var q = _db.PurchaseOrders.AsNoTracking();
        q = PurchaseAnalyticsDateFilter.ApplyAnalyticsStatusFilter(q);
        q = await _dataPermission.ApplyPurchaseOrderDataScopeAsync(userId, q, cancellationToken);

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Personal
            && !BusinessDepartmentRules.UsePurchaseOrderAssistorOnlyScope(scope.Summary)
            && !string.IsNullOrWhiteSpace(scope.PurchaseUserId)
            && scope.Summary.PurchaseDataScope != 1)
        {
            q = q.Where(o => o.PurchaseUserId == scope.PurchaseUserId);
        }

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Department)
        {
            q = await ApplyDepartmentLensAsync(q, scope, cancellationToken);
        }

        return q;
    }

    private Task<IQueryable<PurchaseOrder>> ApplyDepartmentLensAsync(
        IQueryable<PurchaseOrder> q,
        PurchaseAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var deptId = scope.DepartmentId;
        if (string.IsNullOrWhiteSpace(deptId))
            deptId = scope.Summary.PrimaryDepartmentId;

        if (string.IsNullOrWhiteSpace(deptId))
            return Task.FromResult(q);

        if (string.Equals(deptId, PurchaseAnalyticsScopeValidator.UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase))
        {
            var withPrimary = _db.RbacUserDepartments.AsNoTracking()
                .Where(ud => ud.IsPrimary)
                .Select(ud => ud.UserId);
            return Task.FromResult(q.Where(o =>
                o.PurchaseUserId == null
                || !withPrimary.Contains(o.PurchaseUserId)));
        }

        var userIdsInDept = _db.RbacUserDepartments.AsNoTracking()
            .Where(ud => ud.IsPrimary && ud.DepartmentId == deptId)
            .Select(ud => ud.UserId);

        return Task.FromResult(q.Where(o => o.PurchaseUserId != null && userIdsInDept.Contains(o.PurchaseUserId)));
    }

    private async Task<IQueryable<QuoteItem>> BuildScopedQuoteItemQueryAsync(
        string userId,
        PurchaseAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        if (BusinessDepartmentRules.UsePurchaseOrderAssistorOnlyScope(scope.Summary))
            return _db.QuoteItems.Where(_ => false);

        var scopedQuoteIdList = await (await _dataPermission.ApplyQuoteListDataScopeAsync(
            userId,
            _db.Quotes.AsNoTracking(),
            _db.RFQs.AsNoTracking(),
            _db.RFQItems.AsNoTracking(),
            cancellationToken)).Select(q => q.Id).ToListAsync(cancellationToken);

        if (scopedQuoteIdList.Count == 0)
            return _db.QuoteItems.Where(_ => false);

        var q =
            from item in _db.QuoteItems.AsNoTracking()
            join quote in _db.Quotes.AsNoTracking() on item.QuoteId equals quote.Id
            where !item.IsDeleted && !quote.IsDeleted && item.Status != 1
                  && scopedQuoteIdList.Contains(item.QuoteId)
            select new { item, quote };

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Personal && !string.IsNullOrWhiteSpace(scope.PurchaseUserId))
        {
            var uid = scope.PurchaseUserId.Trim();
            q = q.Where(x =>
                (x.quote.PurchaseUserId != null && x.quote.PurchaseUserId == uid)
                || (x.quote.RFQItemId != null
                    && _db.RFQItems.AsNoTracking().Any(i =>
                        i.Id == x.quote.RFQItemId
                        && (i.AssignedPurchaserUserId1 == uid || i.AssignedPurchaserUserId2 == uid))));
        }

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Department)
        {
            var deptId = scope.DepartmentId ?? scope.Summary.PrimaryDepartmentId;
            if (!string.IsNullOrWhiteSpace(deptId))
            {
                if (string.Equals(deptId, PurchaseAnalyticsScopeValidator.UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase))
                {
                    var withPrimary = _db.RbacUserDepartments.AsNoTracking()
                        .Where(ud => ud.IsPrimary)
                        .Select(ud => ud.UserId);
                    q = q.Where(x =>
                        x.quote.PurchaseUserId == null
                        || !withPrimary.Contains(x.quote.PurchaseUserId));
                }
                else
                {
                    var userIdsInDept = _db.RbacUserDepartments.AsNoTracking()
                        .Where(ud => ud.IsPrimary && ud.DepartmentId == deptId)
                        .Select(ud => ud.UserId);
                    q = q.Where(x =>
                        (x.quote.PurchaseUserId != null && userIdsInDept.Contains(x.quote.PurchaseUserId))
                        || (x.quote.RFQItemId != null
                            && _db.RFQItems.AsNoTracking().Any(i =>
                                i.Id == x.quote.RFQItemId
                                && ((i.AssignedPurchaserUserId1 != null && userIdsInDept.Contains(i.AssignedPurchaserUserId1))
                                    || (i.AssignedPurchaserUserId2 != null && userIdsInDept.Contains(i.AssignedPurchaserUserId2))))));
                }
            }
        }

        return q.Select(x => x.item);
    }

    /// <summary>按报价明细 CreateTime；若明细时间缺失则回退 QuoteDate / Quote.CreateTime。</summary>
    private IQueryable<QuoteItem> FilterQuoteItemsByPeriod(
        IQueryable<QuoteItem> items,
        DateTime dateFrom,
        DateTime dateEnd) =>
        items.Where(i =>
            (i.CreateTime >= dateFrom && i.CreateTime < dateEnd)
            || _db.Quotes.AsNoTracking().Any(q =>
                q.Id == i.QuoteId
                && ((q.QuoteDate >= dateFrom && q.QuoteDate < dateEnd)
                    || (q.CreateTime >= dateFrom && q.CreateTime < dateEnd))));

    /// <summary>报价明细已生成采购订单明细（经销单行 extend.quote_item_id 关联）。</summary>
    private IQueryable<string> BuildConvertedQuoteItemIdsQuery() =>
        from ext in _db.SellOrderItemExtends.AsNoTracking()
        join poi in _db.PurchaseOrderItems.AsNoTracking() on ext.Id equals poi.SellOrderItemId
        where !poi.IsDeleted && poi.SellOrderItemId != null && ext.QuoteItemId != null
        select ext.QuoteItemId!;

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

        ApplyRatios(items);
        return items;
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
        1 => "新建",
        2 => "待审核",
        10 => "审核通过",
        20 => "待确认",
        30 => "已确认",
        50 => "进行中",
        100 => "采购完成",
        _ => $"状态{status}"
    };

    private static string FormatStockInProgress(short status) => status switch
    {
        0 => "待入库",
        1 => "部分入库",
        2 => "入库完成",
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
