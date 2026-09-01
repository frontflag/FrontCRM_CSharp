using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class VendorTradeCountRulesTests
{
    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(10, true)]
    [InlineData(100, true)]
    [InlineData(-1, false)]
    [InlineData(-2, false)]
    public void IsValidPaymentStatus_MatchesConfirmedScope(short status, bool expected)
    {
        Assert.Equal(expected, VendorTradeCountRules.IsValidPaymentStatus(status));
    }
}
