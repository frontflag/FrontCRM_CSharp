using CRM.Core.Constants;
using CRM.Infrastructure.Packings;
using Xunit;

namespace CRM.Infrastructure.Tests;

public class PackingStatusReconcileDeriveNotifyStatusTests
{
    [Theory]
    [InlineData(StockOutRequestStatusCode.Packed, true, StockOutRequestStatusCode.StockedOut)]
    [InlineData(StockOutRequestStatusCode.PendingPacking, true, StockOutRequestStatusCode.StockedOut)]
    [InlineData(StockOutRequestStatusCode.StockedOut, true, StockOutRequestStatusCode.StockedOut)]
    [InlineData(StockOutRequestStatusCode.StockedOut, false, StockOutRequestStatusCode.Packed)]
    [InlineData(StockOutRequestStatusCode.Packed, false, StockOutRequestStatusCode.Packed)]
    [InlineData(StockOutRequestStatusCode.PendingCustoms, false, StockOutRequestStatusCode.PendingCustoms)]
    [InlineData(StockOutRequestStatusCode.Cancelled, true, StockOutRequestStatusCode.Cancelled)]
    [InlineData(StockOutRequestStatusCode.Cancelled, false, StockOutRequestStatusCode.Cancelled)]
    public void DeriveNotifyStatus_FollowsLiveCompletedStockOutFact(
        short current,
        bool liveDone,
        short expected)
    {
        Assert.Equal(expected, PackingStatusReconcileService.DeriveNotifyStatus(current, liveDone));
    }
}
