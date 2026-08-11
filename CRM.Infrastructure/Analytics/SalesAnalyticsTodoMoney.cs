using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Utilities;

namespace CRM.Infrastructure.Analytics;

internal static class SalesAnalyticsTodoMoney
{
    public static SalesAnalyticsMoneyDto Empty(bool masked) =>
        new()
        {
            TotalUsd = masked ? null : 0m,
            ByCurrency = Array.Empty<SalesAnalyticsCurrencyAmountDto>()
        };

    public static SalesAnalyticsMoneyDto Build(
        IEnumerable<(decimal Local, short Currency, decimal Price, decimal ConvertPrice)> rows,
        FinanceExchangeRateDto rates,
        bool maskAmounts)
    {
        if (maskAmounts) return Empty(masked: true);

        var built = FinanceAnalyticsMoneyBuilder.Build(
            rows
                .Where(r => r.Local != 0m)
                .Select(r => FinanceAnalyticsMoneyBuilder.FromExtend(
                    r.Local,
                    r.Currency,
                    r.Price,
                    r.ConvertPrice,
                    rates.UsdToCny,
                    rates.UsdToHkd,
                    rates.UsdToEur)),
            maskAmounts: false);

        return Map(built);
    }

    /// <summary>已出库：原币 qty×price，美金 qty×convert_price（价比为 0 时美金用财务汇率）。</summary>
    public static SalesAnalyticsMoneyDto BuildStockOut(
        IEnumerable<(decimal Qty, decimal Price, decimal ConvertPrice, short Currency)> rows,
        FinanceExchangeRateDto rates,
        bool maskAmounts) =>
        Build(
            rows
                .Where(r => r.Qty != 0m)
                .Select(r => (r.Qty * r.Price, r.Currency, r.Price, r.ConvertPrice)),
            rates,
            maskAmounts);

    /// <summary>原币→USD：仅查询日财务汇率（无订单行价比时用）。应收待核销优先走 <see cref="Build"/>。</summary>
    public static SalesAnalyticsMoneyDto BuildFromLocal(
        IEnumerable<(decimal Local, short Currency)> rows,
        FinanceExchangeRateDto rates,
        bool maskAmounts)
    {
        if (maskAmounts) return Empty(masked: true);

        var built = FinanceAnalyticsMoneyBuilder.Build(
            rows
                .Where(r => r.Local != 0m)
                .Select(r => FinanceAnalyticsMoneyBuilder.FromLocal(
                    r.Local,
                    r.Currency,
                    rates.UsdToCny,
                    rates.UsdToHkd,
                    rates.UsdToEur)),
            maskAmounts: false);

        return Map(built);
    }

    private static SalesAnalyticsMoneyDto Map(FinanceAnalyticsMoneyDto built) =>
        new()
        {
            TotalUsd = built.TotalUsd,
            ByCurrency = built.ByCurrency
                .Select(c => new SalesAnalyticsCurrencyAmountDto
                {
                    Currency = c.Currency,
                    CurrencyLabel = c.CurrencyLabel,
                    Amount = c.Amount
                })
                .ToList()
        };
}
