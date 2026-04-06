using CRM.Core.Constants;

namespace CRM.Core.Utilities
{
    /// <summary>
    /// 将本币单价折合为美元单价。汇率参数含义与财务参数中的 USD 基准汇率一致（见 <see cref="CRM.Core.Models.Finance.FinanceExchangeRateSetting"/>）：
    /// 为「1 USD 可兑换的外币数量」（如 UsdToCny=6.9194 表示 1 美元 = 6.9194 人民币）。
    /// </summary>
    public static class ExchangeRateToUsdConverter
    {
        public static decimal UnitLocalToUsd(
            decimal unitPrice,
            short currency,
            decimal usdToCny,
            decimal usdToHkd,
            decimal usdToEur)
        {
            if (unitPrice == 0m) return 0m;
            return (CurrencyCode)currency switch
            {
                CurrencyCode.USD => Round6(unitPrice),
                CurrencyCode.RMB => Div(unitPrice, usdToCny),
                CurrencyCode.EUR => Div(unitPrice, usdToEur),
                CurrencyCode.HKD => Div(unitPrice, usdToHkd),
                _ => 0m
            };
        }

        private static decimal Div(decimal price, decimal rate) =>
            rate > 0m ? Round6(price / rate) : 0m;

        private static decimal Round6(decimal v) =>
            Math.Round(v, 6, MidpointRounding.AwayFromZero);
    }
}
