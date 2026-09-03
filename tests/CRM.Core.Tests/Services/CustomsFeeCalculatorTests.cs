using CRM.Core.Services;
using CRM.Core.Utilities;
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

    [Fact]
    public void CalculateLineFromManualCostUsd_ComputesDownstreamFees()
    {
        var result = _calc.CalculateLineFromManualCostUsd(
            manualCostUsd: 10.5m,
            exchangeRate: 7.2m,
            declareQty: 100,
            dutyRate: 0.05m,
            vatRate: 0.13m,
            brokerAgencyRate: 1.03m,
            otherFee: 20m,
            inspectionFee: 0m);

        Assert.Equal(10.5m, result.CostUsd);
        Assert.Equal(7560m, result.CustomsPaymentGoods);
        Assert.Equal(378m, result.DutyAmount);
        Assert.Equal(1031.94m, result.VatAmount);
        Assert.Equal(269.10m, result.CustomsAgencyFee);
        Assert.Equal(9259.04m, result.TotalValueTax);
        Assert.Equal(92.5904m, result.TaxIncludedUnitPrice);
    }

    [Fact]
    public void CalculateLineFromManualCostUsd_RejectsNonPositive()
    {
        Assert.Throws<ArgumentException>(() => CustomsCostUsdRules.EnsureValid(0m));
    }
}
