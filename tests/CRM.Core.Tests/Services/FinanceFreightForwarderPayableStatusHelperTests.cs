using CRM.Core.Constants;
using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Services;

public class FinanceFreightForwarderPayableStatusHelperTests
{
    [Theory]
    [InlineData(1000, 0, FinanceFreightForwarderPayableStatusCodes.Pending)]
    [InlineData(1000, 300, FinanceFreightForwarderPayableStatusCodes.Partial)]
    [InlineData(1000, 1000, FinanceFreightForwarderPayableStatusCodes.Completed)]
    [InlineData(1000, 1200, FinanceFreightForwarderPayableStatusCodes.Completed)]
    public void Compute_ReturnsExpectedStatus(decimal receipt, decimal paid, short expected)
    {
        Assert.Equal(expected, FinanceFreightForwarderPayableStatusHelper.Compute(receipt, paid));
    }

    [Fact]
    public void PendingAmount_NeverNegative()
    {
        Assert.Equal(0m, FinanceFreightForwarderPayableStatusHelper.PendingAmount(100m, 150m));
        Assert.Equal(40m, FinanceFreightForwarderPayableStatusHelper.PendingAmount(100m, 60m));
    }
}
