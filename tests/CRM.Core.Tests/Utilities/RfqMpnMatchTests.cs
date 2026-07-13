using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public sealed class RfqMpnMatchTests
{
    [Theory]
    [InlineData(" bat54 ", "BAT54", true)]
    [InlineData("BAT54", "BAT54C", false)]
    [InlineData("", "X", false)]
    public void IsExactMatch_Works(string left, string right, bool expected) =>
        Assert.Equal(expected, RfqMpnMatch.IsExactMatch(left, right));
}
