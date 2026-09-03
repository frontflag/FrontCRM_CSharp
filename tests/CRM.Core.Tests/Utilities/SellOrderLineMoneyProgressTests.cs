using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class SellOrderLineMoneyProgressTests
{
    [Fact]
    public void ZeroPrice_NotShipped_StaysPending()
    {
        Assert.Equal(
            SellOrderLineMoneyProgress.Pending,
            SellOrderLineMoneyProgress.Compute(0m, 10m, 0m, 0m, outboundComplete: false));
    }

    [Fact]
    public void ZeroPrice_FullyShipped_IsComplete()
    {
        Assert.Equal(
            SellOrderLineMoneyProgress.Complete,
            SellOrderLineMoneyProgress.Compute(0m, 10m, 0m, 0m, outboundComplete: true));
    }

    [Fact]
    public void ZeroPrice_ZeroQty_DoesNotComplete()
    {
        Assert.Equal(
            SellOrderLineMoneyProgress.Pending,
            SellOrderLineMoneyProgress.Compute(0m, 0m, 0m, 0m, outboundComplete: true));
    }

    [Fact]
    public void PaidLine_FinishEqualsDue_IsComplete()
    {
        Assert.Equal(
            SellOrderLineMoneyProgress.Complete,
            SellOrderLineMoneyProgress.Compute(10m, 10m, 100m, 100m, outboundComplete: true));
    }

    [Fact]
    public void PaidLine_NothingReceived_IsPending()
    {
        Assert.Equal(
            SellOrderLineMoneyProgress.Pending,
            SellOrderLineMoneyProgress.Compute(10m, 10m, 100m, 0m, outboundComplete: true));
    }

    [Fact]
    public void PaidLine_PartialReceived_IsPartial()
    {
        Assert.Equal(
            SellOrderLineMoneyProgress.Partial,
            SellOrderLineMoneyProgress.Compute(10m, 10m, 100m, 30m, outboundComplete: true));
    }
}
