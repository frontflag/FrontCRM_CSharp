using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class SellOrderSalesExpectedProfitCalcTests
{
    [Fact]
    public void Compute_UsesAllPoCost_WhenPoItemsExist_EvenIfZero()
    {
        var r = SellOrderSalesExpectedProfitCalc.Compute(
            revenueUsd: 1000m,
            lineQty: 100m,
            hasPoItems: true,
            poCostUsdTotal: 0m,
            stockingCovered: true,
            stockingUnitCostUsd: 5m,
            quoteConvertCost: 4m);

        Assert.Equal(SellOrderSalesExpectedCostSources.PurchaseOrder, r.CostSource);
        Assert.Equal(0m, r.CostUsd);
        Assert.Equal(1000m, r.ProfitUsd);
        Assert.Null(r.ProfitRate);
    }

    [Fact]
    public void Compute_UsesStocking_WhenNoPoAndCovered()
    {
        var r = SellOrderSalesExpectedProfitCalc.Compute(
            revenueUsd: 20880m,
            lineQty: 2000m,
            hasPoItems: false,
            poCostUsdTotal: 0m,
            stockingCovered: true,
            stockingUnitCostUsd: 10.20m,
            quoteConvertCost: 10.20m);

        Assert.Equal(SellOrderSalesExpectedCostSources.Stocking, r.CostSource);
        Assert.Equal(20400m, r.CostUsd);
        Assert.Equal(480m, r.ProfitUsd);
        Assert.NotNull(r.ProfitRate);
    }

    [Fact]
    public void Compute_UsesQuote_WhenNoPoAndStockingNotCovered()
    {
        var r = SellOrderSalesExpectedProfitCalc.Compute(
            revenueUsd: 1000m,
            lineQty: 100m,
            hasPoItems: false,
            poCostUsdTotal: 0m,
            stockingCovered: false,
            stockingUnitCostUsd: 0m,
            quoteConvertCost: 8m);

        Assert.Equal(SellOrderSalesExpectedCostSources.Quote, r.CostSource);
        Assert.Equal(800m, r.CostUsd);
        Assert.Equal(200m, r.ProfitUsd);
    }

    [Fact]
    public void Compute_ReturnsNone_WhenNoCostSource()
    {
        var r = SellOrderSalesExpectedProfitCalc.Compute(
            revenueUsd: 1000m,
            lineQty: 100m,
            hasPoItems: false,
            poCostUsdTotal: 0m,
            stockingCovered: false,
            stockingUnitCostUsd: 0m,
            quoteConvertCost: 0m);

        Assert.Equal(SellOrderSalesExpectedCostSources.None, r.CostSource);
        Assert.Null(r.ProfitUsd);
        Assert.Equal(0m, r.ProfitUsdForStorage);
    }

    [Fact]
    public void ResolveStockingUnitCost_PrefersOutbound_WhenFullyShipped()
    {
        var (covered, unit) = SellOrderSalesExpectedProfitCalc.ResolveStockingUnitCost(
            lineQty: 2000m,
            outboundQty: 2000m,
            outboundCostUsd: 20400m,
            stockingUsedQty: 500m,
            stockingPickCostUsd: 1000m);

        Assert.True(covered);
        Assert.Equal(10.20m, unit);
    }

    [Fact]
    public void ResolveStockingUnitCost_UsesPick_WhenOutboundPartial()
    {
        var (covered, unit) = SellOrderSalesExpectedProfitCalc.ResolveStockingUnitCost(
            lineQty: 2000m,
            outboundQty: 500m,
            outboundCostUsd: 5100m,
            stockingUsedQty: 2000m,
            stockingPickCostUsd: 20400m);

        Assert.True(covered);
        Assert.Equal(10.20m, unit);
    }

    [Fact]
    public void ResolveStockingUnitCost_NotCovered_WhenPartialStocking()
    {
        var (covered, _) = SellOrderSalesExpectedProfitCalc.ResolveStockingUnitCost(
            lineQty: 2000m,
            outboundQty: 1000m,
            outboundCostUsd: 10200m,
            stockingUsedQty: 1000m,
            stockingPickCostUsd: 10200m);

        Assert.False(covered);
    }
}
