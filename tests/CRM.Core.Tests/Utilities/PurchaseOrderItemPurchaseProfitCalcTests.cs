using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class PurchaseOrderItemPurchaseProfitCalcTests
{
    [Fact]
    public void Compute_Uses_Usd_Unit_Spread_Times_Purchase_Qty()
    {
        var profit = PurchaseOrderItemPurchaseProfitCalc.Compute(
            sellConvertUsdUnitPrice: 1.20m,
            purchaseConvertUsdUnitPrice: 0.80m,
            qty: 100m);
        Assert.Equal(40.00m, profit);
    }

    [Fact]
    public void Compute_Allows_Negative_When_Purchase_Higher()
    {
        var profit = PurchaseOrderItemPurchaseProfitCalc.Compute(0.50m, 0.80m, 10m);
        Assert.Equal(-3.00m, profit);
    }

    [Fact]
    public void Compute_Null_When_Sell_Missing()
    {
        Assert.Null(PurchaseOrderItemPurchaseProfitCalc.Compute(null, 1.0m, 5m));
        Assert.Null(PurchaseOrderItemPurchaseProfitCalc.Compute(0m, 1.0m, 5m));
    }

    [Fact]
    public void Compute_Null_When_Purchase_Missing()
    {
        Assert.Null(PurchaseOrderItemPurchaseProfitCalc.Compute(1.0m, null, 5m));
        Assert.Null(PurchaseOrderItemPurchaseProfitCalc.Compute(1.0m, 0m, 5m));
    }

    [Fact]
    public void Compute_Rounds_Away_From_Zero_To_Cents()
    {
        var profit = PurchaseOrderItemPurchaseProfitCalc.Compute(1.004m, 1.000m, 1m);
        Assert.Equal(0.00m, profit);
        var profit2 = PurchaseOrderItemPurchaseProfitCalc.Compute(1.006m, 1.000m, 1m);
        Assert.Equal(0.01m, profit2);
    }
}
