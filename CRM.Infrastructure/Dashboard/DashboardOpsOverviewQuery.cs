using CRM.Core.Interfaces;
using CRM.Core.Models.Dashboard;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using CRM.Infrastructure.StockIns;
using CRM.Infrastructure.StockOuts;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Dashboard;

public sealed class DashboardOpsOverviewQuery : IDashboardOpsOverviewQuery
{
    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;
    private readonly IRbacService _rbacService;

    public DashboardOpsOverviewQuery(
        ApplicationDbContext db,
        IDataPermissionService dataPermission,
        IRbacService rbacService)
    {
        _db = db;
        _dataPermission = dataPermission;
        _rbacService = rbacService;
    }

    /// <inheritdoc />
    public async Task<DashboardLogisticsOverviewDto> GetLogisticsAsync(
        string? currentUserId,
        DashboardOpsDateRange range,
        CancellationToken cancellationToken = default)
    {
        var dateFrom = range.DateFrom.Date;
        var dateTo = range.DateTo.Date;
        if (dateTo < dateFrom)
            (dateFrom, dateTo) = (dateTo, dateFrom);

        var stockInDates = await LoadStockInItemDatesAsync(currentUserId, dateFrom, dateTo, cancellationToken);
        var stockOutDates = await LoadStockOutItemDatesAsync(currentUserId, dateFrom, dateTo, cancellationToken);
        var customsCount = await CountCustomsDeclarationItemsAsync(currentUserId, dateFrom, dateTo, cancellationToken);

        return new DashboardLogisticsOverviewDto
        {
            StockInItemCount = stockInDates.Count,
            StockOutItemCount = stockOutDates.Count,
            CustomsDeclarationItemCount = customsCount,
            StockInTrends = FillDaily(dateFrom, dateTo, stockInDates),
            StockOutTrends = FillDaily(dateFrom, dateTo, stockOutDates)
        };
    }

    /// <inheritdoc />
    public async Task<DashboardFinanceWriteOffOverviewDto> GetFinanceWriteOffsAsync(
        string? currentUserId,
        DashboardOpsDateRange range,
        DashboardFinanceWriteOffFlags flags,
        CancellationToken cancellationToken = default)
    {
        var dateFrom = range.DateFrom.Date;
        var dateTo = range.DateTo.Date;
        if (dateTo < dateFrom)
            (dateFrom, dateTo) = (dateTo, dateFrom);

        var fromUtc = SalesAnalyticsDateFilter.ToUtcDateStart(dateFrom);
        var endUtc = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(dateTo);

        int? purchase = null;
        int? receivable = null;
        int? sell = null;

        if (flags.IncludePurchaseInvoice)
            purchase = await CountPurchaseInvoiceWriteOffsAsync(currentUserId, fromUtc, endUtc, cancellationToken);
        if (flags.IncludeReceivable)
            receivable = await CountReceivableWriteOffsAsync(currentUserId, fromUtc, endUtc, cancellationToken);
        if (flags.IncludeSellInvoice)
            sell = await CountSellInvoiceWriteOffsAsync(currentUserId, fromUtc, endUtc, cancellationToken);

        return new DashboardFinanceWriteOffOverviewDto
        {
            PurchaseInvoiceWriteOffCount = purchase,
            ReceivableWriteOffCount = receivable,
            SellInvoiceWriteOffCount = sell
        };
    }

    private async Task<List<DateTime>> LoadStockInItemDatesAsync(
        string? currentUserId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken)
    {
        var headers = await StockInListFilter.BuildFilteredQueryAsync(
            _db,
            _dataPermission,
            new StockInQueryRequest
            {
                CurrentUserId = currentUserId,
                StockInDateStart = dateFrom,
                StockInDateEnd = dateTo
            },
            cancellationToken);

        return await (
            from i in _db.StockInItems.AsNoTracking()
            join h in headers on i.StockInId equals h.Id
            select h.StockInDate).ToListAsync(cancellationToken);
    }

    private async Task<List<DateTime>> LoadStockOutItemDatesAsync(
        string? currentUserId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken)
    {
        var q = await StockOutItemListFilter.BuildFilteredJoinQueryAsync(
            _db,
            _dataPermission,
            new StockOutItemListQuery
            {
                CurrentUserId = currentUserId,
                StockOutDateFrom = dateFrom,
                StockOutDateTo = dateTo
            },
            cancellationToken);

        var raw = await q.Select(x => x.Header.StockOutDate).ToListAsync(cancellationToken);
        return raw.Where(d => d.HasValue).Select(d => d!.Value).ToList();
    }

    private async Task<int> CountCustomsDeclarationItemsAsync(
        string? currentUserId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken)
    {
        var fromUtc = SalesAnalyticsDateFilter.ToUtcDateStart(dateFrom);
        var endUtc = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(dateTo);

        var decls = _db.CustomsDeclarations.AsNoTracking().Where(d => !d.IsDeleted);
        if (!string.IsNullOrWhiteSpace(currentUserId))
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(currentUserId.Trim());
            if (!CustomsModuleAccessRules.CanAccessModule(summary))
                return 0;
            if (!CustomsModuleAccessRules.BypassLogisticsDataScopeForCustomsList(summary))
            {
                decls = await _dataPermission.ApplyLogisticsCreatorUserScopeAsync(
                    currentUserId,
                    decls,
                    d => d.CreateByUserId,
                    cancellationToken);
            }
        }

        return await (
            from i in _db.CustomsDeclarationItems.AsNoTracking()
            join d in decls on i.DeclarationId equals d.Id
            where d.DeclareDate >= fromUtc && d.DeclareDate < endUtc
            select i.Id).CountAsync(cancellationToken);
    }

    private async Task<int> CountPurchaseInvoiceWriteOffsAsync(
        string? currentUserId,
        DateTime fromUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        var invoices = await _dataPermission.ApplyFinancePurchaseInvoiceListDataScopeAsync(
            currentUserId,
            _db.FinancePurchaseInvoices.AsNoTracking(),
            _db.Vendors.AsNoTracking(),
            cancellationToken);

        return await (
            from w in _db.FinancePurchaseInvoiceWriteOffs.AsNoTracking()
            join inv in invoices on w.FinancePurchaseInvoiceId equals inv.Id
            where w.CreateTime >= fromUtc && w.CreateTime < endUtc
            select w.Id).CountAsync(cancellationToken);
    }

    private async Task<int> CountReceivableWriteOffsAsync(
        string? currentUserId,
        DateTime fromUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        var receivables = _db.FinanceReceivables.AsNoTracking().Where(r => !r.IsDeleted);
        receivables = await _dataPermission.ApplyFinanceReceivableListDataScopeAsync(
            currentUserId,
            receivables,
            _db.SellOrders.AsNoTracking(),
            cancellationToken);

        return await (
            from w in _db.FinanceReceivableWriteOffs.AsNoTracking()
            join r in receivables on w.FinanceReceivableId equals r.Id
            where w.CreateTime >= fromUtc && w.CreateTime < endUtc
            select w.Id).CountAsync(cancellationToken);
    }

    private async Task<int> CountSellInvoiceWriteOffsAsync(
        string? currentUserId,
        DateTime fromUtc,
        DateTime endUtc,
        CancellationToken cancellationToken)
    {
        var invoices = await _dataPermission.ApplyFinanceSellInvoiceListDataScopeAsync(
            currentUserId,
            _db.FinanceSellInvoices.AsNoTracking(),
            _db.Customers.AsNoTracking(),
            cancellationToken);

        return await (
            from w in _db.FinanceSellInvoiceWriteOffs.AsNoTracking()
            join inv in invoices on w.FinanceSellInvoiceId equals inv.Id
            where w.CreateTime >= fromUtc && w.CreateTime < endUtc
            select w.Id).CountAsync(cancellationToken);
    }

    private static List<DashboardOpsTrendPointDto> FillDaily(
        DateTime dateFrom,
        DateTime dateTo,
        IReadOnlyList<DateTime> dates)
    {
        var counts = new Dictionary<DateTime, int>();
        foreach (var raw in dates)
        {
            if (raw.Year < 2000)
                continue;
            var key = raw.Date;
            counts[key] = counts.TryGetValue(key, out var n) ? n + 1 : 1;
        }

        var result = new List<DashboardOpsTrendPointDto>();
        for (var d = dateFrom.Date; d <= dateTo.Date; d = d.AddDays(1))
        {
            result.Add(new DashboardOpsTrendPointDto
            {
                Period = d.ToString("yyyy-MM-dd"),
                Count = counts.TryGetValue(d, out var n) ? n : 0
            });
        }

        return result;
    }
}
