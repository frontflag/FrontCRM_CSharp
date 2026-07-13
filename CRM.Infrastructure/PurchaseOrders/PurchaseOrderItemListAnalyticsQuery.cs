using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Utilities;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.PurchaseOrders;

public sealed partial class PurchaseOrderItemListQuery
{
    private const short ApprovedStatusThreshold = 10;
    private const short PaymentAuditFailed = -1;
    private const short PaymentCancelled = -2;

    /// <inheritdoc />
    public async Task<PurchaseOrderItemListAnalyticsDashboardDto> GetListAnalyticsDashboardAsync(
        PurchaseOrderItemListQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var approvedRows = await LoadApprovedLineRowsAsync(request, cancellationToken);
        var inStock = await LoadInStockMetricsAsync(request, approvedRows, cancellationToken);
        var payableLines = approvedRows.Where(r => r.PaymentAmountNot > 0m).ToList();

        var currencyLines = BuildCurrencyLines(
            approvedRows,
            r => CalcUsdLineTotal(r),
            maskAmounts);

        var payableCurrencyLines = BuildCurrencyLines(
            payableLines,
            r => PaymentNotToUsd(r),
            maskAmounts);

        return new PurchaseOrderItemListAnalyticsDashboardDto
        {
            Context = new PurchaseOrderItemListAnalyticsContextDto { MaskAmounts = maskAmounts },
            Snapshot = new PurchaseOrderItemListAnalyticsSnapshotDto
            {
                ApprovedVendorCount = approvedRows
                    .Where(r => !string.IsNullOrWhiteSpace(r.VendorId))
                    .Select(r => r.VendorId!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                ApprovedOrderCount = approvedRows
                    .Select(r => r.OrderId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                ApprovedLineCount = approvedRows.Count,
                ApprovedAmountUsd = maskAmounts ? null : approvedRows.Sum(r => CalcUsdLineTotal(r) ?? 0m),
                CurrencyLines = currencyLines,
                InStockVendorCount = inStock.VendorCount,
                InStockLineCount = inStock.LineCount,
                InStockAmountUsd = maskAmounts ? null : inStock.AmountUsd,
                MaxStockAgeDays = inStock.LineCount > 0 ? inStock.MaxAgeDays : null,
                PayableVendorCount = payableLines
                    .Where(r => !string.IsNullOrWhiteSpace(r.VendorId))
                    .Select(r => r.VendorId!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                PayableLineCount = payableLines.Count,
                PayableAmountUsd = maskAmounts ? null : payableLines.Sum(r => PaymentNotToUsd(r) ?? 0m),
                PayableCurrencyLines = payableCurrencyLines
            }
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PurchaseOrderItemListAnalyticsTrendPointDto>> GetListAnalyticsTrendsAsync(
        PurchaseOrderItemListQueryRequest request,
        string groupBy,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var rows = await LoadApprovedLineRowsAsync(request, cancellationToken);
        if (rows.Count == 0)
            return Array.Empty<PurchaseOrderItemListAnalyticsTrendPointDto>();

        var (dateFrom, dateToInclusive) = ResolveTrendDateBounds(
            request,
            rows.Min(r => r.OrderCreateTime),
            rows.Max(r => r.OrderCreateTime));
        var normalizedGroupBy = NormalizeGroupBy(groupBy);
        var periods = BuildPeriodKeys(dateFrom, dateToInclusive, normalizedGroupBy);
        var result = new List<PurchaseOrderItemListAnalyticsTrendPointDto>();

        foreach (var period in periods)
        {
            var (start, end) = ParsePeriodRange(period, normalizedGroupBy);
            var inBucket = rows.Where(r => r.OrderCreateTime >= start && r.OrderCreateTime < end).ToList();
            result.Add(new PurchaseOrderItemListAnalyticsTrendPointDto
            {
                Period = period,
                ApprovedOrderCount = inBucket.Select(r => r.OrderId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                ApprovedLineCount = inBucket.Count,
                ApprovedLineAmountUsd = maskAmounts ? null : inBucket.Sum(r => CalcUsdLineTotal(r) ?? 0m)
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetListAnalyticsBreakdownsAsync(
        PurchaseOrderItemListQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var allRows = await LoadAllLineRowsAsync(request, cancellationToken);
        var approvedRows = allRows.Where(IsApprovedRow).ToList();
        var paymentRequestIds = await LoadActivePaymentRequestItemIdsAsync(
            approvedRows.Select(r => r.ItemId).ToList(),
            cancellationToken);
        foreach (var row in approvedRows)
            row.HasActivePaymentRequest = paymentRequestIds.Contains(row.ItemId);

        var statusItems = allRows
            .GroupBy(r => r.ItemStatus)
            .Select(g => new SalesAnalyticsBreakdownItemDto
            {
                Key = g.Key.ToString(),
                Label = FormatItemStatus(g.Key),
                Value = maskAmounts ? g.Count() : g.Sum(r => CalcUsdLineTotal(r) ?? 0m),
                Ratio = 0
            })
            .ToList();
        ApplyRatios(statusItems);

        var paymentRequestItems = BuildBreakdownFromRows(
            approvedRows,
            r => r.HasActivePaymentRequest ? "1" : "0",
            r => r.HasActivePaymentRequest ? "已申请" : "待申请",
            r => maskAmounts ? 1m : CalcUsdLineTotal(r) ?? 0m,
            maskAmounts);

        var paymentItems = BuildProgressBreakdown(approvedRows, r => r.PaymentProgressStatus, "付款", maskAmounts);
        var purchaseItems = BuildProgressBreakdown(approvedRows, r => r.PurchaseProgressStatus, "采购", maskAmounts);
        var stockInItems = BuildProgressBreakdown(approvedRows, r => r.StockInProgressStatus, "入库", maskAmounts);
        var invoiceItems = BuildProgressBreakdown(approvedRows, r => r.InvoiceProgressStatus, "开票", maskAmounts);

        var currencyItems = BuildBreakdownFromRows(
            approvedRows,
            r => r.Currency.ToString(),
            r => ((CurrencyCode)r.Currency).ToIsoText(),
            r => maskAmounts ? 1m : CalcUsdLineTotal(r) ?? 0m,
            maskAmounts);

        var brandQtyItems = BuildBreakdownFromRows(
            approvedRows,
            r => string.IsNullOrWhiteSpace(r.Brand) ? "_unset" : r.Brand!.Trim(),
            r => string.IsNullOrWhiteSpace(r.Brand) ? "未设置" : r.Brand!.Trim(),
            r => r.Qty,
            maskAmounts: false,
            useSum: true);

        var brandAmountItems = BuildBreakdownFromRows(
            approvedRows,
            r => string.IsNullOrWhiteSpace(r.Brand) ? "_unset" : r.Brand!.Trim(),
            r => string.IsNullOrWhiteSpace(r.Brand) ? "未设置" : r.Brand!.Trim(),
            r => maskAmounts ? 1m : CalcUsdLineTotal(r) ?? 0m,
            maskAmounts);

        var dateCodeItems = BuildBreakdownFromRows(
            approvedRows,
            r => string.IsNullOrWhiteSpace(r.DateCode) ? "_unset" : r.DateCode!.Trim(),
            r => string.IsNullOrWhiteSpace(r.DateCode) ? "未设置" : r.DateCode!.Trim(),
            r => r.Qty,
            maskAmounts: false,
            useSum: true);

        var purchaseUserItems = BuildBreakdownFromRows(
            approvedRows,
            r => r.PurchaseUserId ?? "_unset",
            r => string.IsNullOrWhiteSpace(r.PurchaseUserName) ? "未分配采购员" : r.PurchaseUserName!,
            r => maskAmounts ? 1m : CalcUsdLineTotal(r) ?? 0m,
            maskAmounts);

        return new List<SalesAnalyticsBreakdownGroupDto>
        {
            new() { GroupKey = "itemStatus", GroupLabel = "明细主状态", Items = statusItems },
            new() { GroupKey = "paymentRequestProgress", GroupLabel = "申请付款状态（成单）", Items = paymentRequestItems },
            new() { GroupKey = "paymentProgress", GroupLabel = "付款进度（成单）", Items = paymentItems },
            new() { GroupKey = "purchaseProgress", GroupLabel = "采购进度（成单）", Items = purchaseItems },
            new() { GroupKey = "stockInProgress", GroupLabel = "入库进度（成单）", Items = stockInItems },
            new() { GroupKey = "invoiceProgress", GroupLabel = "开票进度（成单）", Items = invoiceItems },
            new() { GroupKey = "currency", GroupLabel = "币别构成（成单）", Items = currencyItems },
            new() { GroupKey = "brandQty", GroupLabel = "品牌数量（成单 Qty）", Items = brandQtyItems },
            new() { GroupKey = "brandAmount", GroupLabel = "品牌金额（成单 USD）", Items = brandAmountItems },
            new() { GroupKey = "dateCode", GroupLabel = "生产日期/DC（成单）", Items = dateCodeItems },
            new() { GroupKey = "purchaseUser", GroupLabel = "采购员（成单 USD）", Items = purchaseUserItems }
        };
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderItemListAnalyticsRankingsDto> GetListAnalyticsRankingsAsync(
        PurchaseOrderItemListQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        const int topN = 10;
        var rows = await LoadApprovedLineRowsAsync(request, cancellationToken);

        var vendorByAmount = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.VendorId))
            .GroupBy(r => r.VendorId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.First().VendorName ?? g.Key,
                Amount = maskAmounts ? null : g.Sum(r => CalcUsdLineTotal(r) ?? 0m),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.Amount ?? x.OrderCount)
            .Take(topN)
            .ToList();

        var pnByAmount = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.PN))
            .GroupBy(r => r.PN!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.First().Brand != null ? $"{g.Key} / {g.First().Brand}" : g.Key,
                Amount = maskAmounts ? null : g.Sum(r => CalcUsdLineTotal(r) ?? 0m),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.Amount ?? x.OrderCount)
            .Take(topN)
            .ToList();

        var pnByQty = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.PN))
            .GroupBy(r => r.PN!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.First().Brand != null ? $"{g.Key} / {g.First().Brand}" : g.Key,
                Amount = maskAmounts ? null : g.Sum(r => CalcUsdLineTotal(r) ?? 0m),
                OrderCount = (int)Math.Round(g.Sum(r => r.Qty), MidpointRounding.AwayFromZero)
            })
            .OrderByDescending(x => x.OrderCount)
            .Take(topN)
            .ToList();

        var brandByAmount = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Brand))
            .GroupBy(r => r.Brand!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.Key,
                Amount = maskAmounts ? null : g.Sum(r => CalcUsdLineTotal(r) ?? 0m),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.Amount ?? x.OrderCount)
            .Take(topN)
            .ToList();

        var brandByQty = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Brand))
            .GroupBy(r => r.Brand!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.Key,
                Amount = maskAmounts ? null : g.Sum(r => CalcUsdLineTotal(r) ?? 0m),
                OrderCount = (int)Math.Round(g.Sum(r => r.Qty), MidpointRounding.AwayFromZero)
            })
            .OrderByDescending(x => x.OrderCount)
            .Take(topN)
            .ToList();

        var purchaseUserByAmount = rows
            .GroupBy(r => r.PurchaseUserId ?? "_unset", StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.Key == "_unset"
                    ? "未分配采购员"
                    : (g.First().PurchaseUserName ?? g.Key),
                Amount = maskAmounts ? null : g.Sum(r => CalcUsdLineTotal(r) ?? 0m),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.Amount ?? x.OrderCount)
            .Take(topN)
            .ToList();

        return new PurchaseOrderItemListAnalyticsRankingsDto
        {
            VendorByAmount = vendorByAmount,
            PnByAmount = pnByAmount,
            PnByQty = pnByQty,
            BrandByAmount = brandByAmount,
            BrandByQty = brandByQty,
            PurchaseUserByAmount = purchaseUserByAmount
        };
    }

    private async Task<List<ItemLineAnalyticsRow>> LoadAllLineRowsAsync(
        PurchaseOrderItemListQueryRequest request,
        CancellationToken cancellationToken)
    {
        var filtered = await PurchaseOrderItemListFilter.BuildFilteredJoinQueryAsync(
            _db, _dataPermission, request, cancellationToken);
        return await ProjectLineRowsAsync(filtered, cancellationToken);
    }

    private async Task<List<ItemLineAnalyticsRow>> LoadApprovedLineRowsAsync(
        PurchaseOrderItemListQueryRequest request,
        CancellationToken cancellationToken)
    {
        var filtered = await PurchaseOrderItemListFilter.BuildFilteredJoinQueryAsync(
            _db, _dataPermission, request, cancellationToken);
        filtered = PurchaseOrderItemListFilter.ApplyApprovedFilter(filtered);
        return await ProjectLineRowsAsync(filtered, cancellationToken);
    }

    private async Task<List<ItemLineAnalyticsRow>> ProjectLineRowsAsync(
        IQueryable<PurchaseOrderItemLineJoin> filtered,
        CancellationToken cancellationToken)
    {
        return await filtered
            .Select(x => new ItemLineAnalyticsRow
            {
                ItemId = x.Item.Id,
                OrderId = x.Item.PurchaseOrderId,
                OrderStatus = x.Po.Status,
                OrderCreateTime = x.Po.CreateTime,
                VendorId = x.Item.VendorId,
                VendorName = x.Po.VendorName,
                PurchaseUserId = x.Po.PurchaseUserId,
                PurchaseUserName = x.Po.PurchaseUserName,
                PN = x.Item.PN,
                Brand = x.Item.Brand,
                DateCode = x.Item.DateCode,
                ItemStatus = x.Item.Status,
                Qty = x.Item.Qty,
                Cost = x.Item.Cost,
                Currency = x.Item.Currency,
                ConvertPrice = x.Item.ConvertPrice,
                PurchaseProgressStatus = x.Ext != null ? x.Ext.PurchaseProgressStatus : (short)0,
                StockInProgressStatus = x.Ext != null ? x.Ext.StockInProgressStatus : (short)0,
                PaymentProgressStatus = x.Ext != null ? x.Ext.PaymentProgressStatus : (short)0,
                InvoiceProgressStatus = x.Ext != null ? x.Ext.InvoiceProgressStatus : (short)0,
                PaymentAmountNot = x.Ext != null ? x.Ext.PaymentAmountNot : 0m
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<InStockMetrics> LoadInStockMetricsAsync(
        PurchaseOrderItemListQueryRequest request,
        List<ItemLineAnalyticsRow> approvedRows,
        CancellationToken cancellationToken)
    {
        if (approvedRows.Count == 0)
            return new InStockMetrics();

        var itemIds = approvedRows.Select(r => r.ItemId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var vendorByItem = approvedRows.ToDictionary(r => r.ItemId, r => r.VendorId, StringComparer.OrdinalIgnoreCase);

        var scopedStock = await _dataPermission.ApplyStockItemListDataScopeAsync(
            request.CurrentUserId,
            _db.Set<StockItem>().AsNoTracking(),
            _db.SellOrders.AsNoTracking(),
            _db.SellOrderItems.AsNoTracking(),
            _db.Customers.AsNoTracking(),
            cancellationToken);

        var stockRows = await (
            from si in scopedStock
            join sin in _db.Set<StockIn>().AsNoTracking() on si.StockInId equals sin.Id
            where si.QtyRepertory > 0
                && !si.IsDeleted
                && si.PurchaseOrderItemId != null
                && itemIds.Contains(si.PurchaseOrderItemId)
            select new
            {
                si.PurchaseOrderItemId,
                si.QtyRepertory,
                si.PurchasePriceUsd,
                sin.StockInDate
            }
        ).ToListAsync(cancellationToken);

        if (stockRows.Count == 0)
            return new InStockMetrics();

        var today = DateTime.UtcNow.Date;
        var lineIds = stockRows
            .Select(s => s.PurchaseOrderItemId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var vendors = lineIds
            .Select(id => vendorByItem.GetValueOrDefault(id))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        return new InStockMetrics
        {
            VendorCount = vendors,
            LineCount = lineIds.Count,
            AmountUsd = stockRows.Sum(s => s.QtyRepertory * s.PurchasePriceUsd),
            MaxAgeDays = stockRows.Max(s => (int)(today - s.StockInDate.Date).TotalDays)
        };
    }

    private async Task<HashSet<string>> LoadActivePaymentRequestItemIdsAsync(
        IReadOnlyList<string> itemIds,
        CancellationToken cancellationToken)
    {
        if (itemIds.Count == 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var activeIds = await _db.Set<FinancePaymentItem>().AsNoTracking()
            .Where(pi =>
                pi.PurchaseOrderItemId != null
                && itemIds.Contains(pi.PurchaseOrderItemId))
            .Join(
                _db.Set<FinancePayment>().AsNoTracking(),
                pi => pi.FinancePaymentId,
                p => p.Id,
                (pi, p) => new { pi.PurchaseOrderItemId, p.Status })
            .Where(x => x.Status != PaymentAuditFailed && x.Status != PaymentCancelled)
            .Select(x => x.PurchaseOrderItemId!)
            .Distinct()
            .ToListAsync(cancellationToken);

        return activeIds
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsApprovedRow(ItemLineAnalyticsRow r) =>
        r.OrderStatus >= ApprovedStatusThreshold && r.ItemStatus != -2;

    private static decimal? CalcUsdLineTotal(ItemLineAnalyticsRow r) =>
        Math.Round(r.Qty * r.ConvertPrice, 2, MidpointRounding.AwayFromZero);

    private static decimal? PaymentNotToUsd(ItemLineAnalyticsRow r)
    {
        if (r.PaymentAmountNot <= 0m) return null;
        if (r.Currency == (short)CurrencyCode.USD)
            return r.PaymentAmountNot;
        if (r.Cost != 0m && r.ConvertPrice != 0m)
            return Math.Round(r.PaymentAmountNot * r.ConvertPrice / r.Cost, 2, MidpointRounding.AwayFromZero);
        return null;
    }

    private static decimal OriginalLineAmount(ItemLineAnalyticsRow r) =>
        Math.Round(r.Qty * r.Cost, 2, MidpointRounding.AwayFromZero);

    private static List<PurchaseOrderListAnalyticsCurrencyLineDto> BuildCurrencyLines(
        IEnumerable<ItemLineAnalyticsRow> rows,
        Func<ItemLineAnalyticsRow, decimal?> usdSelector,
        bool maskAmounts)
    {
        if (maskAmounts) return new List<PurchaseOrderListAnalyticsCurrencyLineDto>();

        return rows
            .GroupBy(r => r.Currency)
            .Select(g => new PurchaseOrderListAnalyticsCurrencyLineDto
            {
                CurrencyKey = g.Key.ToString(),
                CurrencyLabel = ((CurrencyCode)g.Key).ToIsoText(),
                OriginalAmount = g.Sum(r => OriginalLineAmount(r)),
                UsdAmount = g.Sum(r => usdSelector(r) ?? 0m)
            })
            .OrderByDescending(x => x.UsdAmount ?? x.OriginalAmount ?? 0m)
            .ToList();
    }

    private static List<SalesAnalyticsBreakdownItemDto> BuildProgressBreakdown(
        List<ItemLineAnalyticsRow> rows,
        Func<ItemLineAnalyticsRow, short> statusSelector,
        string labelPrefix,
        bool maskAmounts)
    {
        var items = rows
            .GroupBy(r => statusSelector(r))
            .Select(g => new SalesAnalyticsBreakdownItemDto
            {
                Key = g.Key.ToString(),
                Label = FormatProgressStatus(g.Key, labelPrefix),
                Value = maskAmounts ? g.Count() : g.Sum(r => CalcUsdLineTotal(r) ?? 0m),
                Ratio = 0
            })
            .ToList();
        ApplyRatios(items);
        return items;
    }

    private static List<SalesAnalyticsBreakdownItemDto> BuildBreakdownFromRows(
        List<ItemLineAnalyticsRow> rows,
        Func<ItemLineAnalyticsRow, string> keySelector,
        Func<ItemLineAnalyticsRow, string> labelSelector,
        Func<ItemLineAnalyticsRow, decimal> valueSelector,
        bool maskAmounts,
        bool useSum = false)
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
                    Value = maskAmounts && !useSum ? g.Count() : g.Sum(valueSelector),
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

    private static string FormatItemStatus(short status) => status switch
    {
        1 => "新建",
        2 => "待审核",
        10 => "审核通过",
        20 => "待确认",
        30 => "已确认",
        40 => "已付款",
        50 => "已发货",
        60 => "已入库",
        100 => "采购完成",
        -1 => "审核失败",
        -2 => "取消",
        _ => $"状态{status}"
    };

    private static string FormatProgressStatus(short status, string prefix) => status switch
    {
        0 => $"{prefix}待处理",
        1 => $"{prefix}部分完成",
        2 => $"{prefix}完成",
        _ => $"{prefix}{status}"
    };

    private static (DateTime From, DateTime ToInclusive) ResolveTrendDateBounds(
        PurchaseOrderItemListQueryRequest request,
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

    private sealed class ItemLineAnalyticsRow
    {
        public string ItemId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public short OrderStatus { get; set; }
        public DateTime OrderCreateTime { get; set; }
        public string? VendorId { get; set; }
        public string? VendorName { get; set; }
        public string? PurchaseUserId { get; set; }
        public string? PurchaseUserName { get; set; }
        public string? PN { get; set; }
        public string? Brand { get; set; }
        public string? DateCode { get; set; }
        public short ItemStatus { get; set; }
        public decimal Qty { get; set; }
        public decimal Cost { get; set; }
        public short Currency { get; set; }
        public decimal ConvertPrice { get; set; }
        public short PurchaseProgressStatus { get; set; }
        public short StockInProgressStatus { get; set; }
        public short PaymentProgressStatus { get; set; }
        public short InvoiceProgressStatus { get; set; }
        public decimal PaymentAmountNot { get; set; }
        public bool HasActivePaymentRequest { get; set; }
    }

    private sealed class InStockMetrics
    {
        public int VendorCount { get; set; }
        public int LineCount { get; set; }
        public decimal AmountUsd { get; set; }
        public int MaxAgeDays { get; set; }
    }
}
