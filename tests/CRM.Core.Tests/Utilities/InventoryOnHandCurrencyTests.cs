using CRM.Core.Constants;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class InventoryOnHandCurrencyTests
{
    [Fact]
    public void OrderPresent_UsesRmbUsdEurHkdThenRest_AndNormalizesInvalid()
    {
        var ordered = InventoryOnHandCurrency.OrderPresent(new short[] { 4, 0, 2, 4, 99 });
        Assert.Equal(new short[] { (short)CurrencyCode.RMB, (short)CurrencyCode.USD, (short)CurrencyCode.HKD }, ordered);
    }

    [Fact]
    public void Normalize_MapsUnknownToRmb()
    {
        Assert.Equal((short)CurrencyCode.RMB, InventoryOnHandCurrency.Normalize(0));
        Assert.Equal((short)CurrencyCode.USD, InventoryOnHandCurrency.Normalize(2));
    }

    [Fact]
    public void UnitUsd_PrefersPositiveConvertPrice_ElsePurchasePriceUsd()
    {
        Assert.Equal(10m, InventoryOnHandCurrency.UnitUsd(10m, 3m));
        Assert.Equal(3m, InventoryOnHandCurrency.UnitUsd(0m, 3m));
        Assert.Equal(0m, InventoryOnHandCurrency.UnitUsd(0m, 0m));
    }

    [Fact]
    public void IsConvertedUsdKey_IgnoresNumericCurrencyKeys()
    {
        Assert.True(InventoryOnHandCurrency.IsConvertedUsdKey("usdConverted"));
        Assert.False(InventoryOnHandCurrency.IsConvertedUsdKey("2"));
        Assert.False(InventoryOnHandCurrency.IsConvertedUsdKey("1"));
    }
}
