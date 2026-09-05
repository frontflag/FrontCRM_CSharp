using System.Globalization;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Analytics;

public sealed class FinanceAnalyticsQuery : IFinanceAnalyticsQuery
{
    private const short PoItemCancelled = -2;
    private const short PoApproved = 10;
    private const short PaymentComplete = 100;
    private const short ReceiptReceived = 3;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public FinanceAnalyticsQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    public async Task<FinanceAnalyticsDashboardDto> GetDashboardAsync(
        FinanceAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default)
    {
        var todo = await BuildTodoAsync(scope, cancellationToken);
        var completed = await BuildCompletedAsync(scope, cancellationToken);

        return new FinanceAnalyticsDashboardDto
        {
            ScopeContext = scope.ScopeContext,
            Todo = todo,
            Completed = completed
        };
    }

    public async Task<IReadOnlyList<FinanceAnalyticsTrendPointDto>> GetTrendsAsync(
        FinanceAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default)
    {
        var userId = scope.Summary.UserId;
        var dateFrom = SalesAnalyticsDateFilter.ToUtcDateStart(scope.DateFrom);
        var dateEnd = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(scope.DateTo);

        var paymentRows = await LoadPaymentTrendRowsAsync(userId, scope, dateFrom, dateEnd, cancellationToken);
        var receiptRows = await LoadReceiptTrendRowsAsync(userId, scope, dateFrom, dateEnd, cancellationToken);

        var periods = BuildPeriodKeys(dateFrom, scope.DateTo, scope.GroupBy);
        var result = new List<FinanceAnalyticsTrendPointDto>();

        foreach (var period in periods)
        {
            var (start, end) = ParsePeriodRange(period, scope.GroupBy);
            var paidRows = paymentRows.Where(r => r.Date >= start && r.Date < end).ToList();
            var receivedRows = receiptRows.Where(r => r.Date >= start && r.Date < end).ToList();

            result.Add(new FinanceAnalyticsTrendPointDto
            {
                Period = period,
                PaidAmount = BuildMoney(paidRows, scope),
                ReceivedAmount = BuildMoney(receivedRows, scope)
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        FinanceAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default)
    {
        var todo = await BuildTodoAsync(scope, cancellationToken);
        var groups = new List<SalesAnalyticsBreakdownGroupDto>
        {
            BuildCurrencyBreakdown("currency:payableAmount", "应付款原币构成", todo.PayableAmount, scope.MaskAmounts),
            BuildCurrencyBreakdown("currency:receivableAmount", "应收款原币构成", todo.ReceivableAmount, scope.MaskAmounts),
            BuildCurrencyBreakdown("currency:pendingPurchaseInvoiceAmount", "待开进项发票原币构成", todo.PendingPurchaseInvoiceAmount, scope.MaskAmounts),
            BuildCurrencyBreakdown("currency:pendingSellInvoiceAmount", "待开销项发票原币构成", todo.PendingSellInvoiceAmount, scope.MaskAmounts)
        };

        return groups;
    }

    private async Task<FinanceAnalyticsTodoDto> BuildTodoAsync(
        FinanceAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var poRows = await LoadPoExtendRowsAsync(scope, cancellationToken);
        var soRows = await LoadSoExtendRowsAsync(scope, cancellationToken);
        var receivableRows = await LoadReceivableRowsAsync(scope, cancellationToken);

        var payable = BuildMoney(
            poRows.Select(r => FinanceAnalyticsMoneyBuilder.FromExtend(
                r.PaymentAmountNot, r.Currency, r.Price, r.ConvertPrice,
                scope.UsdToCny, scope.UsdToHkd, scope.UsdToEur)),
            scope);

        var pendingPurchaseInvoice = BuildMoney(
            poRows.Select(r => FinanceAnalyticsMoneyBuilder.FromExtend(
                r.PurchaseInvoiceToBe, r.Currency, r.Price, r.ConvertPrice,
                scope.UsdToCny, scope.UsdToHkd, scope.UsdToEur)),
            scope);

        var pendingSellInvoice = BuildMoney(
            soRows.Select(r => FinanceAnalyticsMoneyBuilder.FromExtend(
                r.InvoiceAmountNot, r.Currency, r.Price, r.ConvertPrice,
                scope.UsdToCny, scope.UsdToHkd, scope.UsdToEur)),
            scope);

        var receivable = BuildMoney(
            receivableRows.Select(r => FinanceAnalyticsMoneyBuilder.FromExtend(
                r.VerifiedToBe, r.Currency, r.Price, r.ConvertPrice,
                scope.UsdToCny, scope.UsdToHkd, scope.UsdToEur)),
            scope);

        return new FinanceAnalyticsTodoDto
        {
            PayableAmount = payable,
            ReceivableAmount = receivable,
            PendingPurchaseInvoiceAmount = pendingPurchaseInvoice,
            PendingSellInvoiceAmount = pendingSellInvoice
        };
    }

    private async Task<FinanceAnalyticsCompletedDto> BuildCompletedAsync(
        FinanceAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var userId = scope.Summary.UserId;
        var dateFrom = SalesAnalyticsDateFilter.ToUtcDateStart(scope.DateFrom);
        var dateEnd = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(scope.DateTo);

        var paidRows = await LoadPaymentTrendRowsAsync(userId, scope, dateFrom, dateEnd, cancellationToken);
        var receivedRows = await LoadReceiptTrendRowsAsync(userId, scope, dateFrom, dateEnd, cancellationToken);
        var purchaseInvoiceRows = await LoadPurchaseInvoiceRowsAsync(userId, scope, dateFrom, dateEnd, cancellationToken);
        var sellInvoiceRows = await LoadSellInvoiceRowsAsync(userId, scope, dateFrom, dateEnd, cancellationToken);

        return new FinanceAnalyticsCompletedDto
        {
            PaidAmount = BuildMoney(paidRows, scope),
            ReceivedAmount = BuildMoney(receivedRows, scope),
            IssuedPurchaseInvoiceAmount = BuildMoney(purchaseInvoiceRows, scope),
            IssuedSellInvoiceAmount = BuildMoney(sellInvoiceRows, scope)
        };
    }

    private FinanceAnalyticsMoneyDto BuildMoney(IEnumerable<FinanceAnalyticsMoneyBuilder.Row> rows, FinanceAnalyticsResolvedScope scope) =>
        FinanceAnalyticsMoneyBuilder.Build(rows, scope.MaskAmounts);

    private async Task<List<PoExtendRow>> LoadPoExtendRowsAsync(
        FinanceAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var userId = scope.Summary.UserId;
        var orders = await BuildPurchaseOrderQueryAsync(userId, scope, cancellationToken);
        const short statusAuditFailed = -1;
        const short statusCancelled = -2;
        var activeOrders = orders.Where(o => o.Status != statusCancelled && o.Status != statusAuditFailed);

        var rows = await (
            from ext in _db.PurchaseOrderItemExtends.AsNoTracking()
            join oi in _db.PurchaseOrderItems.AsNoTracking() on ext.Id equals oi.Id
            join o in activeOrders on oi.PurchaseOrderId equals o.Id
            where !oi.IsDeleted && !ext.IsDeleted && oi.Status != PoItemCancelled
            select new PoExtendRow
            {
                PurchaseUserId = o.PurchaseUserId,
                Currency = o.Currency,
                Price = oi.Cost,
                ConvertPrice = oi.ConvertPrice,
                PaymentAmountNot = ext.PaymentAmountNot,
                PurchaseInvoiceToBe = ext.PurchaseInvoiceToBe,
                PaymentAmountFinish = ext.PaymentAmountFinish,
                PurchaseInvoiceDone = ext.PurchaseInvoiceDone
            }
        ).ToListAsync(cancellationToken);

        return rows.Where(r => PassesPurchaseAttributionLens(scope, r.PurchaseUserId)).ToList();
    }

    private async Task<List<SoExtendRow>> LoadSoExtendRowsAsync(
        FinanceAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var userId = scope.Summary.UserId;
        var orders = await BuildSellOrderQueryAsync(userId, scope, cancellationToken);
        var activeOrders = orders.Where(o =>
            o.Status != SellOrderMainStatus.Cancelled && o.Status != SellOrderMainStatus.AuditFailed);

        var rows = await (
            from ext in _db.SellOrderItemExtends.AsNoTracking()
            join oi in _db.SellOrderItems.AsNoTracking() on ext.Id equals oi.Id
            join o in activeOrders on oi.SellOrderId equals o.Id
            where !oi.IsDeleted && !ext.IsDeleted && oi.Status == 0
            select new SoExtendRow
            {
                SalesUserId = o.SalesUserId,
                Currency = o.Currency,
                Price = oi.Price,
                ConvertPrice = oi.ConvertPrice,
                InvoiceAmountNot = ext.InvoiceAmountNot,
                InvoiceAmountFinish = ext.InvoiceAmountFinish
            }
        ).ToListAsync(cancellationToken);

        return rows.Where(r => PassesSalesAttributionLens(scope, r.SalesUserId)).ToList();
    }

    private async Task<List<ReceivableRow>> LoadReceivableRowsAsync(
        FinanceAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var userId = scope.Summary.UserId;
        var q = _db.FinanceReceivables.AsNoTracking().Where(r => !r.IsDeleted);
        q = await _dataPermission.ApplyFinanceReceivableListDataScopeAsync(
            userId, q, _db.SellOrders.AsNoTracking(), cancellationToken);
        q = ApplyReceivableViewLens(q, scope);

        var rows = await (
            from r in q
            join oi in _db.SellOrderItems.AsNoTracking() on r.SellOrderItemId equals oi.Id into oiJoin
            from oi in oiJoin.DefaultIfEmpty()
            select new ReceivableRow
            {
                SalesUserId = r.SalesUserId,
                Currency = r.Currency,
                VerifiedToBe = r.VerifiedToBe,
                Price = oi != null ? oi.Price : 0m,
                ConvertPrice = oi != null ? oi.ConvertPrice : 0m
            }).ToListAsync(cancellationToken);

        return rows.Where(r => PassesSalesAttributionLens(scope, r.SalesUserId)).ToList();
    }

    private async Task<List<FinanceDocRow>> LoadPaymentTrendRowsAsync(
        string userId,
        FinanceAnalyticsResolvedScope scope,
        DateTime dateFrom,
        DateTime dateEnd,
        CancellationToken cancellationToken)
    {
        var vendors = _db.Vendors.AsNoTracking();
        var q = _db.FinancePayments.AsNoTracking().Where(p => !p.IsDeleted && p.Status == PaymentComplete);
        q = await _dataPermission.ApplyFinancePaymentListDataScopeAsync(userId, q, vendors, cancellationToken);
        q = ApplyPaymentViewLens(q, scope);
        q = q.Where(p => p.PaymentDate >= dateFrom && p.PaymentDate < dateEnd);

        var payments = await q
            .Select(p => new
            {
                p.Id,
                p.PaymentDate,
                Currency = (short)p.PaymentCurrency,
                p.PaymentAmount,
                p.VendorId
            })
            .ToListAsync(cancellationToken);

        if (scope.AccessMode == FinanceAnalyticsAccessModes.SalesPurchaseOnly)
        {
            var vendorIds = payments.Select(p => p.VendorId).Distinct().ToList();
            var vendorOwners = await vendors
                .Where(v => vendorIds.Contains(v.Id))
                .Select(v => new { v.Id, v.PurchaseUserId })
                .ToDictionaryAsync(x => x.Id, x => x.PurchaseUserId, StringComparer.OrdinalIgnoreCase, cancellationToken);

            payments = payments
                .Where(p => vendorOwners.TryGetValue(p.VendorId, out var owner)
                            && PassesPurchaseAttributionLens(scope, owner))
                .ToList();
        }

        payments = payments.Where(p => p.PaymentDate.HasValue).ToList();
        if (payments.Count == 0)
            return new List<FinanceDocRow>();

        var paymentIds = payments.Select(p => p.Id).ToList();
        var items = await (
            from item in _db.FinancePaymentItems.AsNoTracking()
            where paymentIds.Contains(item.FinancePaymentId) && item.PaymentAmount != 0m
            join oi in _db.PurchaseOrderItems.AsNoTracking() on item.PurchaseOrderItemId equals oi.Id into oiJoin
            from oi in oiJoin.DefaultIfEmpty()
            select new
            {
                item.FinancePaymentId,
                item.PaymentAmount,
                Price = oi != null ? oi.Cost : 0m,
                ConvertPrice = oi != null ? oi.ConvertPrice : 0m
            }).ToListAsync(cancellationToken);

        var itemsByPayment = items
            .GroupBy(i => i.FinancePaymentId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var rows = new List<FinanceDocRow>();
        foreach (var p in payments)
        {
            if (itemsByPayment.TryGetValue(p.Id, out var its) && its.Count > 0)
            {
                foreach (var it in its)
                {
                    rows.Add(new FinanceDocRow
                    {
                        Date = p.PaymentDate!.Value,
                        Currency = p.Currency,
                        LocalAmount = it.PaymentAmount,
                        Price = it.Price,
                        ConvertPrice = it.ConvertPrice
                    });
                }
            }
            else
            {
                rows.Add(new FinanceDocRow
                {
                    Date = p.PaymentDate!.Value,
                    Currency = p.Currency,
                    LocalAmount = p.PaymentAmount
                });
            }
        }

        return rows;
    }

    private async Task<List<FinanceDocRow>> LoadReceiptTrendRowsAsync(
        string userId,
        FinanceAnalyticsResolvedScope scope,
        DateTime dateFrom,
        DateTime dateEnd,
        CancellationToken cancellationToken)
    {
        var q = _db.FinanceReceipts.AsNoTracking().Where(r => !r.IsDeleted && r.Status == ReceiptReceived);
        q = await _dataPermission.ApplyFinanceReceiptListDataScopeAsync(
            userId,
            q,
            _db.SellOrders.AsNoTracking(),
            _db.FinanceReceiptItems.AsNoTracking(),
            cancellationToken);
        q = ApplyReceiptViewLens(q, scope);
        q = q.Where(r => r.ReceiptDate >= dateFrom && r.ReceiptDate < dateEnd);

        var receipts = await q
            .Select(r => new
            {
                r.Id,
                r.ReceiptDate,
                Currency = (short)r.ReceiptCurrency,
                r.ReceiptAmount,
                r.SalesUserId
            })
            .ToListAsync(cancellationToken);

        receipts = receipts
            .Where(r => r.ReceiptDate.HasValue && PassesSalesAttributionLens(scope, r.SalesUserId))
            .ToList();
        if (receipts.Count == 0)
            return new List<FinanceDocRow>();

        var receiptIds = receipts.Select(r => r.Id).ToList();
        var items = await (
            from item in _db.FinanceReceiptItems.AsNoTracking()
            where receiptIds.Contains(item.FinanceReceiptId) && item.ReceiptAmount != 0m
            join oi in _db.SellOrderItems.AsNoTracking() on item.SellOrderItemId equals oi.Id into oiJoin
            from oi in oiJoin.DefaultIfEmpty()
            select new
            {
                item.FinanceReceiptId,
                item.ReceiptAmount,
                item.ReceiptConvertAmount,
                Price = oi != null ? oi.Price : 0m,
                ConvertPrice = oi != null ? oi.ConvertPrice : 0m
            }).ToListAsync(cancellationToken);

        var itemsByReceipt = items
            .GroupBy(i => i.FinanceReceiptId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var rows = new List<FinanceDocRow>();
        foreach (var r in receipts)
        {
            if (itemsByReceipt.TryGetValue(r.Id, out var its) && its.Count > 0)
            {
                foreach (var it in its)
                {
                    rows.Add(new FinanceDocRow
                    {
                        Date = r.ReceiptDate!.Value,
                        Currency = r.Currency,
                        LocalAmount = it.ReceiptAmount,
                        Price = it.Price,
                        ConvertPrice = it.ConvertPrice,
                        // 收款明细已存折算美金时优先用（与财务统计 §4.2 一致）
                        UsdOverride = it.ReceiptConvertAmount > 0m ? it.ReceiptConvertAmount : null
                    });
                }
            }
            else
            {
                rows.Add(new FinanceDocRow
                {
                    Date = r.ReceiptDate!.Value,
                    Currency = r.Currency,
                    LocalAmount = r.ReceiptAmount
                });
            }
        }

        return rows;
    }

    private async Task<List<FinanceDocRow>> LoadPurchaseInvoiceRowsAsync(
        string userId,
        FinanceAnalyticsResolvedScope scope,
        DateTime dateFrom,
        DateTime dateEnd,
        CancellationToken cancellationToken)
    {
        var vendors = _db.Vendors.AsNoTracking();
        var q = _db.FinancePurchaseInvoices.AsNoTracking()
            .Where(i => !i.IsDeleted && i.RedInvoiceStatus == 0);
        q = await _dataPermission.ApplyFinancePurchaseInvoiceListDataScopeAsync(userId, q, vendors, cancellationToken);
        q = ApplyPurchaseInvoiceViewLens(q, scope);
        q = q.Where(i => i.InvoiceDate >= dateFrom && i.InvoiceDate < dateEnd);

        var invoices = await q
            .Select(i => new { i.Id, i.InvoiceDate, i.InvoiceAmount, i.VendorId })
            .ToListAsync(cancellationToken);

        if (scope.AccessMode == FinanceAnalyticsAccessModes.SalesPurchaseOnly)
        {
            var vendorIds = invoices.Select(i => i.VendorId).Distinct().ToList();
            var vendorOwners = await vendors
                .Where(v => vendorIds.Contains(v.Id))
                .Select(v => new { v.Id, v.PurchaseUserId })
                .ToDictionaryAsync(x => x.Id, x => x.PurchaseUserId, StringComparer.OrdinalIgnoreCase, cancellationToken);

            invoices = invoices
                .Where(i => vendorOwners.TryGetValue(i.VendorId, out var owner)
                            && PassesPurchaseAttributionLens(scope, owner))
                .ToList();
        }

        invoices = invoices.Where(i => i.InvoiceDate.HasValue).ToList();
        if (invoices.Count == 0)
            return new List<FinanceDocRow>();

        var invoiceIds = invoices.Select(i => i.Id).ToList();
        var invItems = await _db.FinancePurchaseInvoiceItems.AsNoTracking()
            .Where(x => invoiceIds.Contains(x.FinancePurchaseInvoiceId) && x.BillAmount != 0m)
            .Select(x => new { x.FinancePurchaseInvoiceId, x.BillAmount, x.StockInId })
            .ToListAsync(cancellationToken);

        var stockInIds = invItems
            .Where(x => !string.IsNullOrWhiteSpace(x.StockInId))
            .Select(x => x.StockInId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var stockInToPoi = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (stockInIds.Count > 0)
        {
            var stockLinks = await _db.StockItems.AsNoTracking()
                .Where(si => stockInIds.Contains(si.StockInId!)
                             && si.PurchaseOrderItemId != null
                             && si.PurchaseOrderItemId != "")
                .Select(si => new { si.StockInId, si.PurchaseOrderItemId })
                .ToListAsync(cancellationToken);
            foreach (var link in stockLinks)
            {
                if (string.IsNullOrWhiteSpace(link.StockInId) || string.IsNullOrWhiteSpace(link.PurchaseOrderItemId))
                    continue;
                stockInToPoi.TryAdd(link.StockInId.Trim(), link.PurchaseOrderItemId.Trim());
            }
        }

        var poiIds = stockInToPoi.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var poiById = poiIds.Count == 0
            ? new Dictionary<string, (decimal Cost, decimal ConvertPrice, short Currency)>(StringComparer.OrdinalIgnoreCase)
            : await _db.PurchaseOrderItems.AsNoTracking()
                .Where(oi => poiIds.Contains(oi.Id))
                .Select(oi => new { oi.Id, oi.Cost, oi.ConvertPrice, oi.Currency })
                .ToDictionaryAsync(
                    x => x.Id,
                    x => (x.Cost, x.ConvertPrice, x.Currency),
                    StringComparer.OrdinalIgnoreCase,
                    cancellationToken);

        var itemsByInvoice = invItems
            .GroupBy(x => x.FinancePurchaseInvoiceId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var rows = new List<FinanceDocRow>();
        foreach (var inv in invoices)
        {
            if (itemsByInvoice.TryGetValue(inv.Id, out var its) && its.Count > 0)
            {
                foreach (var it in its)
                {
                    decimal price = 0m, convertPrice = 0m;
                    short currency = (short)CurrencyCode.RMB;
                    if (!string.IsNullOrWhiteSpace(it.StockInId)
                        && stockInToPoi.TryGetValue(it.StockInId.Trim(), out var poiId)
                        && poiById.TryGetValue(poiId, out var poi))
                    {
                        price = poi.Cost;
                        convertPrice = poi.ConvertPrice;
                        if (poi.Currency != 0) currency = poi.Currency;
                    }

                    rows.Add(new FinanceDocRow
                    {
                        Date = inv.InvoiceDate!.Value,
                        Currency = currency,
                        LocalAmount = it.BillAmount,
                        Price = price,
                        ConvertPrice = convertPrice
                    });
                }
            }
            else
            {
                rows.Add(new FinanceDocRow
                {
                    Date = inv.InvoiceDate!.Value,
                    Currency = (short)CurrencyCode.RMB,
                    LocalAmount = inv.InvoiceAmount
                });
            }
        }

        return rows;
    }

    private async Task<List<FinanceDocRow>> LoadSellInvoiceRowsAsync(
        string userId,
        FinanceAnalyticsResolvedScope scope,
        DateTime dateFrom,
        DateTime dateEnd,
        CancellationToken cancellationToken)
    {
        var customers = _db.Customers.AsNoTracking();
        var q = _db.FinanceSellInvoices.AsNoTracking().Where(i => !i.IsDeleted);
        q = await _dataPermission.ApplyFinanceSellInvoiceListDataScopeAsync(userId, q, customers, cancellationToken);
        q = ApplySellInvoiceViewLens(q, scope);
        q = q.Where(i => i.MakeInvoiceDate >= dateFrom && i.MakeInvoiceDate < dateEnd);

        var invoices = await (
            from inv in q
            join c in customers on inv.CustomerId equals c.Id into cj
            from c in cj.DefaultIfEmpty()
            select new
            {
                inv.Id,
                inv.MakeInvoiceDate,
                inv.InvoiceTotal,
                Currency = (short)inv.Currency,
                SalesUserId = c != null ? c.SalesUserId : null
            }
        ).ToListAsync(cancellationToken);

        invoices = invoices
            .Where(i => i.MakeInvoiceDate.HasValue && PassesSalesAttributionLens(scope, i.SalesUserId))
            .ToList();
        if (invoices.Count == 0)
            return new List<FinanceDocRow>();

        var invoiceIds = invoices.Select(i => i.Id).ToList();
        var items = await (
            from item in _db.SellInvoiceItems.AsNoTracking()
            where invoiceIds.Contains(item.FinanceSellInvoiceId) && item.InvoiceTotal != 0m
            join ext in _db.StockOutItemExtends.AsNoTracking() on item.StockOutItemId equals ext.Id into extJoin
            from ext in extJoin.DefaultIfEmpty()
            join oi in _db.SellOrderItems.AsNoTracking() on ext.SellOrderItemId equals oi.Id into oiJoin
            from oi in oiJoin.DefaultIfEmpty()
            select new
            {
                item.FinanceSellInvoiceId,
                item.InvoiceTotal,
                ItemCurrency = (short)item.Currency,
                Price = oi != null ? oi.Price : 0m,
                ConvertPrice = oi != null ? oi.ConvertPrice : 0m,
                SoCurrency = oi != null ? oi.Currency : (short)0
            }).ToListAsync(cancellationToken);

        var itemsByInvoice = items
            .GroupBy(x => x.FinanceSellInvoiceId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var rows = new List<FinanceDocRow>();
        foreach (var inv in invoices)
        {
            if (itemsByInvoice.TryGetValue(inv.Id, out var its) && its.Count > 0)
            {
                foreach (var it in its)
                {
                    var currency = it.ItemCurrency != 0
                        ? it.ItemCurrency
                        : (it.SoCurrency != 0 ? it.SoCurrency : inv.Currency);
                    if (currency == 0) currency = (short)CurrencyCode.RMB;

                    rows.Add(new FinanceDocRow
                    {
                        Date = inv.MakeInvoiceDate!.Value,
                        Currency = currency,
                        LocalAmount = it.InvoiceTotal,
                        Price = it.Price,
                        ConvertPrice = it.ConvertPrice
                    });
                }
            }
            else
            {
                rows.Add(new FinanceDocRow
                {
                    Date = inv.MakeInvoiceDate!.Value,
                    Currency = inv.Currency != 0 ? inv.Currency : (short)CurrencyCode.RMB,
                    LocalAmount = inv.InvoiceTotal
                });
            }
        }

        return rows;
    }

    private FinanceAnalyticsMoneyDto BuildMoney(IEnumerable<FinanceDocRow> rows, FinanceAnalyticsResolvedScope scope) =>
        BuildMoney(rows.Select(r => r.ToMoneyRow(scope)), scope);

    private async Task<IQueryable<PurchaseOrder>> BuildPurchaseOrderQueryAsync(
        string userId,
        FinanceAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var q = _db.PurchaseOrders.AsNoTracking();
        q = PurchaseAnalyticsDateFilter.ApplyAnalyticsStatusFilter(q);
        q = await _dataPermission.ApplyPurchaseOrderDataScopeAsync(userId, q, cancellationToken);

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Personal
            && !BusinessDepartmentRules.UsePurchaseOrderAssistorOnlyScope(scope.Summary)
            && !string.IsNullOrWhiteSpace(scope.OwnerUserId)
            && scope.Summary.PurchaseDataScope != 1)
        {
            q = q.Where(o => o.PurchaseUserId == scope.OwnerUserId);
        }

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Department)
            q = await ApplyPurchaseDepartmentLensAsync(q, scope, cancellationToken);

        return q;
    }

    private async Task<IQueryable<SellOrder>> BuildSellOrderQueryAsync(
        string userId,
        FinanceAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var q = _db.SellOrders.AsNoTracking();
        q = SalesAnalyticsDateFilter.ApplyAnalyticsStatusFilter(q);
        q = await _dataPermission.ApplySellOrderDataScopeAsync(userId, q, cancellationToken);

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Personal
            && !BusinessDepartmentRules.UseSellOrderAssistorOnlyScope(scope.Summary)
            && !string.IsNullOrWhiteSpace(scope.OwnerUserId)
            && scope.Summary.SaleDataScope != 1)
        {
            q = q.Where(o => o.SalesUserId == scope.OwnerUserId);
        }

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Department)
            q = await ApplySalesDepartmentLensAsync(q, scope, cancellationToken);

        return q;
    }

    private Task<IQueryable<PurchaseOrder>> ApplyPurchaseDepartmentLensAsync(
        IQueryable<PurchaseOrder> q,
        FinanceAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var deptId = scope.DepartmentId ?? scope.Summary.PrimaryDepartmentId;
        if (string.IsNullOrWhiteSpace(deptId))
            return Task.FromResult(q);

        if (string.Equals(deptId, SalesAnalyticsScopeValidator.UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase))
        {
            var withPrimary = _db.RbacUserDepartments.AsNoTracking()
                .Where(ud => ud.IsPrimary)
                .Select(ud => ud.UserId);
            return Task.FromResult(q.Where(o =>
                o.PurchaseUserId == null || !withPrimary.Contains(o.PurchaseUserId)));
        }

        var userIdsInDept = _db.RbacUserDepartments.AsNoTracking()
            .Where(ud => ud.IsPrimary && ud.DepartmentId == deptId)
            .Select(ud => ud.UserId);

        return Task.FromResult(q.Where(o => o.PurchaseUserId != null && userIdsInDept.Contains(o.PurchaseUserId)));
    }

    private Task<IQueryable<SellOrder>> ApplySalesDepartmentLensAsync(
        IQueryable<SellOrder> q,
        FinanceAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var deptId = scope.DepartmentId ?? scope.Summary.PrimaryDepartmentId;
        if (string.IsNullOrWhiteSpace(deptId))
            return Task.FromResult(q);

        if (string.Equals(deptId, SalesAnalyticsScopeValidator.UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase))
        {
            var withPrimary = _db.RbacUserDepartments.AsNoTracking()
                .Where(ud => ud.IsPrimary)
                .Select(ud => ud.UserId);
            return Task.FromResult(q.Where(o =>
                o.SalesUserId == null || !withPrimary.Contains(o.SalesUserId)));
        }

        var userIdsInDept = _db.RbacUserDepartments.AsNoTracking()
            .Where(ud => ud.IsPrimary && ud.DepartmentId == deptId)
            .Select(ud => ud.UserId);

        return Task.FromResult(q.Where(o => o.SalesUserId != null && userIdsInDept.Contains(o.SalesUserId)));
    }

    private IQueryable<FinanceReceivable> ApplyReceivableViewLens(
        IQueryable<FinanceReceivable> q,
        FinanceAnalyticsResolvedScope scope)
    {
        if (scope.AccessMode == FinanceAnalyticsAccessModes.SalesPurchaseOnly
            || scope.ViewLevel == SalesAnalyticsViewLevels.Company)
            return q;

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Personal)
        {
            var uid = scope.OwnerUserId ?? scope.Summary.UserId;
            return q.Where(r => r.SalesUserId == uid);
        }

        var deptId = scope.DepartmentId ?? scope.Summary.PrimaryDepartmentId;
        if (string.IsNullOrWhiteSpace(deptId))
            return q;

        if (string.Equals(deptId, SalesAnalyticsScopeValidator.UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase))
        {
            var withPrimary = _db.RbacUserDepartments.AsNoTracking()
                .Where(ud => ud.IsPrimary)
                .Select(ud => ud.UserId);
            return q.Where(r => r.SalesUserId == null || !withPrimary.Contains(r.SalesUserId));
        }

        var userIdsInDept = _db.RbacUserDepartments.AsNoTracking()
            .Where(ud => ud.IsPrimary && ud.DepartmentId == deptId)
            .Select(ud => ud.UserId);

        return q.Where(r => r.SalesUserId != null && userIdsInDept.Contains(r.SalesUserId));
    }

    private IQueryable<FinancePayment> ApplyPaymentViewLens(IQueryable<FinancePayment> q, FinanceAnalyticsResolvedScope scope)
    {
        if (scope.AccessMode == FinanceAnalyticsAccessModes.SalesPurchaseOnly
            || scope.ViewLevel == SalesAnalyticsViewLevels.Company)
            return q;

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Personal)
        {
            var uid = scope.OwnerUserId ?? scope.Summary.UserId;
            return q.Where(p => p.CreateByUserId == uid);
        }

        var deptId = scope.DepartmentId ?? scope.Summary.PrimaryDepartmentId;
        if (string.IsNullOrWhiteSpace(deptId))
            return q;

        if (string.Equals(deptId, SalesAnalyticsScopeValidator.UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase))
        {
            var withPrimary = _db.RbacUserDepartments.AsNoTracking()
                .Where(ud => ud.IsPrimary)
                .Select(ud => ud.UserId);
            return q.Where(p => p.CreateByUserId == null || !withPrimary.Contains(p.CreateByUserId));
        }

        var userIdsInDept = _db.RbacUserDepartments.AsNoTracking()
            .Where(ud => ud.IsPrimary && ud.DepartmentId == deptId)
            .Select(ud => ud.UserId);
        return q.Where(p => p.CreateByUserId != null && userIdsInDept.Contains(p.CreateByUserId));
    }

    private IQueryable<FinanceReceipt> ApplyReceiptViewLens(IQueryable<FinanceReceipt> q, FinanceAnalyticsResolvedScope scope)
    {
        if (scope.AccessMode == FinanceAnalyticsAccessModes.SalesPurchaseOnly
            || scope.ViewLevel == SalesAnalyticsViewLevels.Company)
            return q;

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Personal)
        {
            var uid = scope.OwnerUserId ?? scope.Summary.UserId;
            return q.Where(r => r.CreateByUserId == uid);
        }

        var deptId = scope.DepartmentId ?? scope.Summary.PrimaryDepartmentId;
        if (string.IsNullOrWhiteSpace(deptId))
            return q;

        if (string.Equals(deptId, SalesAnalyticsScopeValidator.UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase))
        {
            var withPrimary = _db.RbacUserDepartments.AsNoTracking()
                .Where(ud => ud.IsPrimary)
                .Select(ud => ud.UserId);
            return q.Where(r => r.CreateByUserId == null || !withPrimary.Contains(r.CreateByUserId));
        }

        var userIdsInDept = _db.RbacUserDepartments.AsNoTracking()
            .Where(ud => ud.IsPrimary && ud.DepartmentId == deptId)
            .Select(ud => ud.UserId);
        return q.Where(r => r.CreateByUserId != null && userIdsInDept.Contains(r.CreateByUserId));
    }

    private IQueryable<FinancePurchaseInvoice> ApplyPurchaseInvoiceViewLens(
        IQueryable<FinancePurchaseInvoice> q,
        FinanceAnalyticsResolvedScope scope)
    {
        if (scope.AccessMode == FinanceAnalyticsAccessModes.SalesPurchaseOnly
            || scope.ViewLevel == SalesAnalyticsViewLevels.Company)
            return q;

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Personal)
        {
            var uid = scope.OwnerUserId ?? scope.Summary.UserId;
            return q.Where(i => i.CreateByUserId == uid);
        }

        var deptId = scope.DepartmentId ?? scope.Summary.PrimaryDepartmentId;
        if (string.IsNullOrWhiteSpace(deptId))
            return q;

        if (string.Equals(deptId, SalesAnalyticsScopeValidator.UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase))
        {
            var withPrimary = _db.RbacUserDepartments.AsNoTracking()
                .Where(ud => ud.IsPrimary)
                .Select(ud => ud.UserId);
            return q.Where(i => i.CreateByUserId == null || !withPrimary.Contains(i.CreateByUserId));
        }

        var userIdsInDept = _db.RbacUserDepartments.AsNoTracking()
            .Where(ud => ud.IsPrimary && ud.DepartmentId == deptId)
            .Select(ud => ud.UserId);
        return q.Where(i => i.CreateByUserId != null && userIdsInDept.Contains(i.CreateByUserId));
    }

    private IQueryable<FinanceSellInvoice> ApplySellInvoiceViewLens(
        IQueryable<FinanceSellInvoice> q,
        FinanceAnalyticsResolvedScope scope)
    {
        if (scope.AccessMode == FinanceAnalyticsAccessModes.SalesPurchaseOnly
            || scope.ViewLevel == SalesAnalyticsViewLevels.Company)
            return q;

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Personal)
        {
            var uid = scope.OwnerUserId ?? scope.Summary.UserId;
            return q.Where(i => i.CreateByUserId == uid);
        }

        var deptId = scope.DepartmentId ?? scope.Summary.PrimaryDepartmentId;
        if (string.IsNullOrWhiteSpace(deptId))
            return q;

        if (string.Equals(deptId, SalesAnalyticsScopeValidator.UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase))
        {
            var withPrimary = _db.RbacUserDepartments.AsNoTracking()
                .Where(ud => ud.IsPrimary)
                .Select(ud => ud.UserId);
            return q.Where(i => i.CreateByUserId == null || !withPrimary.Contains(i.CreateByUserId));
        }

        var userIdsInDept = _db.RbacUserDepartments.AsNoTracking()
            .Where(ud => ud.IsPrimary && ud.DepartmentId == deptId)
            .Select(ud => ud.UserId);
        return q.Where(i => i.CreateByUserId != null && userIdsInDept.Contains(i.CreateByUserId));
    }

    private bool PassesPurchaseAttributionLens(FinanceAnalyticsResolvedScope scope, string? purchaseUserId)
    {
        if (scope.AccessMode != FinanceAnalyticsAccessModes.SalesPurchaseOnly)
            return true;

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Personal)
            return string.Equals(purchaseUserId, scope.Summary.UserId, StringComparison.OrdinalIgnoreCase);

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Department)
        {
            return !string.IsNullOrWhiteSpace(purchaseUserId)
                   && scope.SalesPurchaseLensUserIds.Contains(purchaseUserId);
        }

        return true;
    }

    private bool PassesSalesAttributionLens(FinanceAnalyticsResolvedScope scope, string? salesUserId)
    {
        if (scope.AccessMode != FinanceAnalyticsAccessModes.SalesPurchaseOnly)
            return true;

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Personal)
            return string.Equals(salesUserId, scope.Summary.UserId, StringComparison.OrdinalIgnoreCase);

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Department)
        {
            return !string.IsNullOrWhiteSpace(salesUserId)
                   && scope.SalesPurchaseLensUserIds.Contains(salesUserId);
        }

        return true;
    }

    private static SalesAnalyticsBreakdownGroupDto BuildCurrencyBreakdown(
        string groupKey,
        string groupLabel,
        FinanceAnalyticsMoneyDto money,
        bool maskAmounts)
    {
        if (maskAmounts || money.ByCurrency.Count == 0)
        {
            return new SalesAnalyticsBreakdownGroupDto
            {
                GroupKey = groupKey,
                GroupLabel = groupLabel,
                Items = Array.Empty<SalesAnalyticsBreakdownItemDto>()
            };
        }

        var items = money.ByCurrency.Select(c => new SalesAnalyticsBreakdownItemDto
        {
            Key = c.Currency.ToString(),
            Label = c.CurrencyLabel,
            Value = c.Amount,
            Ratio = 0
        }).ToList();
        ApplyRatios(items);

        return new SalesAnalyticsBreakdownGroupDto
        {
            GroupKey = groupKey,
            GroupLabel = groupLabel,
            Items = items
        };
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

    private static List<string> BuildPeriodKeys(DateTime from, DateTime to, string groupBy)
    {
        var keys = new List<string>();
        var cursor = from;
        while (cursor <= to)
        {
            keys.Add(groupBy switch
            {
                "day" => cursor.ToString("yyyy-MM-dd"),
                "week" => $"{cursor:yyyy}-W{ISOWeek.GetWeekOfYear(cursor):D2}",
                _ => cursor.ToString("yyyy-MM")
            });

            cursor = groupBy switch
            {
                "day" => cursor.AddDays(1),
                "week" => cursor.AddDays(7),
                _ => cursor.AddMonths(1)
            };
        }

        return keys.Distinct().ToList();
    }

    private static (DateTime Start, DateTime End) ParsePeriodRange(string period, string groupBy)
    {
        if (groupBy == "day" && DateTime.TryParse(period, out var day))
            return (day, day.AddDays(1));

        if (groupBy == "week" && period.Contains("-W", StringComparison.Ordinal))
        {
            var parts = period.Split("-W", StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && int.TryParse(parts[0], out var year) && int.TryParse(parts[1], out var week))
            {
                var start = ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
                return (start, start.AddDays(7));
            }
        }

        if (DateTime.TryParse(period + "-01", out var monthStart))
            return (monthStart, monthStart.AddMonths(1));

        return (DateTime.UtcNow, DateTime.UtcNow.AddDays(1));
    }

    private sealed class PoExtendRow
    {
        public string? PurchaseUserId { get; init; }
        public short Currency { get; init; }
        public decimal Price { get; init; }
        public decimal ConvertPrice { get; init; }
        public decimal PaymentAmountNot { get; init; }
        public decimal PurchaseInvoiceToBe { get; init; }
        public decimal PaymentAmountFinish { get; init; }
        public decimal PurchaseInvoiceDone { get; init; }
    }

    private sealed class SoExtendRow
    {
        public string? SalesUserId { get; init; }
        public short Currency { get; init; }
        public decimal Price { get; init; }
        public decimal ConvertPrice { get; init; }
        public decimal InvoiceAmountNot { get; init; }
        public decimal InvoiceAmountFinish { get; init; }
    }

    private sealed class ReceivableRow
    {
        public string? SalesUserId { get; init; }
        public short Currency { get; init; }
        public decimal VerifiedToBe { get; init; }
        public decimal Price { get; init; }
        public decimal ConvertPrice { get; init; }
    }

    private sealed class FinanceDocRow
    {
        public DateTime Date { get; init; }
        public short Currency { get; init; }
        public decimal LocalAmount { get; init; }
        public decimal Price { get; init; }
        public decimal ConvertPrice { get; init; }
        /// <summary>已有折算美金（如收款明细 ReceiptConvertAmount）时直接使用。</summary>
        public decimal? UsdOverride { get; init; }

        public FinanceAnalyticsMoneyBuilder.Row ToMoneyRow(FinanceAnalyticsResolvedScope scope)
        {
            if (UsdOverride.HasValue)
            {
                return new FinanceAnalyticsMoneyBuilder.Row
                {
                    Currency = Currency,
                    LocalAmount = LocalAmount,
                    UsdAmount = Math.Round(UsdOverride.Value, 2, MidpointRounding.AwayFromZero)
                };
            }

            return FinanceAnalyticsMoneyBuilder.FromExtend(
                LocalAmount, Currency, Price, ConvertPrice,
                scope.UsdToCny, scope.UsdToHkd, scope.UsdToEur);
        }
    }
}
