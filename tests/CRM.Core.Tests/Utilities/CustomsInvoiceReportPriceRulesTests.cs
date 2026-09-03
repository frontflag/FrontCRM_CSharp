using CRM.Core.Constants;
using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class CustomsInvoiceReportPriceRulesTests
{
    [Fact]
    public void SalesPacking_KeepsOrderCurrency()
    {
        var (price, currency) = CustomsInvoiceReportPriceRules.ResolveLine(
            StockOutTypeCode.Sales, 72m, 10.4m, (short)CurrencyCode.RMB);
        Assert.Equal(72m, price);
        Assert.Equal((short)CurrencyCode.RMB, currency);
    }

    [Fact]
    public void CustomsPacking_UsesConvertPriceAndUsd()
    {
        var (price, currency) = CustomsInvoiceReportPriceRules.ResolveLine(
            StockOutTypeCode.Customs, 72m, 10.4m, (short)CurrencyCode.RMB);
        Assert.Equal(10.4m, price);
        Assert.Equal((short)CurrencyCode.USD, currency);
    }

    [Fact]
    public void CustomsPacking_WithoutConvert_KeepsAmountAsUsd()
    {
        var (price, currency) = CustomsInvoiceReportPriceRules.ResolveLine(
            StockOutTypeCode.Customs, 72m, 0m, (short)CurrencyCode.RMB);
        Assert.Equal(72m, price);
        Assert.Equal((short)CurrencyCode.USD, currency);
    }
}
