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
}
