using CRM.Core.Constants;
using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class RiskAlertRulesTests
{
    [Fact]
    public void Zero_Threshold_Disables_And_Does_Not_Trigger()
    {
        Assert.False(RiskAlertRules.IsEnabled(0m));
        Assert.False(RiskAlertRules.IsEnabled(0));
        Assert.False(RiskAlertRules.ExceedsAmount(999m, 0m));
        Assert.False(RiskAlertRules.ExceedsDays(999, 0));
    }

    [Fact]
    public void Equal_Amount_Is_Not_Over_Threshold()
    {
        Assert.False(RiskAlertRules.ExceedsAmount(100m, 100m));
        Assert.True(RiskAlertRules.ExceedsAmount(100.01m, 100m));
    }

    [Fact]
    public void Equal_Days_Is_Not_Over_Threshold()
    {
        Assert.False(RiskAlertRules.ExceedsDays(90, 90));
        Assert.True(RiskAlertRules.ExceedsDays(91, 90));
    }

    [Fact]
    public void Missing_Date_Skips_Age()
    {
        var today = new DateOnly(2026, 9, 14);
        Assert.Equal(-1, RiskAlertRules.AgeDays(today, null));
        Assert.Equal(-1, RiskAlertRules.AgeDays(today, default(DateTime)));
        Assert.Equal(10, RiskAlertRules.AgeDays(today, new DateTime(2026, 9, 4)));
        Assert.Equal(0, RiskAlertRules.AgeDays(today, new DateTime(2026, 9, 14)));
    }

    [Fact]
    public void Normalize_Usd_Zero_And_Bounds()
    {
        Assert.True(RiskAlertRules.TryNormalizeUsd(null, out var z, out var e0));
        Assert.Equal(0m, z);
        Assert.Null(e0);
        Assert.True(RiskAlertRules.TryNormalizeUsd(0m, out var z2, out _));
        Assert.Equal(0m, z2);
        Assert.True(RiskAlertRules.TryNormalizeUsd(RiskAlertLimits.MaxUsd, out var max, out _));
        Assert.Equal(RiskAlertLimits.MaxUsd, max);
        Assert.False(RiskAlertRules.TryNormalizeUsd(-1m, out _, out var n));
        Assert.Contains("负", n);
        Assert.False(RiskAlertRules.TryNormalizeUsd(1_000_000_000m, out _, out var o));
        Assert.Contains("上限", o);
        Assert.False(RiskAlertRules.TryNormalizeUsd(10.001m, out _, out var s));
        Assert.Contains("两位", s);
    }

    [Fact]
    public void Normalize_Days_Zero_And_Bounds()
    {
        Assert.True(RiskAlertRules.TryNormalizeDays(null, out var z, out _));
        Assert.Equal(0, z);
        Assert.True(RiskAlertRules.TryNormalizeDays(90, out var d, out _));
        Assert.Equal(90, d);
        Assert.False(RiskAlertRules.TryNormalizeDays(-1, out _, out var n));
        Assert.Contains("负", n);
        Assert.False(RiskAlertRules.TryNormalizeDays(10000, out _, out var o));
        Assert.Contains("上限", o);
    }
}
