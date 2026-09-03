using CRM.Core.Services;
using Xunit;

namespace CRM.Core.Tests.Services;

public class CustomsFeeCalculatorTests
{
    private readonly CustomsFeeCalculator _calc = new();

    [Fact]
    public void RecalculateAgencyFeeFromSnapshots_UsesExistingBaseAndNewRate()
    {
        var result = _calc.RecalculateAgencyFeeFromSnapshots(
            customsPaymentGoods: 8000m,
            dutyAmount: 800m,
            vatAmount: 1144m,
            otherFee: 50m,
            declareQty: 10,
            brokerAgencyRate: 1.025m);

        Assert.Equal(248.60m, result.CustomsAgencyFee);
        Assert.Equal(10242.60m, result.TotalValueTax);
        Assert.Equal(1024.26m, result.TaxIncludedUnitPrice);
    }

    [Fact]
    public void RecalculateAgencyFeeFromSnapshots_RateOne_ZeroAgencyFee()
    {
        var result = _calc.RecalculateAgencyFeeFromSnapshots(
            1000m, 100m, 143m, 0m, 2, 1m);

        Assert.Equal(0m, result.CustomsAgencyFee);
        Assert.Equal(1243m, result.TotalValueTax);
        Assert.Equal(621.5m, result.TaxIncludedUnitPrice);
    }

    [Fact]
    public void RecalculateAgencyFeeFromSnapshots_DoesNotTouchInspection()
    {
        var result = _calc.RecalculateAgencyFeeFromSnapshots(1000m, 0m, 0m, 20m, 1, 1.03m);
        Assert.Equal(30m, result.CustomsAgencyFee);
        Assert.Equal(1050m, result.TotalValueTax);
    }
}
