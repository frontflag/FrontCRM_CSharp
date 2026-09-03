using CRM.Core.Constants;

namespace CRM.Core.Utilities;

/// <summary>
/// 报关出库（美金段）Commercial Invoice：金额用销售折算美金价，币别固定 USD。
/// 销售装箱仍用订单原币 <c>Price</c> / <c>PriceCurrency</c>。
/// </summary>
public static class CustomsInvoiceReportPriceRules
{
    public static bool IsCustomsPacking(short stockOutType) =>
        StockOutTypeCode.NormalizeForNotify(stockOutType) == StockOutTypeCode.Customs;

    public static (decimal? Price, short? Currency) ResolveLine(
        short packingStockOutType,
        decimal? price,
        decimal? priceConvertPrice,
        short? priceCurrency)
    {
        if (!IsCustomsPacking(packingStockOutType))
            return (price, priceCurrency);

        var usdPrice = priceConvertPrice is > 0m ? priceConvertPrice : price;
        return (usdPrice, (short)CurrencyCode.USD);
    }
}
