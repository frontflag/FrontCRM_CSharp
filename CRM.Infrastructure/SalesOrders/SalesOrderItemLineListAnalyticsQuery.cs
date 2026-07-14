using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.SalesOrders;

public sealed partial class SalesOrderItemLineListQuery
{
    private const short ApprovedStatusThreshold = (short)SellOrderMainStatus.Approved;
    private const short StockOutCompletedStatus = 2;

    /// <inheritdoc />
    public async Task<SalesOrderItemListAnalyticsDashboardDto> GetListAnalyticsDashboardAsync(
        SellOrderItemLineQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var approvedRows = await LoadApprovedLineRowsAsync(request, cancellationToken);
        var inStock = await LoadInStockMetricsAsync(request, approvedRows, cancellationToken);
        var outboundDates = await LoadEarliestOutboundDatesAsync(
            approvedRows.Where(r => r.ReceiptAmountNot > 0m).Select(r => r.ItemId).ToList(),
            cancellationToken);

        var today = DateTime.UtcNow.Date;
        var receivableLines = approvedRows
            .Where(r => r.ReceiptAmountNot > 0m && outboundDates.ContainsKey(r.ItemId))
            .ToList();

        var currencyLines = BuildCurrencyLines(
            approvedRows,
            r => CalcUsdLineTotal(r),
            maskAmounts);

        var receivableCurrencyLines = BuildCurrencyLines(
            receivableLines,
            r => ReceiptNotToUsd(r),
            maskAmounts);

        int? maxReceivableAge = null;
        if (receivableLines.Count > 0)
        {
            maxReceivableAge = receivableLines
                .Select(r =>
                {
                    var d = outboundDates[r.ItemId];
                    return d.HasValue ? (int)(today - d.Value.Date).TotalDays : 0;
                })
                .DefaultIfEmpty(0)
                .Max();
        }

        return new SalesOrderItemListAnalyticsDashboardDto
        {
            Context = new SalesOrderItemListAnalyticsContextDto { MaskAmounts = maskAmounts },
            Snapshot = new SalesOrderItemListAnalyticsSnapshotDto
            {
                ApprovedCustomerCount = approvedRows
                    .Where(r => !string.IsNullOrWhiteSpace(r.CustomerId))
                    .Select(r => r.CustomerId!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                ApprovedOrderCount = approvedRows
                    .Select(r => r.OrderId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                ApprovedLineCount = approvedRows.Count,
                ApprovedAmountUsd = maskAmounts ? null : approvedRows.Sum(r => CalcUsdLineTotal(r) ?? 0m),
                CurrencyLines = currencyLines,
                PurchaseProfitUsd = maskAmounts ? null : approvedRows.Sum(r => r.SalesProfitExpected),
                OutboundProfitUsd = maskAmounts ? null : approvedRows.Sum(r => r.ProfitOutBizUsd),
                InStockCustomerCount = inStock.CustomerCount,
                InStockLineCount = inStock.LineCount,
                InStockAmountUsd = maskAmounts ? null : inStock.AmountUsd,
                MaxStockAgeDays = inStock.LineCount > 0 ? inStock.MaxAgeDays : null,
                ReceivableCustomerCount = receivableLines
                    .Where(r => !string.IsNullOrWhiteSpace(r.CustomerId))
                    .Select(r => r.CustomerId!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                ReceivableLineCount = receivableLines.Count,
                ReceivableAmountUsd = maskAmounts ? null : receivableLines.Sum(r => ReceiptNotToUsd(r) ?? 0m),
                ReceivableCurrencyLines = receivableCurrencyLines,
                MaxReceivableAgeDays = receivableLines.Count > 0 ? maxReceivableAge : null
            }
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SalesOrderItemListAnalyticsTrendPointDto>> GetListAnalyticsTrendsAsync(
        SellOrderItemLineQueryRequest request,
        string groupBy,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var rows = await LoadApprovedLineRowsAsync(request, cancellationToken);
        if (rows.Count == 0)
            return Array.Empty<SalesOrderItemListAnalyticsTrendPointDto>();

        var (dateFrom, dateToInclusive) = ResolveTrendDateBounds(
            request,
            rows.Min(r => r.OrderCreateTime),
            rows.Max(r => r.OrderCreateTime));
        var normalizedGroupBy = NormalizeGroupBy(groupBy);
        var periods = BuildPeriodKeys(dateFrom, dateToInclusive, normalizedGroupBy);
        var result = new List<SalesOrderItemListAnalyticsTrendPointDto>();

        foreach (var period in periods)
        {
            var (start, end) = ParsePeriodRange(period, normalizedGroupBy);
            var inBucket = rows.Where(r => r.OrderCreateTime >= start && r.OrderCreateTime < end).ToList();
            result.Add(new SalesOrderItemListAnalyticsTrendPointDto
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
        SellOrderItemLineQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var allRows = await LoadAllLineRowsAsync(request, cancellationToken);
        var approvedRows = allRows.Where(IsApprovedRow).ToList();

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

        var currencyItems = BuildBreakdownFromRows(
            approvedRows,
            r => r.Currency.ToString(),
            r => ((CurrencyCode)r.Currency).ToIsoText(),
            r => maskAmounts ? 1m : CalcUsdLineTotal(r) ?? 0m,
            maskAmounts);

        var purchaseItems = BuildProgressBreakdown(approvedRows, r => r.PurchaseProgressStatus, "采购", maskAmounts);
        var stockInItems = BuildProgressBreakdown(approvedRows, r => r.StockInProgressStatus, "入库", maskAmounts);
        var notifyItems = BuildProgressBreakdown(approvedRows, r => r.StockOutNotifyProgressStatus, "出库通知", maskAmounts);
        var receiptItems = BuildProgressBreakdown(approvedRows, r => r.ReceiptProgressStatus, "收款", maskAmounts);
        var invoiceItems = BuildProgressBreakdown(approvedRows, r => r.InvoiceProgressStatus, "开票", maskAmounts);

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
            r => maskAmounts ? 1m : r.Qty,
            maskAmounts: false,
            useSum: true);

        var salesUserItems = BuildBreakdownFromRows(
            approvedRows,
            r => r.SalesUserId ?? "_unset",
            r => string.IsNullOrWhiteSpace(r.SalesUserName) ? "未分配销售员" : r.SalesUserName!,
            r => maskAmounts ? 1m : CalcUsdLineTotal(r) ?? 0m,
            maskAmounts);

        return new List<SalesAnalyticsBreakdownGroupDto>
        {
            new() { GroupKey = "itemStatus", GroupLabel = "明细主状态", Items = statusItems },
            new() { GroupKey = "purchaseProgress", GroupLabel = "采购进度（成单）", Items = purchaseItems },
            new() { GroupKey = "stockInProgress", GroupLabel = "入库进度（成单）", Items = stockInItems },
            new() { GroupKey = "stockOutNotifyProgress", GroupLabel = "出库通知进度（成单）", Items = notifyItems },
            new() { GroupKey = "receiptProgress", GroupLabel = "收款进度（成单）", Items = receiptItems },
            new() { GroupKey = "invoiceProgress", GroupLabel = "开票进度（成单）", Items = invoiceItems },
            new() { GroupKey = "currency", GroupLabel = "币别构成（成单）", Items = currencyItems },
            new() { GroupKey = "brandQty", GroupLabel = "品牌数量（成单 Qty）", Items = brandQtyItems },
            new() { GroupKey = "brandAmount", GroupLabel = "品牌金额（成单 USD）", Items = brandAmountItems },
            new() { GroupKey = "dateCode", GroupLabel = "生产日期/DC（成单）", Items = dateCodeItems },
            new() { GroupKey = "salesUser", GroupLabel = "销售员（成单 USD）", Items = salesUserItems }
        };
    }

    /// <inheritdoc />
    public async Task<SalesOrderItemListAnalyticsRankingsDto> GetListAnalyticsRankingsAsync(
        SellOrderItemLineQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        const int topN = 10;
        var rows = await LoadApprovedLineRowsAsync(request, cancellationToken);

        var customerByAmount = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.CustomerId))
            .GroupBy(r => r.CustomerId!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.First().CustomerName ?? g.Key,
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

        var salesUserByAmount = rows
            .GroupBy(r => r.SalesUserId ?? "_unset", StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.Key == "_unset"
                    ? "未分配销售员"
                    : (g.First().SalesUserName ?? g.Key),
                Amount = maskAmounts ? null : g.Sum(r => CalcUsdLineTotal(r) ?? 0m),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.Amount ?? x.OrderCount)
            .Take(topN)
            .ToList();

        return new SalesOrderItemListAnalyticsRankingsDto
        {
            CustomerByAmount = customerByAmount,
            PnByAmount = pnByAmount,
            PnByQty = pnByQty,
            BrandByAmount = brandByAmount,
            BrandByQty = brandByQty,
            SalesUserByAmount = salesUserByAmount
        };
    }

    private async Task<List<ItemLineAnalyticsRow>> LoadAllLineRowsAsync(
        SellOrderItemLineQueryRequest request,
        CancellationToken cancellationToken)
    {
        var filtered = await SalesOrderItemLineListFilter.BuildFilteredJoinQueryAsync(
            _db, _dataPermission, request, cancellationToken);
        return await ProjectLineRowsAsync(filtered, cancellationToken);
    }

    private async Task<List<ItemLineAnalyticsRow>> LoadApprovedLineRowsAsync(
        SellOrderItemLineQueryRequest request,
        CancellationToken cancellationToken)
    {
        var filtered = await SalesOrderItemLineListFilter.BuildFilteredJoinQueryAsync(
            _db, _dataPermission, request, cancellationToken);
        filtered = SalesOrderItemLineListFilter.ApplyApprovedFilter(filtered);
        return await ProjectLineRowsAsync(filtered, cancellationToken);
    }

    private async Task<List<ItemLineAnalyticsRow>> ProjectLineRowsAsync(
        IQueryable<SellOrderItemLineJoin> filtered,
        CancellationToken cancellationToken)
    {
        var raw = await (
            from x in filtered
            join ext in _db.SellOrderItemExtends.AsNoTracking().Where(e => !e.IsDeleted)
                on x.Item.Id equals ext.Id into extJoin
            from ext in extJoin.DefaultIfEmpty()
            select new ItemLineAnalyticsRow
            {
                ItemId = x.Item.Id,
                OrderId = x.Item.SellOrderId,
                OrderStatus = (short)x.So.Status,
                OrderCreateTime = x.So.CreateTime,
                CustomerId = x.So.CustomerId,
                CustomerName = x.So.CustomerName,
                SalesUserId = x.So.SalesUserId,
                SalesUserName = x.So.SalesUserName,
                PN = x.Item.PN,
                Brand = x.Item.Brand,
                DateCode = x.Item.DateCode,
                ItemStatus = x.Item.Status,
                Qty = x.Item.Qty,
                Price = x.Item.Price,
                Currency = x.Item.Currency,
                ConvertPrice = x.Item.ConvertPrice,
                PurchaseProgressStatus = ext != null ? ext.PurchaseProgressStatus : (short)0,
                StockInProgressStatus = ext != null ? ext.StockInProgressStatus : (short)0,
                StockOutProgressStatus = ext != null ? ext.StockOutProgressStatus : (short)0,
                QtyStockOutNotify = ext != null ? ext.QtyStockOutNotify : 0m,
                ReceiptProgressStatus = ext != null ? ext.ReceiptProgressStatus : (short)0,
                InvoiceProgressStatus = ext != null ? ext.InvoiceProgressStatus : (short)0,
                SalesProfitExpected = ext != null ? ext.SalesProfitExpected : 0m,
                ProfitOutBizUsd = ext != null ? ext.ProfitOutBizUsd : 0m,
                ReceiptAmountNot = ext != null ? ext.ReceiptAmountNot : 0m
            }
        ).ToListAsync(cancellationToken);

        foreach (var row in raw)
            row.StockOutNotifyProgressStatus = CalcNotifyProgress(row.QtyStockOutNotify, row.Qty);

        return raw;
    }

    private async Task<InStockMetrics> LoadInStockMetricsAsync(
        SellOrderItemLineQueryRequest request,
        List<ItemLineAnalyticsRow> approvedRows,
        CancellationToken cancellationToken)
    {
        if (approvedRows.Count == 0)
            return new InStockMetrics();

        var itemIds = approvedRows.Select(r => r.ItemId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var customerByItem = approvedRows.ToDictionary(r => r.ItemId, r => r.CustomerId, StringComparer.OrdinalIgnoreCase);

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
                && si.SellOrderItemId != null
                && itemIds.Contains(si.SellOrderItemId)
            select new
            {
                si.SellOrderItemId,
                si.QtyRepertory,
                si.SalesPriceUsd,
                sin.StockInDate
            }
        ).ToListAsync(cancellationToken);

        if (stockRows.Count == 0)
            return new InStockMetrics();

        var today = DateTime.UtcNow.Date;
        var lineIds = stockRows
            .Select(s => s.SellOrderItemId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var customers = lineIds
            .Select(id => customerByItem.GetValueOrDefault(id))
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var maxAge = stockRows.Max(s => (int)(today - s.StockInDate.Date).TotalDays);
        var amount = stockRows.Sum(s => s.QtyRepertory * (s.SalesPriceUsd ?? 0m));

        return new InStockMetrics
        {
            CustomerCount = customers,
            LineCount = lineIds.Count,
            AmountUsd = amount,
            MaxAgeDays = maxAge
        };
    }

    private async Task<Dictionary<string, DateTime?>> LoadEarliestOutboundDatesAsync(
        IReadOnlyList<string> itemIds,
        CancellationToken cancellationToken)
    {
        if (itemIds.Count == 0)
            return new Dictionary<string, DateTime?>(StringComparer.OrdinalIgnoreCase);

        var rows = await _db.Set<StockOut>().AsNoTracking()
            .Where(so =>
                so.SellOrderItemId != null
                && itemIds.Contains(so.SellOrderItemId)
                && so.StockOutDate != null
                && so.Status == StockOutCompletedStatus
                && (so.StockOutType == StockOutTypeCode.Sales || so.StockOutType == StockOutTypeCode.LegacySales))
            .GroupBy(so => so.SellOrderItemId!)
            .Select(g => new { ItemId = g.Key, FirstOut = g.Min(x => x.StockOutDate) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.ItemId, r => r.FirstOut, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsApprovedRow(ItemLineAnalyticsRow r) =>
        r.OrderStatus >= ApprovedStatusThreshold && r.ItemStatus == 0;

    private static short CalcNotifyProgress(decimal notifyQty, decimal qty)
    {
        if (notifyQty <= 0m) return 0;
        if (notifyQty + 1e-9m >= qty) return 2;
        return 1;
    }

    private static decimal? CalcUsdLineTotal(ItemLineAnalyticsRow r)
    {
        if (r.Currency == (short)CurrencyCode.USD)
            return Math.Round(r.Qty * r.ConvertPrice, 2, MidpointRounding.AwayFromZero);
        if (r.ConvertPrice != 0m)
            return Math.Round(r.Qty * r.ConvertPrice, 2, MidpointRounding.AwayFromZero);
        return null;
    }

    private static decimal? ReceiptNotToUsd(ItemLineAnalyticsRow r)
    {
        if (r.ReceiptAmountNot <= 0m) return null;
        if (r.Currency == (short)CurrencyCode.USD)
            return r.ReceiptAmountNot;
        if (r.Price != 0m && r.ConvertPrice != 0m)
            return Math.Round(r.ReceiptAmountNot * r.ConvertPrice / r.Price, 2, MidpointRounding.AwayFromZero);
        return null;
    }

    private static decimal OriginalLineAmount(ItemLineAnalyticsRow r) =>
        Math.Round(r.Qty * r.Price, 2, MidpointRounding.AwayFromZero);

    private static List<SalesOrderItemListAnalyticsCurrencyLineDto> BuildCurrencyLines(
        IEnumerable<ItemLineAnalyticsRow> rows,
        Func<ItemLineAnalyticsRow, decimal?> usdSelector,
        bool maskAmounts)
    {
        if (maskAmounts) return new List<SalesOrderItemListAnalyticsCurrencyLineDto>();

        return rows
            .GroupBy(r => r.Currency)
            .Select(g => new SalesOrderItemListAnalyticsCurrencyLineDto
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
        0 => "正常",
        1 => "已取消",
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
        SellOrderItemLineQueryRequest request,
        DateTime minCreateTime,
        DateTime maxCreateTime)
    {
        var from = request.OrderCreateStart?.Date ?? minCreateTime.Date;
        var to = request.OrderCreateEnd?.Date ?? maxCreateTime.Date;
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
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? SalesUserId { get; set; }
        public string? SalesUserName { get; set; }
        public string? PN { get; set; }
        public string? Brand { get; set; }
        public string? DateCode { get; set; }
        public short ItemStatus { get; set; }
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public short Currency { get; set; }
        public decimal ConvertPrice { get; set; }
        public short PurchaseProgressStatus { get; set; }
        public short StockInProgressStatus { get; set; }
        public short StockOutProgressStatus { get; set; }
        public decimal QtyStockOutNotify { get; set; }
        public short StockOutNotifyProgressStatus { get; set; }
        public short ReceiptProgressStatus { get; set; }
        public short InvoiceProgressStatus { get; set; }
        public decimal SalesProfitExpected { get; set; }
        public decimal ProfitOutBizUsd { get; set; }
        public decimal ReceiptAmountNot { get; set; }
    }

    private sealed class InStockMetrics
    {
        public int CustomerCount { get; set; }
        public int LineCount { get; set; }
        public decimal AmountUsd { get; set; }
        public int MaxAgeDays { get; set; }
    }
}
