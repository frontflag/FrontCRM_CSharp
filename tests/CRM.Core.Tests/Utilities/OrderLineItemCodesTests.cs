using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class OrderLineItemCodesTests
{
    [Theory]
    [InlineData("STO00221", 1, "STO00221-1")]
    [InlineData("STO00221", 2, "STO00221-2")]
    [InlineData("STO00221", 0, "")]
    [InlineData("STO00221", -1, "")]
    [InlineData("", 1, "")]
    [InlineData("   ", 1, "")]
    [InlineData(null, 1, "")]
    public void StockOut_FollowsHeaderSeqRule(string? stockOutCode, int seq, string expected)
    {
        Assert.Equal(expected, OrderLineItemCodes.StockOut(stockOutCode, seq));
    }

    [Fact]
    public void StockOut_MatchesSellShape()
    {
        Assert.Equal("SO00238-1", OrderLineItemCodes.Sell("SO00238", 1));
        Assert.Equal("STO00221-1", OrderLineItemCodes.StockOut("STO00221", 1));
    }

    [Fact]
    public void RequireStockOut_ReturnsCode()
    {
        Assert.Equal("STO00221-1", OrderLineItemCodes.RequireStockOut("STO00221", 1));
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData("", 1)]
    [InlineData("STO00221", 0)]
    public void RequireStockOut_ThrowsWhenInvalid(string? stockOutCode, int seq)
    {
        Assert.Throws<InvalidOperationException>(() => OrderLineItemCodes.RequireStockOut(stockOutCode, seq));
    }
}
