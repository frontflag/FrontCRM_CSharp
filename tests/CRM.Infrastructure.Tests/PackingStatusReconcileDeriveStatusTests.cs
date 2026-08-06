using CRM.Core.Constants;
using CRM.Infrastructure.Packings;
using Xunit;

namespace CRM.Infrastructure.Tests;

public class PackingStatusReconcileDeriveStatusTests
{
    [Theory]
    [InlineData(PackingStatusCode.StockOutFinished, false, PackingStatusCode.Ready)]
    [InlineData(PackingStatusCode.PendingStockOut, false, PackingStatusCode.Ready)]
    [InlineData(PackingStatusCode.Ready, false, PackingStatusCode.Ready)]
    [InlineData(PackingStatusCode.StockOutFinished, true, PackingStatusCode.StockOutFinished)]
    [InlineData(PackingStatusCode.Ready, true, PackingStatusCode.StockOutFinished)]
    public void DeriveStatus_FollowsLiveCompletedStockOutFact(short current, bool liveDone, short expected)
    {
        Assert.Equal(expected, PackingStatusReconcileService.DeriveStatus(current, liveDone));
    }
}
