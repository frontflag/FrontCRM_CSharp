using CRM.Core.Constants;
using CRM.Core.Models.Analytics;

namespace CRM.Core.Utilities;

/// <summary>财务看板金额聚合：原币分档 + USD 合计。</summary>
public static class FinanceAnalyticsMoneyBuilder
{
    public sealed class Row
    {
        public short Currency { get; init; }
        public decimal LocalAmount { get; init; }
        public decimal UsdAmount { get; init; }
    }

    public static FinanceAnalyticsMoneyDto Empty(bool maskAmounts) =>
        maskAmounts
            ? new FinanceAnalyticsMoneyDto { TotalUsd = null, ByCurrency = Array.Empty<FinanceAnalyticsCurrencyAmountDto>() }
            : new FinanceAnalyticsMoneyDto { TotalUsd = 0m, ByCurrency = Array.Empty<FinanceAnalyticsCurrencyAmountDto>() };

    public static FinanceAnalyticsMoneyDto Build(IEnumerable<Row> rows, bool maskAmounts)
    {
        if (maskAmounts)
            return Empty(true);

        var list = rows.Where(r => r.LocalAmount != 0m || r.UsdAmount != 0m).ToList();
        if (list.Count == 0)
            return Empty(false);

        var byCurrency = list
            .GroupBy(r => r.Currency)
            .Select(g => new FinanceAnalyticsCurrencyAmountDto
            {
                Currency = g.Key,
                CurrencyLabel = ((CurrencyCode)g.Key).ToIsoText(),
                Amount = Math.Round(g.Sum(x => x.LocalAmount), 2, MidpointRounding.AwayFromZero)
            })
            .OrderBy(x => x.Currency)
            .ToList();

        var totalUsd = Math.Round(list.Sum(x => x.UsdAmount), 2, MidpointRounding.AwayFromZero);
        return new FinanceAnalyticsMoneyDto { TotalUsd = totalUsd, ByCurrency = byCurrency };
    }

    public static Row FromExtend(
        decimal localAmount,
        short currency,
        decimal linePrice,
        decimal convertPrice,
        decimal usdToCny,
        decimal usdToHkd,
        decimal usdToEur) =>
        new()
        {
            Currency = currency,
            LocalAmount = localAmount,
            UsdAmount = ExtendAmountToUsd(localAmount, linePrice, convertPrice, currency, usdToCny, usdToHkd, usdToEur)
        };

    public static Row FromLocal(
        decimal localAmount,
        short currency,
        decimal usdToCny,
        decimal usdToHkd,
        decimal usdToEur) =>
        new()
        {
            Currency = currency,
            LocalAmount = localAmount,
            UsdAmount = ExchangeRateToUsdConverter.UnitLocalToUsd(localAmount, currency, usdToCny, usdToHkd, usdToEur)
        };

    public static decimal ExtendAmountToUsd(
        decimal localAmount,
        decimal linePrice,
        decimal convertPrice,
        short currency,
        decimal usdToCny,
        decimal usdToHkd,
        decimal usdToEur)
    {
        if (localAmount == 0m) return 0m;
        if (linePrice > 0m && convertPrice > 0m)
            return Math.Round(localAmount * convertPrice / linePrice, 2, MidpointRounding.AwayFromZero);
        return Math.Round(
            ExchangeRateToUsdConverter.UnitLocalToUsd(localAmount, currency, usdToCny, usdToHkd, usdToEur),
            2,
            MidpointRounding.AwayFromZero);
    }
}
