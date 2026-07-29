using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Customers;

/// <summary>
/// 客户首页 KPI：应收台账（VerifiedToBe→USD）与客单在库金额，与列表 quickFilter 同口径。
/// </summary>
public static class CustomerHomeKpiQuery
{
    public sealed record KpiAmount(decimal AmountUsd, int CustomerCount);

    /// <summary>
    /// 未结应收：<c>finance_receivable.VerifiedToBe &gt; 0</c>（全局软删过滤），
    /// 金额按查询日财务参数汇率折 USD（与财务应收看板一致）。
    /// </summary>
    public static async Task<KpiAmount> GetOpenReceivableAsync(
        ApplicationDbContext db,
        IFinanceExchangeRateService exchangeRateService,
        IReadOnlySet<string> allowedCustomerIds,
        CancellationToken cancellationToken = default)
    {
        if (allowedCustomerIds.Count == 0)
            return new KpiAmount(0m, 0);

        var rates = await exchangeRateService.GetCurrentAsync(cancellationToken);
        var rows = await db.FinanceReceivables.AsNoTracking()
            .Where(r => r.VerifiedToBe > 0m)
            .Select(r => new { r.CustomerId, r.VerifiedToBe, r.Currency })
            .ToListAsync(cancellationToken);

        var scoped = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.CustomerId)
                        && allowedCustomerIds.Contains(r.CustomerId))
            .ToList();

        if (scoped.Count == 0)
            return new KpiAmount(0m, 0);

        var amount = scoped.Sum(r => ToUsd(r.VerifiedToBe, r.Currency, rates));
        var customers = scoped
            .Select(r => r.CustomerId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        return new KpiAmount(Math.Round(amount, 2, MidpointRounding.AwayFromZero), customers);
    }

    /// <summary>
    /// 客单在库：StockType=客单、<c>QtyRepertory &gt; 0</c>，能挂到客户
    /// （行上 CustomerId，或经销售明细关联订单客户）；金额 <c>QtyRepertory × SalesPriceUsd</c>。
    /// </summary>
    public static async Task<KpiAmount> GetCustomerOrderInStockAsync(
        ApplicationDbContext db,
        IReadOnlySet<string> allowedCustomerIds,
        CancellationToken cancellationToken = default)
    {
        if (allowedCustomerIds.Count == 0)
            return new KpiAmount(0m, 0);

        var stockRows = await db.StockItems.AsNoTracking()
            .Where(si => si.QtyRepertory > 0
                         && si.StockType == StockInventoryTypeCodes.CustomerOrder)
            .Select(si => new
            {
                si.CustomerId,
                si.SellOrderItemId,
                si.QtyRepertory,
                si.SalesPriceUsd
            })
            .ToListAsync(cancellationToken);

        if (stockRows.Count == 0)
            return new KpiAmount(0m, 0);

        var needLookup = stockRows
            .Where(s => string.IsNullOrWhiteSpace(s.CustomerId)
                        && !string.IsNullOrWhiteSpace(s.SellOrderItemId))
            .Select(s => s.SellOrderItemId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Dictionary<string, string> itemToCustomer = new(StringComparer.OrdinalIgnoreCase);
        if (needLookup.Count > 0)
        {
            var pairs = await (
                from item in db.SellOrderItems.AsNoTracking()
                join so in db.SellOrders.AsNoTracking() on item.SellOrderId equals so.Id
                where needLookup.Contains(item.Id) && so.CustomerId != null && so.CustomerId != ""
                select new { ItemId = item.Id, so.CustomerId }
            ).ToListAsync(cancellationToken);

            foreach (var p in pairs)
            {
                if (!string.IsNullOrWhiteSpace(p.CustomerId))
                    itemToCustomer[p.ItemId] = p.CustomerId;
            }
        }

        decimal amount = 0m;
        var customerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var s in stockRows)
        {
            var cid = !string.IsNullOrWhiteSpace(s.CustomerId)
                ? s.CustomerId.Trim()
                : (s.SellOrderItemId != null
                   && itemToCustomer.TryGetValue(s.SellOrderItemId, out var fromSo)
                    ? fromSo
                    : null);

            if (string.IsNullOrWhiteSpace(cid) || !allowedCustomerIds.Contains(cid))
                continue;

            amount += s.QtyRepertory * (s.SalesPriceUsd ?? 0m);
            customerIds.Add(cid);
        }

        return new KpiAmount(Math.Round(amount, 2, MidpointRounding.AwayFromZero), customerIds.Count);
    }

    private static decimal ToUsd(decimal local, short currency, FinanceExchangeRateDto rates) =>
        Math.Round(
            ExchangeRateToUsdConverter.UnitLocalToUsd(
                local, currency, rates.UsdToCny, rates.UsdToHkd, rates.UsdToEur),
            2,
            MidpointRounding.AwayFromZero);
}
