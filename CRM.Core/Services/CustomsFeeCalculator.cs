using CRM.Core.Interfaces;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public class CustomsFeeCalculator : ICustomsFeeCalculator
{
    public CustomsFeeLineResult CalculateLine(
        decimal originalPurchasePrice,
        short purchaseCurrency,
        decimal purchaseRatio,
        decimal exchangeRate,
        int declareQty,
        decimal dutyRate,
        decimal vatRate,
        decimal brokerAgencyRate,
        decimal otherFee,
        decimal inspectionFee,
        FinanceExchangeRateDto systemFx)
    {
        _ = inspectionFee;

        if (declareQty <= 0)
            throw new InvalidOperationException("申报数量须大于 0。");
        if (exchangeRate <= 0m)
            throw new InvalidOperationException("请填写报关汇率。");
        if (purchaseRatio <= 0m)
            throw new InvalidOperationException("采购系数无效。");
        if (brokerAgencyRate < 1m)
            throw new InvalidOperationException("报关代理费率无效。");

        var costUsdRaw = ExchangeRateToUsdConverter.UnitLocalToUsd(
            originalPurchasePrice,
            purchaseCurrency,
            systemFx.UsdToCny,
            systemFx.UsdToHkd,
            systemFx.UsdToEur);
        if (originalPurchasePrice > 0m && costUsdRaw <= 0m)
            throw new InvalidOperationException("无法将采购单价折合为美元，请检查采购币别与财务汇率。");

        var costUsd = Round6(costUsdRaw * purchaseRatio);
        var customsUsdPrice = costUsd;

        var qty = (decimal)declareQty;
        var customsPaymentGoods = Round2(costUsd * exchangeRate * qty);
        var dutyAmount = Round2(customsPaymentGoods * dutyRate);
        var vatAmount = Round2((customsPaymentGoods + dutyAmount) * vatRate);
        var agencyMargin = brokerAgencyRate - 1m;
        var customsAgencyFee = Round2((customsPaymentGoods + dutyAmount + vatAmount) * agencyMargin);
        var totalValueTax = Round2(customsPaymentGoods + dutyAmount + vatAmount + customsAgencyFee + otherFee);
        var taxIncludedUnitPrice = Round6(totalValueTax / qty);

        return new CustomsFeeLineResult
        {
            CostUsd = costUsd,
            CustomsUsdPrice = customsUsdPrice,
            CustomsPaymentGoods = customsPaymentGoods,
            DutyAmount = dutyAmount,
            VatAmount = vatAmount,
            CustomsAgencyFee = customsAgencyFee,
            TotalValueTax = totalValueTax,
            TaxIncludedUnitPrice = taxIncludedUnitPrice
        };
    }

    private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    private static decimal Round6(decimal v) => Math.Round(v, 6, MidpointRounding.AwayFromZero);
}
