using CRM.Core.Constants;
using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class IncentiveTargetRulesTests
{
    [Theory]
    [InlineData((short)2, (short)CommissionRoleType.Purchase)]
    [InlineData((short)3, (short)CommissionRoleType.Purchase)]
    [InlineData((short)1, (short)CommissionRoleType.Sales)]
    [InlineData((short)0, (short)CommissionRoleType.Sales)]
    [InlineData((short)4, (short)CommissionRoleType.Sales)]
    public void Role_Follows_Purchase_Primary_Identity(short identity, short expected)
    {
        Assert.Equal(expected, IncentiveTargetRules.RoleTypeFromIdentity(identity));
    }

    [Fact]
    public void Zero_Or_Null_Clears_Target()
    {
        Assert.True(IncentiveTargetRules.TryNormalizeTarget(null, out var a, out var e1));
        Assert.Null(a);
        Assert.Null(e1);
        Assert.True(IncentiveTargetRules.TryNormalizeTarget(0m, out var b, out var e2));
        Assert.Null(b);
        Assert.Null(e2);
    }

    [Fact]
    public void Min_And_Max_Succeed()
    {
        Assert.True(IncentiveTargetRules.TryNormalizeTarget(0.01m, out var min, out _));
        Assert.Equal(0.01m, min);
        Assert.True(IncentiveTargetRules.TryNormalizeTarget(IncentiveTargetLimits.MaxUsd, out var max, out _));
        Assert.Equal(IncentiveTargetLimits.MaxUsd, max);
    }

    [Fact]
    public void Negative_OverMax_And_Scale_Reject()
    {
        Assert.False(IncentiveTargetRules.TryNormalizeTarget(-1m, out _, out var n));
        Assert.Contains("负", n);
        Assert.False(IncentiveTargetRules.TryNormalizeTarget(100_000_000m, out _, out var o));
        Assert.Contains("上限", o);
        Assert.False(IncentiveTargetRules.TryNormalizeTarget(10.001m, out _, out var s));
        Assert.Contains("两位", s);
    }

    [Fact]
    public void Completion_Uses_Target_And_Allows_Over_100()
    {
        Assert.Null(IncentiveTargetRules.CompletionPct(100m, null));
        Assert.Null(IncentiveTargetRules.CompletionPct(100m, 0m));
        Assert.Equal(100.0m, IncentiveTargetRules.CompletionPct(100m, 100m));
        Assert.Equal(40.0m, IncentiveTargetRules.CompletionPct(3200.50m, 8000m));
        Assert.Equal(150.0m, IncentiveTargetRules.CompletionPct(150m, 100m));
        Assert.Equal(0.0m, IncentiveTargetRules.CompletionPct(0m, 100m));
    }
}
