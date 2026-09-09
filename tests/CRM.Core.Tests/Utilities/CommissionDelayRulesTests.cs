using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public sealed class CommissionDelayRulesTests
{
    [Fact]
    public void Delay_zero_same_day_not_eligible()
    {
        var day = new DateOnly(2026, 4, 10);
        Assert.False(CommissionDelayRules.IsEligible(day, day, 0));
        Assert.True(CommissionDelayRules.IsEligible(day.AddDays(1), day, 0));
        Assert.Equal(day.AddDays(1), CommissionDelayRules.FirstEligibleDate(day, 0));
    }

    [Fact]
    public void Delay_three_requires_strictly_greater()
    {
        var receipt = new DateOnly(2026, 4, 10);
        Assert.False(CommissionDelayRules.IsEligible(receipt.AddDays(3), receipt, 3));
        Assert.True(CommissionDelayRules.IsEligible(receipt.AddDays(4), receipt, 3));
        Assert.Equal(receipt.AddDays(4), CommissionDelayRules.FirstEligibleDate(receipt, 3));
    }
}
