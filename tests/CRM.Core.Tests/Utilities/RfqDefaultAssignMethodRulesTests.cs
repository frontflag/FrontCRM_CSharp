using CRM.Core.Constants;
using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public sealed class RfqDefaultAssignMethodRulesTests
{
    [Theory]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(5, true)]
    [InlineData(1, false)]
    [InlineData(4, false)]
    public void IsAllowed_ReflectsCreatableMethods(short code, bool expected) =>
        Assert.Equal(expected, RfqDefaultAssignMethodRules.IsAllowed(code));

    [Fact]
    public void Normalize_InvalidFallsBackToPurchaseQuotePriority() =>
        Assert.Equal(
            RfqAssignMethodCodes.PurchaseQuotePriority,
            RfqDefaultAssignMethodRules.Normalize(99));
}
