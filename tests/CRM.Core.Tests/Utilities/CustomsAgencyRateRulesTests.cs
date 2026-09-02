using CRM.Core.Utilities;
using FluentAssertions;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class CustomsAgencyRateRulesTests
{
    [Fact]
    public void EnsureValid_allows_one_and_above()
    {
        CustomsAgencyRateRules.EnsureValid(1m);
        CustomsAgencyRateRules.EnsureValid(1.025m);
        CustomsAgencyRateRules.EnsureValid(10m);
    }

    [Fact]
    public void EnsureValid_rejects_below_one()
    {
        var act = () => CustomsAgencyRateRules.EnsureValid(0.999m);
        act.Should().Throw<ArgumentException>().WithParameterName("agencyRate");
    }

    [Fact]
    public void ResolveForCalculation_system_uses_broker_master()
    {
        CustomsAgencyRateRules.ResolveForCalculation(false, snapshotRate: 1.9m, brokerMasterRate: 1.025m)
            .Should().Be(1.025m);
    }

    [Fact]
    public void ResolveForCalculation_manual_keeps_snapshot()
    {
        CustomsAgencyRateRules.ResolveForCalculation(true, snapshotRate: 1.08m, brokerMasterRate: 1.025m)
            .Should().Be(1.08m);
    }

    [Fact]
    public void ResolveForCalculation_manual_rejects_invalid_snapshot()
    {
        var act = () => CustomsAgencyRateRules.ResolveForCalculation(true, snapshotRate: 0.5m, brokerMasterRate: 1.025m);
        act.Should().Throw<InvalidOperationException>();
    }
}
