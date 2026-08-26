using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public sealed class RfqDesignatedPurchaserRulesTests
{
    [Fact]
    public void EnsureEnabled_True_DoesNotThrow() =>
        RfqDesignatedPurchaserRules.EnsureEnabled(true);

    [Fact]
    public void EnsureEnabled_False_ThrowsNotEnabledMessage()
    {
        var ex = Assert.Throws<ArgumentException>(() => RfqDesignatedPurchaserRules.EnsureEnabled(false));
        Assert.Equal(RfqDesignatedPurchaserRules.NotEnabledMessage, ex.Message);
    }
}
