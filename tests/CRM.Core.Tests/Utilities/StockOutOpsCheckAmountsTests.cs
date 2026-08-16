using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class StockOutOpsCheckAmountsTests
{
    [Fact]
    public void AmountsMatch_Equal_IsTrue()
    {
        Assert.True(StockOutOpsCheckAmounts.AmountsMatch(196920.00m, 196920.00m));
    }

    [Fact]
    public void AmountsMatch_OverByOneCent_IsFalse()
    {
        Assert.False(StockOutOpsCheckAmounts.AmountsMatch(196920.00m, 196920.01m));
    }

    [Fact]
    public void AmountsMatch_UnderByOneCent_IsFalse()
    {
        Assert.False(StockOutOpsCheckAmounts.AmountsMatch(196920.00m, 196919.99m));
    }

    [Fact]
    public void RoundAmount_AwayFromZero()
    {
        Assert.Equal(1.13m, StockOutOpsCheckAmounts.RoundAmount(1.125m));
    }
}
